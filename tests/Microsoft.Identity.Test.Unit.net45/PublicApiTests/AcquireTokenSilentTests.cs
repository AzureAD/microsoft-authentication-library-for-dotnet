// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class AcquireTokenSilentTests
    {
        private TokenCacheHelper _tokenCacheHelper;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();

            _tokenCacheHelper = new TokenCacheHelper();
        }

        [TestMethod]
        public async Task NullAccount_EmptyLoginHintAsync()
        {
            IPublicClientApplication app = PublicClientApplicationBuilder
                .Create(MsalTestConstants.ClientId)
                .WithTelemetry(new TraceTelemetryConfig())
                .Build();

            await AssertException.TaskThrowsAsync<ArgumentNullException>(
             () => app.AcquireTokenSilent(MsalTestConstants.Scope.ToArray(), (string)null).ExecuteAsync()).ConfigureAwait(false);

            var ex = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(
              () => app.AcquireTokenSilent(MsalTestConstants.Scope.ToArray(), (IAccount)null).ExecuteAsync()).ConfigureAwait(false);
            Assert.AreEqual(MsalError.UserNullError, ex.ErrorCode);
        }

        [TestMethod]
        public async Task AcquireTokenSilentScopeAndEmptyCacheTestAsync()
        {
            var receiver = new MyReceiver();
            PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                        .WithTelemetry(receiver.HandleTelemetryEvents)
                                                                        .BuildConcrete();

            try
            {
                AuthenticationResult result = await app
                    .AcquireTokenSilent(
                        MsalTestConstants.Scope.ToArray(),
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null))
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException exc)
            {
                Assert.AreEqual(MsalError.NoTokensFoundError, exc.ErrorCode);
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
                httpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                _tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);
                app.UserTokenCacheInternal.Accessor.DeleteAccessToken(
                    new MsalAccessTokenCacheKey(
                        MsalTestConstants.ProductionPrefNetworkEnvironment,
                        MsalTestConstants.Utid,
                        MsalTestConstants.UserIdentifier,
                        MsalTestConstants.ClientId,
                        MsalTestConstants.ScopeForAnotherResourceStr));

                Task<AuthenticationResult> task = app
                    .AcquireTokenSilent(
                        MsalTestConstants.ScopeForAnotherResource.ToArray(),
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null))
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.ScopeForAnotherResource.AsSingleString(), result.Scopes.AsSingleString());
                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        public void AcquireTokenSilentScopeAndUserOverloadDefaultAuthorityTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(MsalTestConstants.AuthorityTestTenant), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                _tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);
                app.UserTokenCacheInternal.Accessor.DeleteAccessToken(
                    new MsalAccessTokenCacheKey(
                        MsalTestConstants.ProductionPrefNetworkEnvironment,
                        MsalTestConstants.Utid,
                        MsalTestConstants.UserIdentifier,
                        MsalTestConstants.ClientId,
                        MsalTestConstants.ScopeForAnotherResourceStr));

                httpManager.AddInstanceDiscoveryMockHandler();

                Task<AuthenticationResult> task = app
                    .AcquireTokenSilent(
                        MsalTestConstants.Scope.ToArray(),
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null))
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());
            }
        }

        [TestMethod]
        public void AcquireTokenSilentScopeAndUserOverloadTenantSpecificAuthorityTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(MsalTestConstants.AuthorityGuestTenant), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();
                _tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);
                app.UserTokenCacheInternal.Accessor.DeleteAccessToken(
                    new MsalAccessTokenCacheKey(
                        MsalTestConstants.ProductionPrefNetworkEnvironment,
                        MsalTestConstants.Utid,
                        MsalTestConstants.UserIdentifier,
                        MsalTestConstants.ClientId,
                        MsalTestConstants.ScopeForAnotherResourceStr));

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityGuestTenant);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.UniqueId,
                            MsalTestConstants.DisplayableId,
                            MsalTestConstants.Scope.ToArray())
                    });

                Task<AuthenticationResult> task = app
                    .AcquireTokenSilent(
                        MsalTestConstants.Scope.ToArray(),
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null))
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());
            }
        }

        [TestMethod]
        public void AcquireTokenSilentCacheOnlyLookupTest()
        {
            var receiver = new MyReceiver();
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(MsalTestConstants.AuthorityTestTenant), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(receiver.HandleTelemetryEvents)
                                                                            .BuildConcrete();
                _tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);

                app.UserTokenCacheInternal.Accessor.DeleteAccessToken(new MsalAccessTokenCacheKey(
                    MsalTestConstants.ProductionPrefNetworkEnvironment,
                    MsalTestConstants.Utid,
                    MsalTestConstants.UserIdentifier,
                    MsalTestConstants.ClientId,
                    MsalTestConstants.ScopeForAnotherResourceStr));

                httpManager.AddInstanceDiscoveryMockHandler();

                Task<AuthenticationResult> task = app
                    .AcquireTokenSilent(
                        MsalTestConstants.Scope.ToArray(),
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null))
                    .WithAuthority(app.Authority, false)
                    .WithForceRefresh(false)
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());

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
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(MsalTestConstants.AuthorityTestTenant), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();
                _tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);

                httpManager.AddInstanceDiscoveryMockHandler();

                AuthenticationResult result = await app.AcquireTokenSilent(
                    MsalTestConstants.Scope.ToArray(),
                    MsalTestConstants.DisplayableId)
                    .WithAuthority(app.Authority, false)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());
            }
        }

        [TestMethod]
        public async Task AcquireTokenSilent_LoginHint_NoAccountAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(MsalTestConstants.AuthorityTestTenant), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();
                _tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);

                var exception = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(() => app.AcquireTokenSilent(
                    MsalTestConstants.Scope.ToArray(),
                    "other_login_hint@contoso.com")
                    .WithAuthority(app.Authority, false)
                    .ExecuteAsync()).ConfigureAwait(false);

                Assert.AreEqual(MsalError.NoAccountForLoginHint, exception.ErrorCode);
            }
        }

        [TestMethod]
        public async Task AcquireTokenSilent_LoginHint_MultipleAccountsAsync()
        {
            var receiver = new MyReceiver();
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(MsalTestConstants.AuthorityTestTenant), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(receiver.HandleTelemetryEvents)
                                                                            .BuildConcrete();

                _tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, "uid1", "utid");
                _tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor, "uid2", "utid");

                var exception = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(async () => await app.AcquireTokenSilent(
                    MsalTestConstants.Scope.ToArray(),
                    MsalTestConstants.DisplayableId)
                    .WithAuthority(app.Authority, false)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.AreEqual(MsalError.MultipleAccountsForLoginHint, exception.ErrorCode);
            }
        }



        [TestMethod]
        [TestCategory("Regression")]
        [WorkItem(695)] // Fix for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/695
        public void AcquireTokenSilentForceRefreshTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                _tokenCacheHelper.PopulateCacheWithOneAccessToken(app.UserTokenCacheInternal.Accessor);

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityUtidTenant);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.UniqueId,
                            MsalTestConstants.DisplayableId,
                            MsalTestConstants.Scope.ToArray())
                    });

                Task<AuthenticationResult> task = app
                    .AcquireTokenSilent(
                        MsalTestConstants.Scope.ToArray(),
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null))
                    .WithForceRefresh(true)
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result.Scopes.AsSingleString());

                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
            }
        }

        [TestMethod]
        [TestCategory("Regression")]
        [WorkItem(695)] // Fix for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/695
        public void AcquireTokenSilentForceRefreshMultipleTenantsTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .BuildConcrete();
                _tokenCacheHelper.PopulateCacheWithOneAccessToken(app.UserTokenCacheInternal.Accessor);

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.UniqueId,
                            MsalTestConstants.DisplayableId,
                            MsalTestConstants.Scope.ToArray())
                    });

                // ForceRefresh=true, so skip cache lookup of Access Token
                // Use refresh token to acquire a new Access Token
                Task<AuthenticationResult> task = app
                    .AcquireTokenSilent(
                        MsalTestConstants.Scope.ToArray(),
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null))
                    .WithAuthority(MsalTestConstants.AuthorityCommonTenant)
                    .WithForceRefresh(true)
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result.Scopes.AsSingleString());

                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityGuidTenant2);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.UniqueId,
                            MsalTestConstants.DisplayableId,
                            MsalTestConstants.Scope.ToArray())
                    });

                Task<AuthenticationResult> task2 = app
                    .AcquireTokenSilent(
                        MsalTestConstants.Scope.ToArray(),
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null))
                    .WithAuthority(MsalTestConstants.AuthorityGuidTenant2)
                    .WithForceRefresh(true)
                    .ExecuteAsync(CancellationToken.None);

                // Same user, scopes, clientId, but different authority
                // Should result in new AccessToken, but same refresh token
                AuthenticationResult result2 = task2.Result;
                Assert.IsNotNull(result2);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result2.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result2.Scopes.AsSingleString());

                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityGuidTenant);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.UniqueId,
                            MsalTestConstants.DisplayableId,
                            MsalTestConstants.Scope.ToArray())
                    });

                // Same user, scopes, clientId, but different authority
                // Should result in new AccessToken, but same refresh token
                Task<AuthenticationResult> task3 = app
                    .AcquireTokenSilent(
                        MsalTestConstants.Scope.ToArray(),
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null))
                    .WithAuthority(MsalTestConstants.AuthorityGuidTenant)
                    .WithForceRefresh(true)
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result3 = task3.Result;
                Assert.IsNotNull(result3);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result3.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result3.Scopes.AsSingleString());

                Assert.AreEqual(3, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                // Use same authority as above, number of access tokens should remain constant
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.UniqueId,
                            MsalTestConstants.DisplayableId,
                            MsalTestConstants.Scope.ToArray())
                    });

                Task<AuthenticationResult> task4 = app
                    .AcquireTokenSilent(
                        MsalTestConstants.Scope.ToArray(),
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null))
                    .WithAuthority(MsalTestConstants.AuthorityGuidTenant)
                    .WithForceRefresh(true)
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result4 = task4.Result;
                Assert.IsNotNull(result4);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result4.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result4.Scopes.AsSingleString());

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
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();

                // PopulateCache() creates two access tokens
                _tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.UniqueId,
                            MsalTestConstants.DisplayableId,
                            MsalTestConstants.Scope.ToArray())
                    });

                Task<AuthenticationResult> task = app
                    .AcquireTokenSilent(
                        MsalTestConstants.Scope.ToArray(),
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null))
                    .WithAuthority(MsalTestConstants.AuthorityCommonTenant)
                    .WithForceRefresh(false)
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result.Scopes.AsSingleString());

                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityGuidTenant2);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.UniqueId,
                            MsalTestConstants.DisplayableId,
                            MsalTestConstants.Scope.ToArray())
                    });

                // Same user, scopes, clientId, but different authority
                // Should result in new AccessToken, but same refresh token
                Task<AuthenticationResult> task2 = app
                    .AcquireTokenSilent(
                        MsalTestConstants.Scope.ToArray(),
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null))
                    .WithAuthority(MsalTestConstants.AuthorityGuidTenant2)
                    .WithForceRefresh(false)
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result2 = task2.Result;
                Assert.IsNotNull(result2);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result2.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result2.Scopes.AsSingleString());

                Assert.AreEqual(3, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityGuidTenant);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.UniqueId,
                            MsalTestConstants.DisplayableId,
                            MsalTestConstants.Scope.ToArray())
                    });

                // Same user, scopes, clientId, but different authority
                // Should result in new AccessToken, but same refresh token
                Task<AuthenticationResult> task3 = app
                    .AcquireTokenSilent(
                        MsalTestConstants.Scope.ToArray(),
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null))
                    .WithAuthority(MsalTestConstants.AuthorityGuidTenant)
                    .WithForceRefresh(false)
                    .ExecuteAsync(CancellationToken.None);

                AuthenticationResult result3 = task3.Result;
                Assert.IsNotNull(result3);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result3.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result3.Scopes.AsSingleString());

                Assert.AreEqual(4, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
            }
        }

        [TestMethod]
        public void AcquireTokenSilentServiceErrorTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                PublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(httpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);

                //populate cache
                _tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);

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
                            MsalTestConstants.CacheMissScope,
                            new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null))
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
    }
}
