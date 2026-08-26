// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.WsTrust;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.WsTrustTests
{
    [TestClass]
    [DeploymentItem(@"Resources\TestMex2005.xml")]
    public class MexParserTests : TestBase
    {
        [TestMethod]
        [Description("WS-Trust Address Extraction Test")]
        public void WsTrust2005AddressExtractionTest()
        {
            // Arrange
            string responseBody = File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("TestMex2005.xml"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(responseBody));

            // Act
            var mexDocument = new MexDocument(responseBody);
            var wsTrustEndpoint = mexDocument.GetWsTrustWindowsTransportEndpoint();

            // Assert
            Assert.AreEqual(
                "https://sts.usystech.net/adfs/services/trust/2005/windowstransport",
                wsTrustEndpoint.Uri.AbsoluteUri);
            Assert.AreEqual(WsTrustVersion.WsTrust2005, wsTrustEndpoint.Version);

            // Act
            wsTrustEndpoint = mexDocument.GetWsTrustUsernamePasswordEndpoint();

            // Assert
            Assert.AreEqual("https://sts.usystech.net/adfs/services/trust/2005/usernamemixed", wsTrustEndpoint.Uri.AbsoluteUri);
            Assert.AreEqual(WsTrustVersion.WsTrust2005, wsTrustEndpoint.Version);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\TestMex.xml")]
        public void WsTrustHttpEndpointsAreNotSelectedTest()
        {
            // Arrange
            string responseBody = File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("TestMex.xml"))
                .Replace(
                    "https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed",
                    "http://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed")
                .Replace(
                    "https://msft.sts.microsoft.com/adfs/services/trust/13/usernamemixed",
                    "http://msft.sts.microsoft.com/adfs/services/trust/13/usernamemixed")
                .Replace(
                    "https://msft.sts.microsoft.com/adfs/services/trust/2005/windowstransport",
                    "http://msft.sts.microsoft.com/adfs/services/trust/2005/windowstransport")
                .Replace(
                    "https://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport",
                    "http://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport");

            // Act
            var mexDocument = new MexDocument(responseBody);

            // Assert
            Assert.IsNull(mexDocument.GetWsTrustUsernamePasswordEndpoint());
            Assert.IsNull(mexDocument.GetWsTrustWindowsTransportEndpoint());
        }

        [TestMethod]
        [DeploymentItem(@"Resources\TestMex.xml")]
        public void WsTrustHttp13EndpointsFallBackToHttps2005Test()
        {
            // Arrange
            string responseBody = File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("TestMex.xml"))
                .Replace(
                    "https://msft.sts.microsoft.com/adfs/services/trust/13/usernamemixed",
                    "http://msft.sts.microsoft.com/adfs/services/trust/13/usernamemixed")
                .Replace(
                    "https://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport",
                    "http://msft.sts.microsoft.com/adfs/services/trust/13/windowstransport");

            // Act
            var mexDocument = new MexDocument(responseBody);
            WsTrustEndpoint usernamePasswordEndpoint = mexDocument.GetWsTrustUsernamePasswordEndpoint();
            WsTrustEndpoint windowsTransportEndpoint = mexDocument.GetWsTrustWindowsTransportEndpoint();

            // Assert
            Assert.AreEqual(
                "https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed",
                usernamePasswordEndpoint.Uri.AbsoluteUri);
            Assert.AreEqual(WsTrustVersion.WsTrust2005, usernamePasswordEndpoint.Version);
            Assert.AreEqual(
                "https://msft.sts.microsoft.com/adfs/services/trust/2005/windowstransport",
                windowsTransportEndpoint.Uri.AbsoluteUri);
            Assert.AreEqual(WsTrustVersion.WsTrust2005, windowsTransportEndpoint.Version);
        }

        [TestMethod]
        [Description("Mex endpoint fails to resolve")]
        public async Task MexEndpointFailsToResolveTestAsync()
        {
            // TODO: should we move this into a separate test class for WsTrustWebRequestManager?
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddMockHandlerContentNotFound(HttpMethod.Get);

                try
                {
                    await harness.ServiceBundle.WsTrustWebRequestManager.GetMexDocumentAsync("https://somehost",
                                            new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null))
                                            .ConfigureAwait(false);
                    Assert.Fail("We expect an exception to be thrown here");
                }
                catch (MsalException ex)
                {
                    Assert.AreEqual(MsalError.AccessingWsMetadataExchangeFailed, ex.ErrorCode);
                }
            }
        }

        [TestMethod]
        public async Task MexHttpEndpointIsRejectedBeforeRequestTestAsync()
        {
            // Arrange
            const string mexAddress = "http://somehost/adfs/services/trust/mex";

            using (var harness = CreateTestHarness())
            {
                MockHttpMessageHandler handler = harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = mexAddress,
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    });

                // Act
                MsalClientException exception = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => harness.ServiceBundle.WsTrustWebRequestManager.GetMexDocumentAsync(
                        mexAddress,
                        new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null)))
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.AccessingWsMetadataExchangeFailed, exception.ErrorCode);
                Assert.IsNull(handler.ActualRequestMessage);
                harness.HttpManager.ClearQueue();
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\TestMex2005.xml")]
        public async Task MexHttpRedirectIsRejectedBeforeParsingTestAsync()
        {
            // Arrange
            const string mexAddress = "https://somehost/adfs/services/trust/mex";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("TestMex2005.xml"))),
                RequestMessage = new HttpRequestMessage(
                    HttpMethod.Get,
                    "http://somehost/adfs/services/trust/mex")
            };

            using (var harness = CreateTestHarness())
            {
                MockHttpMessageHandler handler = harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = mexAddress,
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = response
                    });

                // Act
                MsalClientException exception = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => harness.ServiceBundle.WsTrustWebRequestManager.GetMexDocumentAsync(
                        mexAddress,
                        new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null)))
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.NonHttpsRedirectNotSupported, exception.ErrorCode);
                Assert.IsFalse(handler.AllowAutoRedirect);
                Assert.IsNotNull(handler.ActualRequestMessage);
            }
        }

        [TestMethod]
        public async Task MexHttpRedirectLocationIsRejectedTestAsync()
        {
            // Arrange
            const string mexAddress = "https://somehost/adfs/services/trust/mex";
            var response = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect);
            response.Headers.Location = new Uri("http://somehost/adfs/services/trust/mex");

            using (var harness = CreateTestHarness())
            {
                MockHttpMessageHandler handler = harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = mexAddress,
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = response
                    });

                // Act
                MsalClientException exception = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => harness.ServiceBundle.WsTrustWebRequestManager.GetMexDocumentAsync(
                        mexAddress,
                        new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null)))
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.NonHttpsRedirectNotSupported, exception.ErrorCode);
                Assert.IsNotNull(handler.ActualRequestMessage);
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\TestMex2005.xml")]
        public async Task MexHttpsRedirectIsFollowedTestAsync()
        {
            // Arrange
            const string mexAddress = "https://somehost/adfs/services/trust/mex";
            const string redirectedMexAddress = "https://redirected.somehost/adfs/services/trust/mex";
            var redirectResponse = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect);
            redirectResponse.Headers.Location = new Uri(redirectedMexAddress);

            using (var harness = CreateTestHarness())
            {
                MockHttpMessageHandler redirectHandler = harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = mexAddress,
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = redirectResponse
                    });
                MockHttpMessageHandler responseHandler = harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = redirectedMexAddress,
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("TestMex2005.xml")))
                        }
                    });

                // Act
                MexDocument mexDocument = await harness.ServiceBundle.WsTrustWebRequestManager.GetMexDocumentAsync(
                    mexAddress,
                    new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null))
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(mexDocument.GetWsTrustUsernamePasswordEndpoint());
                Assert.IsFalse(redirectHandler.AllowAutoRedirect);
                Assert.IsFalse(responseHandler.AllowAutoRedirect);
                Assert.IsNotNull(redirectHandler.ActualRequestMessage);
                Assert.IsNotNull(responseHandler.ActualRequestMessage);
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\TestMex2005.xml")]
        public async Task MexRelativeHttpsRedirectUsesEffectiveResponseUriTestAsync()
        {
            // Arrange
            const string mexAddress = "https://somehost/adfs/services/trust/mex";
            const string effectiveMexAddress = "https://effective.somehost/adfs/services/trust/mex";
            const string redirectedMexAddress = "https://effective.somehost/adfs/services/redirected/mex";
            var redirectResponse = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect)
            {
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, effectiveMexAddress)
            };
            redirectResponse.Headers.Location = new Uri("../redirected/mex", UriKind.Relative);

            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = mexAddress,
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = redirectResponse
                    });
                MockHttpMessageHandler responseHandler = harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedUrl = redirectedMexAddress,
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("TestMex2005.xml")))
                        }
                    });

                // Act
                MexDocument mexDocument = await harness.ServiceBundle.WsTrustWebRequestManager.GetMexDocumentAsync(
                    mexAddress,
                    new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null))
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(mexDocument.GetWsTrustUsernamePasswordEndpoint());
                Assert.IsNotNull(responseHandler.ActualRequestMessage);
            }
        }

        [TestMethod]
        public async Task MexMissingEndpointIsReportedSeparatelyTestAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                // Act
                MsalClientException exception = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => harness.ServiceBundle.WsTrustWebRequestManager.GetMexDocumentAsync(
                        null,
                        new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null)))
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.MissingFederationMetadataUrl, exception.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.MissingFederationMetadataUrl, exception.Message);
            }
        }

        [TestMethod]
        public async Task MexMalformedEndpointIsReportedSeparatelyTestAsync()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                // Act
                MsalClientException exception = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => harness.ServiceBundle.WsTrustWebRequestManager.GetMexDocumentAsync(
                        "not an absolute URI",
                        new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null)))
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.ParsingWsMetadataExchangeFailed, exception.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.WsTrustMetadataEndpointInvalidUri, exception.Message);
            }
        }

        [TestMethod]
        [Description("Mex endpoint fails to parse")]
        public void MexEndpointFailsToParseTest()
        {
            Assert.Throws<XmlException>(() => new MexDocument("malformed, non-xml content"));
        }
    }
}
