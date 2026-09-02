// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.WsTrust;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common;
using System.Globalization;
using Microsoft.Identity.Client.Internal;
using System.Linq;
using Microsoft.Identity.Test.Common.Core.Helpers;
using NSubstitute.Extensions;

namespace Microsoft.Identity.Test.Unit.CoreTests.WsTrustTests
{
    [TestClass]
    [DeploymentItem(@"Resources\WsTrustResponse13.xml")]
    [DeploymentItem(@"Resources\WsTrustResponseNoToken.xml")]
    public class WsTrustTests : TestBase
    {
        [TestMethod]
        [Description("WS-Trust Request Test")]
        public async Task WsTrustRequestTestAsync()
        {
            string wsTrustAddress = "https://some/address/usernamemixed";
            var endpoint = new WsTrustEndpoint(new Uri(wsTrustAddress), WsTrustVersion.WsTrust13);

            using (var harness = CreateTestHarness())
            {
                MockHttpMessageHandler handler = harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedUrl = wsTrustAddress,
                        ExpectedMethod = HttpMethod.Post,
                        AdditionalRequestValidation = (req) =>
                        {
                            Assert.IsFalse(req.Headers.Contains("ContentType"));
                            var soapActionValues = req.Headers.GetValues("SOAPAction");
                            var soapAction = soapActionValues.Single();
                            Assert.AreEqual(XmlNamespace.Issue.ToString(), soapAction);

                            // Content Type is set on the content, not on the request!
                            var contentType = req.Content.Headers.ContentType;
                            Assert.AreEqual("application/soap+xml", contentType.MediaType);
                            Assert.AreEqual("utf-8", contentType.CharSet);
                        },
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                               File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("WsTrustResponse13.xml")))
                        }
                    });

                var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null);
                var wsTrustRequest = endpoint.BuildTokenRequestMessageWindowsIntegratedAuth("urn:federation:SomeAudience");
                var wsTrustResponse = await harness.ServiceBundle.WsTrustWebRequestManager.GetWsTrustResponseAsync(endpoint, wsTrustRequest, requestContext)
                                                   .ConfigureAwait(false);

                Assert.IsNotNull(wsTrustResponse.Token);
                Assert.IsFalse(handler.AllowAutoRedirect);
            }
        }

        [TestMethod]
        public async Task WsTrustHttpRedirectIsRejectedWithoutFollowingTestAsync()
        {
            // Arrange
            const string wsTrustAddress = "https://some/address/usernamemixed";
            var endpoint = new WsTrustEndpoint(new Uri(wsTrustAddress), WsTrustVersion.WsTrust13);

            using (var harness = CreateTestHarness())
            {
                var response = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect);
                response.Headers.Location = new Uri("http://some/address/redirected");

                MockHttpMessageHandler handler = harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = wsTrustAddress,
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = response
                    });

                var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null);
                string wsTrustRequest = endpoint.BuildTokenRequestMessageUsernamePassword(
                    "urn:federation:SomeAudience",
                    "username",
                    "password");

                // Act
                MsalClientException exception = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => harness.ServiceBundle.WsTrustWebRequestManager.GetWsTrustResponseAsync(
                        endpoint,
                        wsTrustRequest,
                        requestContext))
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.NonHttpsRedirectNotSupported, exception.ErrorCode);
                Assert.IsFalse(handler.AllowAutoRedirect);
                Assert.IsNotNull(handler.ActualRequestMessage);
            }
        }

        [TestMethod]
        public async Task WsTrustHttpsRedirectIsFollowedTestAsync()
        {
            // Arrange
            const string wsTrustAddress = "https://some/address/usernamemixed";
            const string redirectedWsTrustAddress = "https://some/redirected/usernamemixed";
            var endpoint = new WsTrustEndpoint(new Uri(wsTrustAddress), WsTrustVersion.WsTrust13);
            var redirectResponse = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect);
            redirectResponse.Headers.Location = new Uri(redirectedWsTrustAddress);
            string wsTrustRequest = endpoint.BuildTokenRequestMessageUsernamePassword(
                "urn:federation:SomeAudience",
                "username",
                "password");

            using (var harness = CreateTestHarness())
            {
                MockHttpMessageHandler redirectHandler = harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = wsTrustAddress,
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = redirectResponse
                    });
                MockHttpMessageHandler responseHandler = harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = redirectedWsTrustAddress,
                        ExpectedMethod = HttpMethod.Post,
                        AdditionalRequestValidation = request =>
                        {
                            Assert.IsNotNull(request.Content);
                            Assert.AreEqual(
                                "application/soap+xml",
                                request.Content.Headers.ContentType.MediaType);
                            Assert.AreEqual(
                                wsTrustRequest,
                                request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                        },
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("WsTrustResponse13.xml")))
                        }
                    });

                var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null);

                // Act
                WsTrustResponse wsTrustResponse =
                    await harness.ServiceBundle.WsTrustWebRequestManager.GetWsTrustResponseAsync(
                        endpoint,
                        wsTrustRequest,
                        requestContext)
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(wsTrustResponse.Token);
                Assert.IsFalse(redirectHandler.AllowAutoRedirect);
                Assert.IsFalse(responseHandler.AllowAutoRedirect);
                Assert.IsTrue(redirectHandler.UseDefaultCredentials);
                Assert.IsTrue(responseHandler.UseDefaultCredentials);
                Assert.IsNotNull(redirectHandler.ActualRequestMessage);
                Assert.IsNotNull(responseHandler.ActualRequestMessage);
            }
        }

        [TestMethod]
        public async Task WsTrustCrossOriginCredentialRedirectIsRejectedTestAsync()
        {
            // Arrange
            const string wsTrustAddress = "https://some/address/usernamemixed";
            var endpoint = new WsTrustEndpoint(new Uri(wsTrustAddress), WsTrustVersion.WsTrust13);
            var redirectResponse = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect);
            redirectResponse.Headers.Location = new Uri("https://different.example/usernamemixed");

            using (var harness = CreateTestHarness())
            {
                MockHttpMessageHandler handler = harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = wsTrustAddress,
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = redirectResponse
                    });

                var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null);
                string wsTrustRequest = endpoint.BuildTokenRequestMessageUsernamePassword(
                    "urn:federation:SomeAudience",
                    "username",
                    "password");

                // Act
                MsalClientException exception = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => harness.ServiceBundle.WsTrustWebRequestManager.GetWsTrustResponseAsync(
                        endpoint,
                        wsTrustRequest,
                        requestContext))
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.WsTrustCrossOriginRedirectNotSupported, exception.ErrorCode);
                Assert.IsNotNull(handler.ActualRequestMessage);
                Assert.AreEqual(0, harness.HttpManager.QueueSize);
            }
        }

        [TestMethod]
        public async Task WsTrustMultipleChoicesRedirectChangesPostToGetTestAsync()
        {
            // Arrange
            const string wsTrustAddress = "https://some/address/usernamemixed";
            const string redirectedWsTrustAddress = "https://redirected.some/address/usernamemixed";
            var endpoint = new WsTrustEndpoint(new Uri(wsTrustAddress), WsTrustVersion.WsTrust13);
            var redirectResponse = new HttpResponseMessage(HttpStatusCode.MultipleChoices);
            redirectResponse.Headers.Location = new Uri(redirectedWsTrustAddress);

            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = wsTrustAddress,
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = redirectResponse
                    });
                MockHttpMessageHandler responseHandler = harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = redirectedWsTrustAddress,
                        ExpectedMethod = HttpMethod.Get,
                        AdditionalRequestValidation = request => Assert.IsNull(request.Content),
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("WsTrustResponse13.xml")))
                        }
                    });

                var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null);
                string wsTrustRequest = endpoint.BuildTokenRequestMessageUsernamePassword(
                    "urn:federation:SomeAudience",
                    "username",
                    "password");

                // Act
                WsTrustResponse wsTrustResponse =
                    await harness.ServiceBundle.WsTrustWebRequestManager.GetWsTrustResponseAsync(
                        endpoint,
                        wsTrustRequest,
                        requestContext)
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(wsTrustResponse.Token);
                Assert.IsFalse(responseHandler.UseDefaultCredentials);
                Assert.IsNotNull(responseHandler.ActualRequestMessage);
            }
        }

        [TestMethod]
        public async Task WsTrustHttpEndpointIsRejectedBeforeRequestTestAsync()
        {
            // Arrange
            const string wsTrustAddress = "http://some/address/usernamemixed";
            var endpoint = new WsTrustEndpoint(new Uri(wsTrustAddress), WsTrustVersion.WsTrust13);

            using (var harness = CreateTestHarness())
            {
                MockHttpMessageHandler handler = harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = wsTrustAddress,
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("WsTrustResponse13.xml")))
                        }
                    });

                var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null);
                string wsTrustRequest = endpoint.BuildTokenRequestMessageUsernamePassword(
                    "urn:federation:SomeAudience",
                    "username",
                    "password");

                // Act
                MsalClientException exception = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => harness.ServiceBundle.WsTrustWebRequestManager.GetWsTrustResponseAsync(
                        endpoint,
                        wsTrustRequest,
                        requestContext))
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.WsTrustEndpointNotFoundInMetadataDocument, exception.ErrorCode);
                Assert.IsNull(handler.ActualRequestMessage);
                harness.HttpManager.ClearQueue();
            }
        }

        [TestMethod]
        [Description("WsTrustRequest encounters HTTP 404")]
        public async Task WsTrustRequestFailureTestAsync()
        {
            string uri = "https://some/address/usernamemixed";
            var endpoint = new WsTrustEndpoint(new Uri(uri), WsTrustVersion.WsTrust13);

            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddMockHandlerContentNotFound(HttpMethod.Post, url: uri);

                var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null);
                try
                {
                    var message = endpoint.BuildTokenRequestMessageWindowsIntegratedAuth("urn:federation:SomeAudience");

                    WsTrustResponse wstResponse =
                        await harness.ServiceBundle.WsTrustWebRequestManager.GetWsTrustResponseAsync(endpoint, message, requestContext).ConfigureAwait(false);
                    Assert.Fail("We expect an exception to be thrown here");
                }
                catch (MsalException ex)
                {
                    Assert.AreEqual(MsalError.FederatedServiceReturnedError, ex.ErrorCode);
                }
            }
        }

        [TestMethod]
        [Description("WsTrustRequest encounters a non parseable response from the wsTrust endpoint")]
        public async Task WsTrustRequestParseErrorTestAsync()
        {
            const string body = "Non-Parsable";
            const string uri = "https://some/address/usernamemixed";
            string expectedMessage = string.Format(CultureInfo.CurrentCulture, MsalErrorMessage.ParsingWsTrustResponseFailedErrorTemplate, uri);

            var endpoint = new WsTrustEndpoint(new Uri(uri), WsTrustVersion.WsTrust13);

            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedUrl = uri,
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(body)
                        }
                    });

                var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null);
                try
                {
                    var message = endpoint.BuildTokenRequestMessageWindowsIntegratedAuth("urn:federation:SomeAudience");

                    WsTrustResponse wstResponse =
                        await harness.ServiceBundle.WsTrustWebRequestManager.GetWsTrustResponseAsync(endpoint, message, requestContext).ConfigureAwait(false);
                    Assert.Fail("We expect an exception to be thrown here");
                }
                catch (MsalException ex)
                {
                    Assert.AreEqual(MsalError.ParsingWsTrustResponseFailed, ex.ErrorCode);
                    Assert.AreEqual(expectedMessage, ex.Message);
                }
            }
        }

        [TestMethod]
        [Description("WsTrustRequest encounters a response with no token from the wsTrust endpoint")]
        public async Task WsTrustRequestTokenNotFoundInResponseTestAsync()
        {
            const string uri = "https://some/address/usernamemixed";

            var endpoint = new WsTrustEndpoint(new Uri(uri), WsTrustVersion.WsTrust2005);

            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedUrl = uri,
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                               File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("WsTrustResponseNoToken.xml")))
                        }
                    });

                var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null);

                var message = endpoint.BuildTokenRequestMessageWindowsIntegratedAuth("urn:federation:SomeAudience");

                var ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                    harness.ServiceBundle.WsTrustWebRequestManager.GetWsTrustResponseAsync(endpoint, message, requestContext)).ConfigureAwait(false);
                
                Assert.AreEqual(MsalError.ParsingWsTrustResponseFailed, ex.ErrorCode);
            }
        }
    }
}
