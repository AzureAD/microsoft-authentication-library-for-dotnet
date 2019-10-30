// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class AcquireTokenSilentTests : TestBase
    {
        [TestMethod]
        public async Task NullAccount_EmptyLoginHintAsync()
        {
            IPublicClientApplication app = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithTelemetry(new TraceTelemetryConfig())
                .Build();

            await AssertException.TaskThrowsAsync<ArgumentNullException>(
             () => app.AcquireTokenSilent(TestConstants.s_scope.ToArray(), (string)null).ExecuteAsync()).ConfigureAwait(false);

            var ex = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(
              () => app.AcquireTokenSilent(TestConstants.s_scope.ToArray(), (IAccount)null).ExecuteAsync()).ConfigureAwait(false);
            Assert.AreEqual(MsalError.UserNullError, ex.ErrorCode);
            Assert.AreEqual(UiRequiredExceptionClassification.AcquireTokenSilentFailed, ex.Classification);

        }

        [TestMethod]
        public async Task AcquireTokenSilentScopeAndEmptyCacheTestAsync()
        {
            var receiver = new MyReceiver();
            PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                        .WithTelemetry(receiver.HandleTelemetryEvents)
                                                                        .BuildConcrete();

            try
            {
                AuthenticationResult result = await app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException exc)
            {
                Assert.AreEqual(MsalError.NoTokensFoundError, exc.ErrorCode);
                Assert.AreEqual(UiRequiredExceptionClassification.AcquireTokenSilentFailed, exc.Classification);
            }

            Assert.IsNotNull(
                receiver.EventsReceived.Find(
                    anEvent => // Expect finding such an event
                        anEvent[EventBase.EventNameKey].EndsWith("api_event") &&
                        anEvent[MsalTelemetryBlobEventNames.ApiIdConstStrKey] == "1007" &&
                        anEvent[ApiEvent.WasSuccessfulKey] == "false" &&
                        anEvent[ApiEvent.ApiErrorCodeKey] == "no_tokens_found"));
        }

        [TestMethod]
        public void AcquireTokenSilentScopeAndUserOverloadWithNoMatchingScopesInCacheTest()
        {
            // this test ensures that the API can
            // get authority (if unique) from the cache entries where scope does not match.
            // it should only happen for case where no authority is passed.

            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();
                var tokenCacheHelper = new TokenCacheHelper();
                tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);
                app.UserTokenCacheInternal.Accessor.DeleteAccessToken(
                    new MsalAccessTokenCacheKey(
                        TestConstants.ProductionPrefNetworkEnvironment,
                        TestConstants.Utid,
                        TestConstants.s_userIdentifier,
                        TestConstants.ClientId,
                        TestConstants.ScopeForAnotherResourceStr));
                var cacheAccess = app.UserTokenCache.RecordAccess();

                Task<AuthenticationResult> task = app
                    .AcquireTokenSilent(
                        TestConstants.s_scopeForAnotherResource.ToArray(),
                        new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(TestConstants.s_scopeForAnotherResource.AsSingleString(), result.Scopes.AsSingleString());
                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                cacheAccess.AssertAccessCounts(1, 0);
            }
        }

        [TestMethod]
        public void AcquireTokenSilentScopeAndUserOverloadDefaultAuthorityTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.AuthorityTestTenant), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                var tokenCacheHelper = new TokenCacheHelper();
                tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);

                app.UserTokenCacheInternal.Accessor.DeleteAccessToken(
                    new MsalAccessTokenCacheKey(
                        TestConstants.ProductionPrefNetworkEnvironment,
                        TestConstants.Utid,
                        TestConstants.s_userIdentifier,
                        TestConstants.ClientId,
                        TestConstants.ScopeForAnotherResourceStr));

                Task<AuthenticationResult> task = app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\CustomInstanceMetadata.json")]
        public void AcquireTokenSilentScopeAndUserOverloadTenantSpecificAuthorityTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.AuthorityGuestTenant), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                var tokenCacheHelper = new TokenCacheHelper();
                tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);
                app.UserTokenCacheInternal.Accessor.DeleteAccessToken(
                    new MsalAccessTokenCacheKey(
                        TestConstants.ProductionPrefNetworkEnvironment,
                        TestConstants.Utid,
                        TestConstants.s_userIdentifier,
                        TestConstants.ClientId,
                        TestConstants.ScopeForAnotherResourceStr));

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityGuestTenant);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            TestConstants.UniqueId,
                            TestConstants.DisplayableId,
                            TestConstants.s_scope.ToArray())
                    });

                Task<AuthenticationResult> task = app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());
            }
        }

        [TestMethod]
        public void AcquireTokenSilentCacheOnlyLookupTest()
        {
            RunAcquireTokenSilentCacheOnlyTest(
                TestConstants.AuthorityTestTenant,
                expectNetworkDiscovery: false);  // MSAL known authority

            RunAcquireTokenSilentCacheOnlyTest(
                 TestConstants.AuthorityWindowsNet,
                 expectNetworkDiscovery: false);  // MSAL known authority

            RunAcquireTokenSilentCacheOnlyTest(
                TestConstants.AuthorityNotKnownTenanted,
                expectNetworkDiscovery: true);  // not known authority

        }

        private void RunAcquireTokenSilentCacheOnlyTest(string authority, bool expectNetworkDiscovery)
        {
            var receiver = new MyReceiver();
            using (MockHttpAndServiceBundle testHarness = base.CreateTestHarness())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(authority, true)
                                                                            .WithHttpManager(testHarness.HttpManager)
                                                                            .WithTelemetry(receiver.HandleTelemetryEvents)
                                                                            .BuildConcrete();

                var tokenCacheHelper = new TokenCacheHelper();
                tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);

                app.UserTokenCacheInternal.Accessor.DeleteAccessToken(new MsalAccessTokenCacheKey(
                    TestConstants.ProductionPrefNetworkEnvironment,
                    TestConstants.Utid,
                    TestConstants.s_userIdentifier,
                    TestConstants.ClientId,
                    TestConstants.ScopeForAnotherResourceStr));

                if (expectNetworkDiscovery)
                {
                    string host = new Uri(authority).Host;
                    string discoveryHost = KnownMetadataProvider.IsKnownEnvironment(host)
                                               ? host
                                               : AadAuthority.DefaultTrustedHost;

                    string discoveryEndpoint = $"https://{discoveryHost}/common/discovery/instance";

                    var jsonResponse = TestConstants.DiscoveryJsonResponse.Replace("login.microsoft.com", host);
                    testHarness.HttpManager.AddMockHandler(
                        MockHelpers.CreateInstanceDiscoveryMockHandler(discoveryEndpoint, jsonResponse));
                }

                Task<AuthenticationResult> task = app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
                    .WithAuthority(app.Authority, false)
                    .WithForceRefresh(false)
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());

                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
                Assert.IsNotNull(receiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                    anEvent[EventBase.EventNameKey].EndsWith("api_event") && anEvent[ApiEvent.WasSuccessfulKey] == "true"
                    && anEvent[MsalTelemetryBlobEventNames.ApiIdConstStrKey] == "1007"));
            }
        }

        [TestMethod]
        public async Task AcquireTokenSilent_LoginHintAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.AuthorityTestTenant), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();
                var tokenCacheHelper = new TokenCacheHelper();
                tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);
                var cacheAccess = app.UserTokenCache.RecordAccess();

                AuthenticationResult result = await app.AcquireTokenSilent(
                    TestConstants.s_scope.ToArray(),
                    TestConstants.DisplayableId)
                    .WithAuthority(app.Authority, false)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());
                cacheAccess.AssertAccessCounts(1, 0);
            }
        }

        [TestMethod]
        public async Task AcquireTokenSilent_LoginHint_NoAccountAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.AuthorityTestTenant), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();
                var tokenCacheHelper = new TokenCacheHelper();
                tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);

                var exception = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(() => app.AcquireTokenSilent(
                    TestConstants.s_scope.ToArray(),
                    "other_login_hint@contoso.com")
                    .WithAuthority(app.Authority, false)
                    .ExecuteAsync()).ConfigureAwait(false);

                Assert.AreEqual(MsalError.NoAccountForLoginHint, exception.ErrorCode);
                Assert.AreEqual(UiRequiredExceptionClassification.AcquireTokenSilentFailed, exception.Classification);
            }
        }

        [TestMethod]
        public async Task AcquireTokenSilent_LoginHint_MultipleAccountsAsync()
        {
            var receiver = new MyReceiver();
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(TestConstants.AuthorityTestTenant), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(receiver.HandleTelemetryEvents)
                                                                            .BuildConcrete();

                var tokenCacheHelper = new TokenCacheHelper();
                var cacheAccess = app.UserTokenCache.RecordAccess();

                tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, "uid1", "utid");
                tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, "uid2", "utid");

                var exception = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(async () => await app.AcquireTokenSilent(
                    TestConstants.s_scope.ToArray(),
                    TestConstants.DisplayableId)
                    .WithAuthority(app.Authority, false)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.AreEqual(MsalError.MultipleAccountsForLoginHint, exception.ErrorCode);
                Assert.AreEqual(UiRequiredExceptionClassification.AcquireTokenSilentFailed, exception.Classification);
                cacheAccess.AssertAccessCounts(1, 0);
            }
        }

       

        [TestMethod]
        [TestCategory("Regression")]
        [WorkItem(695)] // Fix for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/695
        public void AcquireTokenSilentForceRefreshTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                var tokenCacheHelper = new TokenCacheHelper();
                tokenCacheHelper.PopulateCacheWithOneAccessToken(app.UserTokenCacheInternal.Accessor);
                var cacheAccess = app.UserTokenCache.RecordAccess();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityUtidTenant);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            TestConstants.UniqueId,
                            TestConstants.DisplayableId,
                            TestConstants.s_scope.ToArray())
                    });

                Task<AuthenticationResult> task = app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
                    .WithForceRefresh(true)
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(TestConstants.s_scope.ToArray().AsSingleString(), result.Scopes.AsSingleString());

                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
                cacheAccess.AssertAccessCounts(1, 1);
            }
        }

        [TestMethod]
        [TestCategory("Regression")]
        [WorkItem(695)] // Fix for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/695
        public void AcquireTokenSilentForceRefreshMultipleTenantsTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();
                var tokenCacheHelper = new TokenCacheHelper();
                tokenCacheHelper.PopulateCacheWithOneAccessToken(app.UserTokenCacheInternal.Accessor);

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityUtidTenant);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            TestConstants.UniqueId,
                            TestConstants.DisplayableId,
                            TestConstants.s_scope.ToArray())
                    });

                // ForceRefresh=true, so skip cache lookup of Access Token
                // Use refresh token to acquire a new Access Token
                Task<AuthenticationResult> task = app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithForceRefresh(true)
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(TestConstants.s_scope.ToArray().AsSingleString(), result.Scopes.AsSingleString());

                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityGuidTenant2);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            TestConstants.UniqueId,
                            TestConstants.DisplayableId,
                            TestConstants.s_scope.ToArray())
                    });

                Task<AuthenticationResult> task2 = app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
                    .WithAuthority(TestConstants.AuthorityGuidTenant2)
                    .WithForceRefresh(true)
                    .ExecuteAsync(CancellationToken.None);

                // Same user, scopes, clientId, but different authority
                // Should result in new AccessToken, but same refresh token
                AuthenticationResult result2 = task2.Result;
                Assert.IsNotNull(result2);
                Assert.AreEqual(TestConstants.DisplayableId, result2.Account.Username);
                Assert.AreEqual(TestConstants.s_scope.ToArray().AsSingleString(), result2.Scopes.AsSingleString());

                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityGuidTenant);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            TestConstants.UniqueId,
                            TestConstants.DisplayableId,
                            TestConstants.s_scope.ToArray())
                    });

                // Same user, scopes, clientId, but different authority
                // Should result in new AccessToken, but same refresh token
                Task<AuthenticationResult> task3 = app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
                    .WithAuthority(TestConstants.AuthorityGuidTenant)
                    .WithForceRefresh(true)
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result3 = task3.Result;
                Assert.IsNotNull(result3);
                Assert.AreEqual(TestConstants.DisplayableId, result3.Account.Username);
                Assert.AreEqual(TestConstants.s_scope.ToArray().AsSingleString(), result3.Scopes.AsSingleString());

                Assert.AreEqual(3, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                // Use same authority as above, number of access tokens should remain constant
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            TestConstants.UniqueId,
                            TestConstants.DisplayableId,
                            TestConstants.s_scope.ToArray())
                    });

                Task<AuthenticationResult> task4 = app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
                    .WithAuthority(TestConstants.AuthorityGuidTenant)
                    .WithForceRefresh(true)
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result4 = task4.Result;
                Assert.IsNotNull(result4);
                Assert.AreEqual(TestConstants.DisplayableId, result4.Account.Username);
                Assert.AreEqual(TestConstants.s_scope.ToArray().AsSingleString(), result4.Scopes.AsSingleString());

                Assert.AreEqual(3, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
            }
        }

        [TestMethod]
        [TestCategory("Regression")]
        [WorkItem(695)] // Fix for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/695
        public void AcquireTokenSilentForceRefreshFalseMultipleTenantsTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                // PopulateCache() creates two access tokens
                var tokenCacheHelper = new TokenCacheHelper();
                tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);

                Task<AuthenticationResult> task = app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithForceRefresh(false)
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(TestConstants.s_scope.ToArray().AsSingleString(), result.Scopes.AsSingleString());
                Assert.AreEqual(0, httpManager.QueueSize);

                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityGuidTenant2);
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            TestConstants.UniqueId,
                            TestConstants.DisplayableId,
                            TestConstants.s_scope.ToArray())
                    });

                // Same user, scopes, clientId, but different authority
                // Should result in new AccessToken, but same refresh token
                Task<AuthenticationResult> task2 = app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
                    .WithAuthority(TestConstants.AuthorityGuidTenant2)                    
                    .WithForceRefresh(false)
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result2 = task2.Result;
                Assert.IsNotNull(result2);
                Assert.AreEqual(TestConstants.DisplayableId, result2.Account.Username);
                Assert.AreEqual(TestConstants.s_scope.ToArray().AsSingleString(), result2.Scopes.AsSingleString());

                Assert.AreEqual(3, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityGuidTenant);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            TestConstants.UniqueId,
                            TestConstants.DisplayableId,
                            TestConstants.s_scope.ToArray())
                    });

                // Same user, scopes, clientId, but different authority
                // Should result in new AccessToken, but same refresh token
                Task<AuthenticationResult> task3 = app
                    .AcquireTokenSilent(
                        TestConstants.s_scope.ToArray(),
                        new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
                    .WithAuthority(TestConstants.AuthorityGuidTenant)
                    .WithForceRefresh(false)
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result3 = task3.Result;
                Assert.IsNotNull(result3);
                Assert.AreEqual(TestConstants.DisplayableId, result3.Account.Username);
                Assert.AreEqual(TestConstants.s_scope.ToArray().AsSingleString(), result3.Scopes.AsSingleString());

                Assert.AreEqual(4, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
            }
        }

        [TestMethod]
        public void AcquireTokenSilentServiceErrorTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityUtidTenant);

                //populate cache
                var tokenCacheHelper = new TokenCacheHelper();
                tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage()
                    });
                try
                {
                    Task<AuthenticationResult> task = app
                        .AcquireTokenSilent(
                            TestConstants.s_cacheMissScope,
                            new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
                        .WithAuthority(app.Authority)
                        .WithForceRefresh(false)
                        .ExecuteAsync(CancellationToken.None);

                    AuthenticationResult result = task.Result;
                    Assert.Fail("MsalUiRequiredException was expected");
                }
                catch (AggregateException ex)
                {
                    Assert.IsNotNull(ex.InnerException);
                    Assert.IsTrue(ex.InnerException is MsalUiRequiredException);
                    var msalExc = (MsalUiRequiredException)ex.InnerException;
                    Assert.AreEqual(msalExc.ErrorCode, MsalError.InvalidGrantError);
                }
            }
        }

        #region Tests around tenant ID
        [TestMethod]
        [TestCategory("Regression")]
        [WorkItem(1456)] // Fix for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1456
        public async Task AcquireTokenSilent_OverrideWithCommon_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(ClientApplicationBase.DefaultAuthority)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();

                var tokenCacheHelper = new TokenCacheHelper();
                tokenCacheHelper.PopulateCacheWithOneAccessToken(app.UserTokenCacheInternal.Accessor);
                var cacheAccess = app.UserTokenCache.RecordAccess();

                var acc = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();

                AuthenticationResult result = await app
                    .AcquireTokenSilent(TestConstants.s_scope, acc)
                    .WithAuthority(ClientApplicationBase.DefaultAuthority) // this override should do nothing, it's mean to specify a tenant id
                    .ExecuteAsync().ConfigureAwait(false);
            }
        }

        #endregion
    }
}
