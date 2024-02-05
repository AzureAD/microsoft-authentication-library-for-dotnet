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
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
                AuthenticationRequestParameters requestParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    new TokenCache(harness.ServiceBundle, false));

                var interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.SelectAccount,
                    ExtraScopesToConsent = TestConstants.s_scopeForAnotherResource.ToArray(),
                };

                AssertException.Throws<ArgumentNullException>(() =>
                    new InteractiveRequest(null, interactiveParameters));
                AssertException.Throws<ArgumentNullException>(() =>
                    new InteractiveRequest(requestParams, null));
            }
        }

        [TestMethod]
        public async Task WithExtraQueryParamsAndClaimsAsync()
        {
            IDictionary<string, string> extraQueryParamsAndClaims =
                TestConstants.ExtraQueryParameters.ToDictionary(e => e.Key, e => e.Value);
            extraQueryParamsAndClaims.Add(OAuth2Parameter.Claims, TestConstants.Claims);

            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                var cache = new TokenCache(harness.ServiceBundle, false);

                var ui = new MockWebUI()
                {
                    MockResult = AuthorizationResult.FromUri(TestConstants.AuthorityHomeTenant + "?code=some-code"),
                    QueryParamsToValidate = TestConstants.ExtraQueryParameters
                };
                MsalMockHelpers.ConfigureMockWebUI(harness.ServiceBundle, ui);

                MockInstanceDiscoveryAndOpenIdRequest(harness.HttpManager);

                var tokenResponseHandler = new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Post,
                    ExpectedQueryParams = TestConstants.ExtraQueryParameters,
                    ExpectedPostData = new Dictionary<string, string>()
                        { {OAuth2Parameter.Claims,  TestConstants.Claims } },
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                };
                harness.HttpManager.AddMockHandler(tokenResponseHandler);

                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    cache,
                    extraQueryParameters: TestConstants.ExtraQueryParameters,
                    claims: TestConstants.Claims);

                parameters.RedirectUri = new Uri("some://uri");
                parameters.LoginHint = TestConstants.DisplayableId;

                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.SelectAccount,
                    ExtraScopesToConsent = TestConstants.s_scopeForAnotherResource.ToArray(),
                };

                var request = new InteractiveRequest(
                    parameters,
                    interactiveParameters);

                AuthenticationResult result = await request.RunAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(1, ((ITokenCacheInternal)cache).Accessor.GetAllRefreshTokens().Count);
                Assert.AreEqual(1, ((ITokenCacheInternal)cache).Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(result.AccessToken, "some-access-token");
            }
        }

        [TestMethod]
        public async Task NoCacheLookupAsync()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                TokenCache cache = new TokenCache(harness.ServiceBundle, false);
                string clientInfo = MockHelpers.CreateClientInfo();
                string homeAccountId = ClientInfo.CreateFromJson(clientInfo).ToAccountIdentifier();
                MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.ClientId,
                    TestConstants.s_scope.AsSingleString(),
                    TestConstants.Utid,
                    null,
                    new DateTimeOffset(DateTime.UtcNow),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3599)),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(7200)),
                    clientInfo,
                    homeAccountId);

                string atKey = atItem.CacheKey;
                atItem.Secret = atKey;
                ((ITokenCacheInternal)cache).Accessor.SaveAccessToken(atItem);

                MockWebUI ui = new MockWebUI()
                {
                    MockResult = AuthorizationResult.FromUri(TestConstants.AuthorityHomeTenant + "?code=some-code")
                };

                MsalMockHelpers.ConfigureMockWebUI(harness.ServiceBundle, ui);

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
                    parameters,
                    interactiveParameters);

                AuthenticationResult result = await request.RunAsync().ConfigureAwait(false);

                Assert.AreEqual(1, ((ITokenCacheInternal)cache).Accessor.GetAllRefreshTokens().Count);
                Assert.AreEqual(2, ((ITokenCacheInternal)cache).Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(result.AccessToken, "some-access-token");
            }
        }

        [TestMethod]
        public async Task RedirectUriContainsFragmentErrorTestAsync()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                MockInstanceDiscoveryAndOpenIdRequest(harness.HttpManager);

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

                var request = new InteractiveRequest(
                    parameters,
                    interactiveParameters);
                var ex = await AssertException.TaskThrowsAsync<ArgumentException>
                    (() => request.RunAsync()).ConfigureAwait(false);

                Assert.IsTrue(ex.Message.Contains(MsalErrorMessage.RedirectUriContainsFragment));
            }
        }

        [TestMethod]
        public async Task VerifyAuthorizationResultTestAsync()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                MockInstanceDiscoveryAndOpenIdRequest(harness.HttpManager);

                MockWebUI webUi = new MockWebUI()
                {
                    MockResult = AuthorizationResult.FromUri(TestConstants.AuthorityHomeTenant + "?error=" + OAuth2Error.LoginRequired),
                };
                MsalMockHelpers.ConfigureMockWebUI(harness.ServiceBundle, webUi);

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
                    parameters,
                    interactiveParameters);

                var ex = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(
                    () => request.RunAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(
                    MsalError.NoPromptFailedError,
                    ex.ErrorCode);

                Assert.AreEqual(
                   UiRequiredExceptionClassification.PromptNeverFailed,
                   ex.Classification);

                webUi = new MockWebUI
                {
                    MockResult = AuthorizationResult.FromUri(
                        TestConstants.AuthorityHomeTenant + "?error=invalid_request&error_description=some error description")
                };
                MsalMockHelpers.ConfigureMockWebUI(harness.ServiceBundle, webUi);

                request = new InteractiveRequest(
                    parameters,
                    interactiveParameters);

                var ex2 = await AssertException.TaskThrowsAsync<MsalServiceException>(
                     () => request.RunAsync())
                     .ConfigureAwait(false);

                Assert.AreEqual("invalid_request", ex2.ErrorCode);
                Assert.AreEqual("some error description", ex2.Message);
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
                MsalMockHelpers.ConfigureMockWebUI(harness.ServiceBundle, webUi);

                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    new TokenCache(harness.ServiceBundle, false));

                var request = new InteractiveRequest(
                    parameters,
                    new AcquireTokenInteractiveParameters());

                var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(
                    () => request.RunAsync(CancellationToken.None))
                    .ConfigureAwait(false);

                Assert.AreEqual("some_error", ex.ErrorCode);
                Assert.AreEqual("Unknown error", ex.Message);
            }
        }

        [TestMethod]
        public void DuplicateQueryParameterErrorTest()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

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

                var request = new InteractiveRequest(
                    parameters,
                    interactiveParameters);
                MsalMockHelpers.ConfigureMockWebUI(harness.ServiceBundle, new MockWebUI());

                try
                {
                    request.RunAsync().Wait();
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

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task WithMultiCloudSupportEnabledAsync(bool multiCloudSupportEnabled)
        {
            var expectedQueryParams = TestConstants.ExtraQueryParameters;
            var authorizationResultUri = TestConstants.AuthorityHomeTenant + "?code=some-code";

            if (multiCloudSupportEnabled)
            {
                expectedQueryParams.Add("instance_aware", "true");
                authorizationResultUri += "&cloud_instance_name=microsoftonline.us&cloud_instance_host_name=login.microsoftonline.us";
            }

            using (MockHttpAndServiceBundle harness = CreateTestHarness(isMultiCloudSupportEnabled: multiCloudSupportEnabled))
            {
                var cache = new TokenCache(harness.ServiceBundle, false);

                var ui = new MockWebUI()
                {
                    MockResult = AuthorizationResult.FromUri(authorizationResultUri),
                    QueryParamsToValidate = expectedQueryParams
                };
                MsalMockHelpers.ConfigureMockWebUI(harness.ServiceBundle, ui);

                MockInstanceDiscoveryAndOpenIdRequest(harness.HttpManager);

                var tokenResponseHandler = new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Post,
                    ExpectedQueryParams = expectedQueryParams,
                    ExpectedPostData = new Dictionary<string, string>()
                        { {OAuth2Parameter.Claims,  TestConstants.Claims } },
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                };
                harness.HttpManager.AddMockHandler(tokenResponseHandler);

                AuthenticationRequestParameters parameters = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    cache,
                    extraQueryParameters: TestConstants.ExtraQueryParameters,
                    claims: TestConstants.Claims);

                parameters.RedirectUri = new Uri("some://uri");
                parameters.LoginHint = TestConstants.DisplayableId;

                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters
                {
                    Prompt = Prompt.SelectAccount,
                    ExtraScopesToConsent = TestConstants.s_scopeForAnotherResource.ToArray(),
                };

                var request = new InteractiveRequest(
                    parameters,
                    interactiveParameters);

                AuthenticationResult result = await request.RunAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);

                if (multiCloudSupportEnabled)
                    Assert.AreEqual("https://login.microsoftonline.us/home/oauth2/v2.0/token", result.AuthenticationResultMetadata.TokenEndpoint);
                else
                    Assert.AreEqual("https://login.microsoftonline.com/home/oauth2/v2.0/token", result.AuthenticationResultMetadata.TokenEndpoint);
                Assert.AreEqual(1, ((ITokenCacheInternal)cache).Accessor.GetAllRefreshTokens().Count);
                Assert.AreEqual(1, ((ITokenCacheInternal)cache).Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(result.AccessToken, "some-access-token");
            }
        }

        private static void MockInstanceDiscoveryAndOpenIdRequest(MockHttpManager mockHttpManager)
        {
            mockHttpManager.AddInstanceDiscoveryMockHandler();
        }

    }
}
