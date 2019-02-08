// ------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// ------------------------------------------------------------------------------

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.Identity.Test.Unit.PublicApiTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache.Items;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class InteractiveRequestTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();
        }

        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public async Task WithExtraQueryParamsAndClaimsAsync()
        {

            IDictionary<string, string> extraQueryParamsAndClaims =
                MsalTestConstants.ExtraQueryParams.ToDictionary(e => e.Key, e => e.Value);
            extraQueryParamsAndClaims.Add(OAuth2Parameter.Claims, MsalTestConstants.Claims);

            using (MockHttpAndServiceBundle harness = new MockHttpAndServiceBundle())
            {
                var cache = new TokenCache(harness.ServiceBundle);

                var ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        MsalTestConstants.AuthorityHomeTenant + "?code=some-code"),
                    QueryParamsToValidate = MsalTestConstants.ExtraQueryParams
                };

                MockInstanceDiscoveryAndOpenIdRequest(harness.HttpManager);

                var tokenResponseHandler = new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Post,
                    ExpectedQueryParams = extraQueryParamsAndClaims,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                };
                harness.HttpManager.AddMockHandler(tokenResponseHandler);

                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    MsalTestConstants.AuthorityHomeTenant,
                    MsalTestConstants.Scope,
                    cache,
                    extraQueryParameters: MsalTestConstants.ExtraQueryParams,
                    claims: MsalTestConstants.Claims);

                parameters.RedirectUri = new Uri("some://uri");
                parameters.LoginHint = MsalTestConstants.DisplayableId;

                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.SelectAccount,
                    ExtraScopesToConsent = MsalTestConstants.ScopeForAnotherResource.ToArray(),
                };

                InteractiveRequest request = new InteractiveRequest(
                    harness.ServiceBundle,
                    parameters,
                    interactiveParameters,
                    ui);

                AuthenticationResult result = await request.RunAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(1, ((ITokenCacheInternal)cache).Accessor.RefreshTokenCount);
                Assert.AreEqual(1, ((ITokenCacheInternal)cache).Accessor.AccessTokenCount);
                Assert.AreEqual(result.AccessToken, "some-access-token");
            }
        }

        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public void NoCacheLookup()
        {
            MyReceiver myReceiver = new MyReceiver();

            using (MockHttpAndServiceBundle harness = new MockHttpAndServiceBundle(telemetryCallback: myReceiver.HandleTelemetryEvents))
            {
                TokenCache cache = new TokenCache(harness.ServiceBundle);

                MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.ClientId,
                    MsalTestConstants.Scope.AsSingleString(),
                    MsalTestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3599)),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(7200)),
                    MockHelpers.CreateClientInfo());

                string atKey = atItem.GetKey().ToString();
                atItem.Secret = atKey;
                ((ITokenCacheInternal)cache).Accessor.SaveAccessToken(atItem);

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.Success,
                        MsalTestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MockInstanceDiscoveryAndOpenIdRequest(harness.HttpManager);

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.AuthorityHomeTenant);

                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    MsalTestConstants.AuthorityHomeTenant,
                    MsalTestConstants.Scope,
                    cache,
                    extraQueryParameters: new Dictionary<string, string>
                    {
                        {"extra", "qp"}
                    });
                parameters.RedirectUri = new Uri("some://uri");
                parameters.LoginHint = MsalTestConstants.DisplayableId;
                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.SelectAccount,
                    ExtraScopesToConsent = MsalTestConstants.ScopeForAnotherResource.ToArray(),
                };

                InteractiveRequest request = new InteractiveRequest(
                    harness.ServiceBundle,
                    parameters,
                    interactiveParameters,
                    ui);

                Task<AuthenticationResult> task = request.RunAsync(CancellationToken.None);
                task.Wait();
                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(1, ((ITokenCacheInternal)cache).Accessor.RefreshTokenCount);
                Assert.AreEqual(2, ((ITokenCacheInternal)cache).Accessor.AccessTokenCount);
                Assert.AreEqual(result.AccessToken, "some-access-token");

                Assert.IsNotNull(
                    myReceiver.EventsReceived.Find(
                        anEvent => // Expect finding such an event
                            anEvent[EventBase.EventNameKey].EndsWith("ui_event") &&
                            anEvent[UiEvent.UserCancelledKey] == "false"));
                Assert.IsNotNull(
                    myReceiver.EventsReceived.Find(
                        anEvent => // Expect finding such an event
                            anEvent[EventBase.EventNameKey].EndsWith("api_event") &&
                            anEvent[ApiEvent.PromptKey] == "select_account"));
                Assert.IsNotNull(
                    myReceiver.EventsReceived.Find(
                        anEvent => // Expect finding such an event
                            anEvent[EventBase.EventNameKey].EndsWith("ui_event") &&
                            anEvent[UiEvent.AccessDeniedKey] == "false"));
            }
        }

        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public void RedirectUriContainsFragmentErrorTest()
        {
            try
            {
                using (MockHttpAndServiceBundle harness = new MockHttpAndServiceBundle())
                {
                    AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                        MsalTestConstants.AuthorityHomeTenant,
                        MsalTestConstants.Scope,
                        null,
                        extraQueryParameters: new Dictionary<string, string>
                        {
                            {"extra", "qp"}
                        });
                    parameters.RedirectUri = new Uri("some://uri#fragment=not-so-good");
                    parameters.LoginHint = MsalTestConstants.DisplayableId;
                    AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters
                    {
                        Prompt = Prompt.ForceLogin,
                        ExtraScopesToConsent = MsalTestConstants.ScopeForAnotherResource.ToArray(),
                    };

                    new InteractiveRequest(
                        harness.ServiceBundle,
                        parameters,
                        interactiveParameters,
                        new MockWebUI());

                    Assert.Fail("ArgumentException should be thrown here");
                }
            }
            catch (ArgumentException ae)
            {
                Assert.IsTrue(ae.Message.Contains(CoreErrorMessages.RedirectUriContainsFragment));
            }
        }

        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public void OAuthClient_FailsWithServiceExceptionWhenItCannotParseJsonResponse()
        {
            using (MockHttpAndServiceBundle harness = new MockHttpAndServiceBundle())
            {
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = MockHelpers.CreateTooManyRequestsNonJsonResponse() // returns a non json response
                    });

                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    MsalTestConstants.AuthorityHomeTenant,
                    MsalTestConstants.Scope,
                    null);
                parameters.RedirectUri = new Uri("some://uri");
                parameters.LoginHint = MsalTestConstants.DisplayableId;
                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.SelectAccount,
                    ExtraScopesToConsent = MsalTestConstants.ScopeForAnotherResource.ToArray(),
                };

                InteractiveRequest request = new InteractiveRequest(
                    harness.ServiceBundle,
                    parameters,
                    interactiveParameters,
                    new MockWebUI());

                try
                {
                    request.ExecuteAsync(CancellationToken.None).Wait();

                    Assert.Fail("MsalException should have been thrown here");
                }
                catch (Exception exc)
                {
                    MsalServiceException serverEx = exc.InnerException as MsalServiceException;
                    Assert.IsNotNull(serverEx);
                    Assert.AreEqual(429, serverEx.StatusCode);
                    Assert.AreEqual(MockHelpers.TooManyRequestsContent, serverEx.ResponseBody);
                    Assert.AreEqual(MockHelpers.TestRetryAfterDuration, serverEx.Headers.RetryAfter.Delta);
                    Assert.AreEqual(CoreErrorCodes.NonParsableOAuthError, serverEx.ErrorCode);
                }
            }
        }

        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public void OAuthClient_FailsWithServiceExceptionWhenItCanParseJsonResponse()
        {
            using (MockHttpAndServiceBundle harness = new MockHttpAndServiceBundle())
            {
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = MockHelpers.CreateTooManyRequestsJsonResponse() // returns a non json response
                    });

                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    MsalTestConstants.AuthorityHomeTenant,
                    MsalTestConstants.Scope,
                    null);
                parameters.RedirectUri = new Uri("some://uri");
                parameters.LoginHint = MsalTestConstants.DisplayableId;
                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.SelectAccount,
                    ExtraScopesToConsent = MsalTestConstants.ScopeForAnotherResource.ToArray(),
                };

                InteractiveRequest request = new InteractiveRequest(
                    harness.ServiceBundle,
                    parameters,
                    interactiveParameters,
                    new MockWebUI());

                try
                {
                    request.ExecuteAsync(CancellationToken.None).Wait();
                    Assert.Fail("MsalException should have been thrown here");
                }
                catch (Exception exc)
                {
                    MsalServiceException serverEx = exc.InnerException as MsalServiceException;
                    Assert.IsNotNull(serverEx);
                    Assert.AreEqual(429, serverEx.StatusCode);
                    Assert.AreEqual(MockHelpers.TestRetryAfterDuration, serverEx.Headers.RetryAfter.Delta);
                    Assert.AreEqual("Server overload", serverEx.ErrorCode);
                }
            }
        }

        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public void VerifyAuthorizationResultTest()
        {
            using (MockHttpAndServiceBundle harness = new MockHttpAndServiceBundle())
            {
                MockInstanceDiscoveryAndOpenIdRequest(harness.HttpManager);

                MockWebUI webUi = new MockWebUI()
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.ErrorHttp,
                        MsalTestConstants.AuthorityHomeTenant + "?error=" + OAuth2Error.LoginRequired)
                };

                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    MsalTestConstants.AuthorityHomeTenant,
                    MsalTestConstants.Scope,
                    null,
                    extraQueryParameters: new Dictionary<string, string> { { "extra", "qp" } });
                parameters.RedirectUri = new Uri("some://uri");
                parameters.LoginHint = MsalTestConstants.DisplayableId;
                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.ForceLogin,
                    ExtraScopesToConsent = MsalTestConstants.ScopeForAnotherResource.ToArray(),
                };

                InteractiveRequest request = new InteractiveRequest(
                    harness.ServiceBundle,
                    parameters,
                    interactiveParameters,
                    webUi);

                try
                {
                    request.ExecuteAsync(CancellationToken.None).Wait();
                    Assert.Fail("MsalException should have been thrown here");
                }
                catch (Exception exc)
                {
                    Assert.IsTrue(exc.InnerException is MsalUiRequiredException);
                    Assert.AreEqual(
                        MsalUiRequiredException.NoPromptFailedError,
                        ((MsalUiRequiredException)exc.InnerException).ErrorCode);
                }

                webUi = new MockWebUI
                {
                    MockResult = new AuthorizationResult(
                        AuthorizationStatus.ErrorHttp,
                        MsalTestConstants.AuthorityHomeTenant + "?error=invalid_request&error_description=some error description")
                };

                request = new InteractiveRequest(
                    harness.ServiceBundle,
                    parameters,
                    interactiveParameters,
                    webUi);

                try
                {
                    request.ExecuteAsync(CancellationToken.None).Wait(CancellationToken.None);
                    Assert.Fail("MsalException should have been thrown here");
                }
                catch (Exception exc)
                {
                    Assert.IsTrue(exc.InnerException is MsalException);
                    Assert.AreEqual("invalid_request", ((MsalException)exc.InnerException).ErrorCode);
                    Assert.AreEqual("some error description", ((MsalException)exc.InnerException).Message);
                }
            }
        }

        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public void DuplicateQueryParameterErrorTest()
        {
            using (MockHttpAndServiceBundle harness = new MockHttpAndServiceBundle())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);

                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    MsalTestConstants.AuthorityHomeTenant,
                    MsalTestConstants.Scope,
                    null,
                    extraQueryParameters: new Dictionary<string, string> { { "extra", "qp" }, { "prompt", "login" } });
                parameters.RedirectUri = new Uri("some://uri");
                parameters.LoginHint = MsalTestConstants.DisplayableId;
                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.ForceLogin,
                    ExtraScopesToConsent = MsalTestConstants.ScopeForAnotherResource.ToArray(),
                };

                InteractiveRequest request = new InteractiveRequest(
                    harness.ServiceBundle,
                    parameters,
                    interactiveParameters,
                    new MockWebUI());

                try
                {
                    request.ExecuteAsync(CancellationToken.None).Wait();
                    Assert.Fail("MsalException should be thrown here");
                }
                catch (Exception exc)
                {
                    Assert.IsTrue(exc.InnerException is MsalException);
                    Assert.AreEqual(
                        MsalClientException.DuplicateQueryParameterError,
                        ((MsalException)exc.InnerException).ErrorCode);
                }
            }
        }

        private static void MockInstanceDiscoveryAndOpenIdRequest(MockHttpManager mockHttpManager)
        {
            mockHttpManager.AddInstanceDiscoveryMockHandler();
            mockHttpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
        }
    }
}
