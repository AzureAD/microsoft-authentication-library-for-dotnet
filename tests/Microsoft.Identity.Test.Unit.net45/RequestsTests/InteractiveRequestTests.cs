// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.Identity.Test.Unit.PublicApiTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Microsoft.Identity.Client.Internal.Broker;
using NSubstitute.Core;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class InteractiveRequestTests : TestBase
    {
        [TestMethod]
        public void NullArgs()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                var bundle = harness.ServiceBundle;

                AuthenticationRequestParameters requestParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    new TokenCache(harness.ServiceBundle, false));

                var interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.SelectAccount,
                    ExtraScopesToConsent = TestConstants.s_scopeForAnotherResource.ToArray(),
                };

                var webUi = NSubstitute.Substitute.For<IWebUI>();

                AssertException.Throws<ArgumentNullException>(() =>
                    new InteractiveRequest(null, requestParams, interactiveParameters, webUi));
                AssertException.Throws<ArgumentNullException>(() =>
                    new InteractiveRequest(bundle, null, interactiveParameters, webUi));
                AssertException.Throws<ArgumentNullException>(() =>
                    new InteractiveRequest(bundle, requestParams, null, webUi));
            }
        }

        [TestMethod]
        public async Task WithExtraQueryParamsAndClaimsAsync()
        {
            IDictionary<string, string> extraQueryParamsAndClaims =
                TestConstants.s_extraQueryParams.ToDictionary(e => e.Key, e => e.Value);
            extraQueryParamsAndClaims.Add(OAuth2Parameter.Claims, TestConstants.Claims);

            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                var cache = new TokenCache(harness.ServiceBundle, false);

                var ui = new MockWebUI()
                {
                    MockResult = AuthorizationResult.FromUri(TestConstants.AuthorityHomeTenant + "?code=some-code"),
                    QueryParamsToValidate = TestConstants.s_extraQueryParams
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
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    cache,
                    extraQueryParameters: TestConstants.s_extraQueryParams,
                    claims: TestConstants.Claims);

                parameters.RedirectUri = new Uri("some://uri");
                parameters.LoginHint = TestConstants.DisplayableId;

                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.SelectAccount,
                    ExtraScopesToConsent = TestConstants.s_scopeForAnotherResource.ToArray(),
                };

                InteractiveRequest request = new InteractiveRequest(
                    harness.ServiceBundle,
                    parameters,
                    interactiveParameters,
                    ui);

                AuthenticationResult result = await request.RunAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(1, ((ITokenCacheInternal)cache).Accessor.GetAllRefreshTokens().Count());
                Assert.AreEqual(1, ((ITokenCacheInternal)cache).Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(result.AccessToken, "some-access-token");
            }
        }

        [TestMethod]
        public void NoCacheLookup()
        {
            MyReceiver myReceiver = new MyReceiver();

            using (MockHttpAndServiceBundle harness = CreateTestHarness(telemetryCallback: myReceiver.HandleTelemetryEvents))
            {
                TokenCache cache = new TokenCache(harness.ServiceBundle, false);

                MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    TestConstants.s_scope.AsSingleString(),
                    TestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3599)),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(7200)),
                    MockHelpers.CreateClientInfo());

                string atKey = atItem.GetKey().ToString();
                atItem.Secret = atKey;
                ((ITokenCacheInternal)cache).Accessor.SaveAccessToken(atItem);

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = AuthorizationResult.FromUri(TestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MockInstanceDiscoveryAndOpenIdRequest(harness.HttpManager);

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityHomeTenant);

                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    cache,
                    extraQueryParameters: new Dictionary<string, string>
                    {
                        {"extra", "qp"}
                    });
                parameters.RedirectUri = new Uri("some://uri");
                parameters.LoginHint = TestConstants.DisplayableId;
                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.SelectAccount,
                    ExtraScopesToConsent = TestConstants.s_scopeForAnotherResource.ToArray(),
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
                Assert.AreEqual(1, ((ITokenCacheInternal)cache).Accessor.GetAllRefreshTokens().Count());
                Assert.AreEqual(2, ((ITokenCacheInternal)cache).Accessor.GetAllAccessTokens().Count());
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
        public async Task BrokerInteractiveRequestBrokerRequiredTestAsync()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                MockWebUI ui = new MockWebUI()
                {
                    // When the auth code is returned from the authorization server prefixed with msauth:// 
                    // this means the user who logged requires cert based auth and broker
                    MockResult = AuthorizationResult.FromUri(TestConstants.AuthorityHomeTenant + "?code=msauth://some-code")
                };

                MockInstanceDiscoveryAndOpenIdRequest(harness.HttpManager);

                harness.ServiceBundle.PlatformProxy.SetBrokerForTest(CreateMockBroker());

                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    new TokenCache(harness.ServiceBundle, false));
                parameters.IsBrokerEnabled = false;

                InteractiveRequest request = new InteractiveRequest(
                    harness.ServiceBundle,
                    parameters,
                    new AcquireTokenInteractiveParameters(),
                    ui);

                AuthenticationResult result = await request.RunAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.AreEqual("access-token", result.AccessToken);
            }
        }

        [TestMethod]
        public void RedirectUriContainsFragmentErrorTest()
        {
            try
            {
                using (MockHttpAndServiceBundle harness = CreateTestHarness())
                {
                    AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                        TestConstants.AuthorityHomeTenant,
                        TestConstants.s_scope,
                        new TokenCache(harness.ServiceBundle, false),
                        extraQueryParameters: new Dictionary<string, string>
                        {
                            {"extra", "qp"}
                        });
                    parameters.RedirectUri = new Uri("some://uri#fragment=not-so-good");
                    parameters.LoginHint = TestConstants.DisplayableId;
                    AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters
                    {
                        Prompt = Prompt.ForceLogin,
                        ExtraScopesToConsent = TestConstants.s_scopeForAnotherResource.ToArray(),
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
                Assert.IsTrue(ae.Message.Contains(MsalErrorMessage.RedirectUriContainsFragment));
            }
        }

        [TestMethod]
        public void VerifyAuthorizationResultTest()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                MockInstanceDiscoveryAndOpenIdRequest(harness.HttpManager);

                MockWebUI webUi = new MockWebUI()
                {
                    MockResult = AuthorizationResult.FromUri(TestConstants.AuthorityHomeTenant + "?error=" + OAuth2Error.LoginRequired),
                };

                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    new TokenCache(harness.ServiceBundle, false),
                    extraQueryParameters: new Dictionary<string, string> { { "extra", "qp" } });
                parameters.RedirectUri = new Uri("some://uri");
                parameters.LoginHint = TestConstants.DisplayableId;
                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.ForceLogin,
                    ExtraScopesToConsent = TestConstants.s_scopeForAnotherResource.ToArray(),
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
                        MsalError.NoPromptFailedError,
                        ((MsalUiRequiredException)exc.InnerException).ErrorCode);

                    Assert.AreEqual(
                       UiRequiredExceptionClassification.PromptNeverFailed,
                       ((MsalUiRequiredException)exc.InnerException).Classification);
                }

                webUi = new MockWebUI
                {
                    MockResult = AuthorizationResult.FromUri(
                        TestConstants.AuthorityHomeTenant + "?error=invalid_request&error_description=some error description")
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
        [WorkItem(1418)] // test for bug https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1418
        public async Task VerifyAuthorizationResult_NoErrorDescription_Async()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                MockInstanceDiscoveryAndOpenIdRequest(harness.HttpManager);

                MockWebUI webUi = new MockWebUI()
                {
                    // error code and error description is empty string (not null)
                    MockResult = AuthorizationResult.FromUri(TestConstants.AuthorityHomeTenant + "?error=some_error&error_description= "),
                };

                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    new TokenCache(harness.ServiceBundle, false));

                InteractiveRequest request = new InteractiveRequest(
                    harness.ServiceBundle,
                    parameters,
                    new AcquireTokenInteractiveParameters(),
                    webUi);

                var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(
                    () => request.ExecuteAsync(CancellationToken.None))
                    .ConfigureAwait(false);

                Assert.AreEqual("some_error", ex.ErrorCode);
                Assert.AreEqual(InteractiveRequest.UnknownError, ex.Message);
            }
        }

        [TestMethod]
        public void DuplicateQueryParameterErrorTest()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityHomeTenant);

                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    new TokenCache(harness.ServiceBundle, false),
                    extraQueryParameters: new Dictionary<string, string> { { "extra", "qp" }, { "prompt", "login" } });
                parameters.RedirectUri = new Uri("some://uri");
                parameters.LoginHint = TestConstants.DisplayableId;
                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.ForceLogin,
                    ExtraScopesToConsent = TestConstants.s_scopeForAnotherResource.ToArray(),
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
                        MsalError.DuplicateQueryParameterError,
                        ((MsalException)exc.InnerException).ErrorCode);
                }
            }
        }

        private static void MockInstanceDiscoveryAndOpenIdRequest(MockHttpManager mockHttpManager)
        {
            mockHttpManager.AddInstanceDiscoveryMockHandler();
            mockHttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityHomeTenant);
        }

        private IBroker CreateMockBroker()
        {
            IBroker mockBroker = Substitute.For<IBroker>();
            mockBroker.CanInvokeBroker(null).ReturnsForAnyArgs(true);
            mockBroker.AcquireTokenUsingBrokerAsync(null).ReturnsForAnyArgs(TestConstants.CreateMsalTokenResponse());
            return mockBroker;
        }
    }
}
