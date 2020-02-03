// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class OAuthClientTests : TestBase
    {
        [TestMethod]
        public void OAuthClient_FailsWithServiceExceptionWhenItCannotParseJsonResponse()
        {
            ValidateOathClient(
                MockHelpers.CreateTooManyRequestsNonJsonResponse(),
                exception =>
                {
                    MsalServiceException serverEx = exception.InnerException as MsalServiceException;
                    Assert.IsNotNull(serverEx);
                    Assert.AreEqual(429, serverEx.StatusCode);
                    Assert.AreEqual(MockHelpers.TooManyRequestsContent, serverEx.ResponseBody);
                    Assert.AreEqual(MockHelpers.TestRetryAfterDuration, serverEx.Headers.RetryAfter.Delta);
                    Assert.AreEqual(MsalError.NonParsableOAuthError, serverEx.ErrorCode);
                });
        }

        [TestMethod]
        public void OAuthClient_FailsWithServiceExceptionWhenItCanParseJsonResponse()
        {
            ValidateOathClient(
                MockHelpers.CreateTooManyRequestsJsonResponse(),
                exception =>
                {
                    MsalServiceException serverEx = exception.InnerException as MsalServiceException;
                    Assert.IsNotNull(serverEx);
                    Assert.AreEqual(429, serverEx.StatusCode);
                    Assert.AreEqual(MockHelpers.TestRetryAfterDuration, serverEx.Headers.RetryAfter.Delta);
                    Assert.AreEqual("Server overload", serverEx.ErrorCode);
                });
        }

        [TestMethod]
        public void OAuthClient_FailsWithServiceExceptionWhenEntireResponseIsNull()
        {
            ValidateOathClient(
                null,
                exception =>
                {
                    var innerException = exception.InnerException as InvalidOperationException;
                    Assert.IsNotNull(innerException);
                });
        }

        [TestMethod]
        public void OAuthClient_FailsWithServiceExceptionWhenResponseIsEmpty()
        {
            ValidateOathClient(
                MockHelpers.CreateEmptyResponseMessage(),
                exception =>
                {
                    var serverEx = exception.InnerException as MsalServiceException;
                    Assert.IsNotNull(serverEx);
                    Assert.AreEqual((int)HttpStatusCode.BadRequest, serverEx.StatusCode);
                    Assert.IsNotNull(serverEx.ResponseBody);
                    Assert.AreEqual(MsalError.HttpStatusCodeNotOk, serverEx.ErrorCode);
                });
        }

        [TestMethod]
        public void OAuthClient_FailsWithServiceExceptionWhenResponseIsNull()
        {
            ValidateOathClient(
                MockHelpers.CreateNullResponseMessage(),
                exception =>
                {
                    var serverEx = exception.InnerException as MsalServiceException;
                    Assert.IsNotNull(serverEx);
                    Assert.AreEqual((int)HttpStatusCode.BadRequest, serverEx.StatusCode);
                    Assert.IsNull(serverEx.ResponseBody);
                    Assert.AreEqual(MsalError.HttpStatusCodeNotOk, serverEx.ErrorCode);
                });
        }

        [TestMethod]
        public void OAuthClient_FailsWithServiceExceptionWhenResponseDoesNotContainAnErrorField()
        {
            ValidateOathClient(
                MockHelpers.CreateNoErrorFieldResponseMessage(),
                exception =>
                {
                    var serverEx = exception.InnerException as MsalServiceException;
                    Assert.IsNotNull(serverEx);
                    Assert.AreEqual((int)HttpStatusCode.BadRequest, serverEx.StatusCode);
                    Assert.IsNotNull(serverEx.ResponseBody);
                    Assert.AreEqual(MsalError.HttpStatusCodeNotOk, serverEx.ErrorCode);
                });
        }

        [TestMethod]
        public void OAuthClient_FailsWithServiceExceptionWhenResponseIsHttpNotFound()
        {
            ValidateOathClient(
                MockHelpers.CreateHttpStatusNotFoundResponseMessage(),
                exception =>
                {
                    var serverEx = exception.InnerException as MsalServiceException;
                    Assert.IsNotNull(serverEx);
                    Assert.AreEqual((int)HttpStatusCode.NotFound, serverEx.StatusCode);
                    Assert.IsNotNull(serverEx.ResponseBody);
                    Assert.AreEqual(MsalError.HttpStatusNotFound, serverEx.ErrorCode);
                });
        }

        private void ValidateOathClient(HttpResponseMessage httpResponseMessage, Action<Exception> validationHandler)
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = httpResponseMessage
                    });

                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    new TokenCache(harness.ServiceBundle, false));
                parameters.RedirectUri = new Uri("some://uri");
                parameters.LoginHint = TestConstants.DisplayableId;
                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.SelectAccount,
                    ExtraScopesToConsent = TestConstants.s_scopeForAnotherResource.ToArray(),
                };
                MsalMockHelpers.ConfigureMockWebUI(harness.ServiceBundle.PlatformProxy, new MockWebUI());

                var request = new InteractiveRequest(
                    parameters,
                    interactiveParameters);

                try
                {
                    request.RunAsync().Wait();
                    Assert.Fail("MsalException should have been thrown here");
                }
                catch (Exception exc)
                {
                    validationHandler(exc);
                }
            }
        }

    }
}
