//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------


using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net.Http;
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
            TestCommon.ResetStateAndInitMsal();

            _tokenCacheHelper = new TokenCacheHelper();
        }

        [TestMethod]
        public void NullAccount_EmptyLoginHint()
        {
            var receiver = new MyReceiver();
            IPublicClientApplication app = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                       .WithTelemetry(receiver.HandleTelemetryEvents)
                                                                       .Build();

            var ex = AssertException.TaskThrows<MsalUiRequiredException>(
                () => app.AcquireTokenSilentAsync(MsalTestConstants.Scope.ToArray(), (IAccount)null));
            Assert.AreEqual(MsalUiRequiredException.UserNullError, ex.ErrorCode);

            AssertException.TaskThrows<ArgumentNullException>(
             () => app.AcquireTokenSilent(MsalTestConstants.Scope.ToArray(), (string)null).ExecuteAsync());

            ex = AssertException.TaskThrows<MsalUiRequiredException>(
              () => app.AcquireTokenSilent(MsalTestConstants.Scope.ToArray(), (IAccount)null).ExecuteAsync());
            Assert.AreEqual(MsalUiRequiredException.UserNullError, ex.ErrorCode);
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
                AuthenticationResult result = await app.AcquireTokenSilentAsync(
                                                  MsalTestConstants.Scope.ToArray(),
                                                  new Account(
                                                      MsalTestConstants.UserIdentifier,
                                                      MsalTestConstants.DisplayableId,
                                                      null)).ConfigureAwait(false);
            }
            catch (MsalUiRequiredException exc)
            {
                Assert.AreEqual(MsalUiRequiredException.NoTokensFoundError, exc.ErrorCode);
            }

            Assert.IsNotNull(
                receiver.EventsReceived.Find(
                    anEvent => // Expect finding such an event
                        anEvent[EventBase.EventNameKey].EndsWith("api_event") && anEvent[ApiEvent.ApiIdKey] == "30" &&
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
                                                                            .BuildConcrete();

                _tokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);
                app.UserTokenCacheInternal.Accessor.DeleteAccessToken(
                    new MsalAccessTokenCacheKey(
                        MsalTestConstants.ProductionPrefNetworkEnvironment,
                        MsalTestConstants.Utid,
                        MsalTestConstants.UserIdentifier,
                        MsalTestConstants.ClientId,
                        MsalTestConstants.ScopeForAnotherResourceStr));

                Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(
                    MsalTestConstants.ScopeForAnotherResource.ToArray(),
                    new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null));

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.ScopeForAnotherResource.AsSingleString(), result.Scopes.AsSingleString());
                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.AccessTokenCount);
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

                Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(
                    MsalTestConstants.Scope.ToArray(),
                    new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null));

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

                Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(
                    MsalTestConstants.Scope.ToArray(),
                    new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null));
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

                Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(
                    MsalTestConstants.Scope.ToArray(),
                    new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null),
                    app.Authority,
                    false);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());

                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.AccessTokenCount);
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.RefreshTokenCount);

                Assert.IsNotNull(receiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                    anEvent[EventBase.EventNameKey].EndsWith("api_event") && anEvent[ApiEvent.WasSuccessfulKey] == "true"
                    && anEvent[ApiEvent.ApiIdKey] == "31"));
            }
        }

        [TestMethod]
        public async Task AcquireTokenSilent_LoginHintAsync()
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
        public void AcquireTokenSilent_LoginHint_NoAccount()
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

                var exception = AssertException.Throws<MsalUiRequiredException>(() => app.AcquireTokenSilent(
                    MsalTestConstants.Scope.ToArray(),
                    "other_login_hint@contoso.com")
                    .WithAuthority(app.Authority, false)
                    .ExecuteAsync());

                Assert.AreEqual(MsalUiRequiredException.NoAccountForLoginHint, exception.ErrorCode);
            }
        }

        [TestMethod]
        public void AcquireTokenSilent_LoginHint_MultipleAccounts()
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

                var exception = AssertException.Throws<MsalUiRequiredException>(() => app.AcquireTokenSilent(
                    MsalTestConstants.Scope.ToArray(),
                    MsalTestConstants.DisplayableId)
                    .WithAuthority(app.Authority, false)
                    .ExecuteAsync());

                Assert.AreEqual(MsalUiRequiredException.MultipleAccountsForLoginHint, exception.ErrorCode);
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

                Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(
                    MsalTestConstants.Scope.ToArray(),
                    new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null),
                    null,
                    true);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result.Scopes.AsSingleString());

                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.AccessTokenCount);
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.RefreshTokenCount);
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
                Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(
                    MsalTestConstants.Scope.ToArray(),
                    new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null),
                    MsalTestConstants.AuthorityCommonTenant,
                    true);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result.Scopes.AsSingleString());

                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.AccessTokenCount);
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.RefreshTokenCount);

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

                Task<AuthenticationResult> task2 = app.AcquireTokenSilentAsync(
                    MsalTestConstants.Scope.ToArray(),
                    new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null),
                    MsalTestConstants.AuthorityGuidTenant2,
                    true);

                // Same user, scopes, clientId, but different authority
                // Should result in new AccessToken, but same refresh token
                AuthenticationResult result2 = task2.Result;
                Assert.IsNotNull(result2);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result2.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result2.Scopes.AsSingleString());

                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.AccessTokenCount);
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.RefreshTokenCount);

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
                Task<AuthenticationResult> task3 = app.AcquireTokenSilentAsync(
                    MsalTestConstants.Scope.ToArray(),
                    new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null),
                    MsalTestConstants.AuthorityGuidTenant,
                    true);

                AuthenticationResult result3 = task3.Result;
                Assert.IsNotNull(result3);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result3.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result3.Scopes.AsSingleString());

                Assert.AreEqual(3, app.UserTokenCacheInternal.Accessor.AccessTokenCount);
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.RefreshTokenCount);

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

                Task<AuthenticationResult> task4 = app.AcquireTokenSilentAsync(
                    MsalTestConstants.Scope.ToArray(),
                    new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null),
                    MsalTestConstants.AuthorityGuidTenant,
                    true);

                AuthenticationResult result4 = task4.Result;
                Assert.IsNotNull(result4);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result4.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result4.Scopes.AsSingleString());

                Assert.AreEqual(3, app.UserTokenCacheInternal.Accessor.AccessTokenCount);
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.RefreshTokenCount);
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

                Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(
                    MsalTestConstants.Scope.ToArray(),
                    new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null),
                    MsalTestConstants.AuthorityCommonTenant,
                    false);

                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result.Scopes.AsSingleString());

                Assert.AreEqual(2, app.UserTokenCacheInternal.Accessor.AccessTokenCount);
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.RefreshTokenCount);

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
                Task<AuthenticationResult> task2 = app.AcquireTokenSilentAsync(
                    MsalTestConstants.Scope.ToArray(),
                    new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null),
                    MsalTestConstants.AuthorityGuidTenant2,
                    false);

                AuthenticationResult result2 = task2.Result;
                Assert.IsNotNull(result2);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result2.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result2.Scopes.AsSingleString());

                Assert.AreEqual(3, app.UserTokenCacheInternal.Accessor.AccessTokenCount);
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.RefreshTokenCount);

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
                Task<AuthenticationResult> task3 = app.AcquireTokenSilentAsync(
                    MsalTestConstants.Scope.ToArray(),
                    new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null),
                    MsalTestConstants.AuthorityGuidTenant,
                    false);

                AuthenticationResult result3 = task3.Result;
                Assert.IsNotNull(result3);
                Assert.AreEqual(MsalTestConstants.DisplayableId, result3.Account.Username);
                Assert.AreEqual(MsalTestConstants.Scope.ToArray().AsSingleString(), result3.Scopes.AsSingleString());

                Assert.AreEqual(4, app.UserTokenCacheInternal.Accessor.AccessTokenCount);
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.RefreshTokenCount);
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
                    Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(
                        MsalTestConstants.CacheMissScope,
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null),
                        app.Authority,
                        false);
                    AuthenticationResult result = task.Result;
                    Assert.Fail("MsalUiRequiredException was expected");
                }
                catch (AggregateException ex)
                {
                    Assert.IsNotNull(ex.InnerException);
                    Assert.IsTrue(ex.InnerException is MsalUiRequiredException);
                    var msalExc = (MsalUiRequiredException)ex.InnerException;
                    Assert.AreEqual(msalExc.ErrorCode, MsalUiRequiredException.InvalidGrantError);
                }
            }
        }
    }
}
