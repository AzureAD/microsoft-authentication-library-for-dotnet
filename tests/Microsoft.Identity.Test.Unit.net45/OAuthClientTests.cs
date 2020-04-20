// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
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

#if DESKTOP
        [TestMethod]
        public void PKeyAuthSuccsesResponseTest()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);

                //Initiates PKeyAuth challenge which will trigger an additional request sent to AAD to satisfy the PKeyAuth challenge
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreatePKeyAuthChallengeResponse()
                    });
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandlerWithPKeyAuthValidation());

                AuthenticationResult result = app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .Result;
            }
        }

        private static MockHttpMessageHandler CreateTokenResponseHttpHandlerWithPKeyAuthValidation()
        {
            return new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                AdditionalRequestValidation = request =>
                {
                    var requestContent = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var formsData = CoreHelpers.ParseKeyValueList(requestContent, '&', true, null);

                    // Check presence of client_assertion in request
                    var encodedJwt = formsData.First().Value;

                    // Check presence and value of pkeyAuth value.
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadJwtToken(encodedJwt);
                    var pKeyAuth = jsonToken.Header.Where(header => header.Key == "x-ms-PKeyAuth").FirstOrDefault();
                    Assert.AreEqual("x-ms-PKeyAuth", pKeyAuth.Key, "PKey Auth Header: x-ms-PKeyAuth should be present");
                    Assert.AreEqual(pKeyAuth.Value.ToString(), TestConstants.PKeyAuthResponse);
                }
            };
        }
#endif

        private static void MockInstanceDiscoveryAndOpenIdRequest(MockHttpManager mockHttpManager)
        {
            mockHttpManager.AddInstanceDiscoveryMockHandler();
            mockHttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityHomeTenant);
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
