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

using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
#if DESKTOP
    [TestClass]
    public class FociTests
    {
        private enum ServerTokenResponse
        {
            NonFociToken,
            FociToken,
            ErrorClientMismatch,
            OtherError
        }

        private string _inMemoryCache;
        private PublicClientApplication _appA;
        private PublicClientApplication _appB;
        private MockHttpAndServiceBundle _harness;
        private bool _instanceAndEndpointRequestPerformed = false;

        [TestInitialize]
        public void Init()
        {
            TestCommon.ResetInternalStaticCaches();
            _inMemoryCache = "{}";
            _instanceAndEndpointRequestPerformed = false;
        }

        /// <summary>
        /// A and B apps part of the family. A acquires a token interactively. B can now acquire a token silently.
        /// </summary>
        [TestMethod]
        public async Task FociHappyPathAsync()
        {
            // Arrange
            using (_harness = new MockHttpAndServiceBundle())
            {
                InitApps();

                // Act
                await InteractiveAsync(_appA, ServerTokenResponse.FociToken).ConfigureAwait(false);
                await SilentAsync(_appB, ServerTokenResponse.FociToken).ConfigureAwait(false);

                // Assert
                await AssertAccountsAsync().ConfigureAwait(false);

                // Make sure smth reloads the cache before using the Accessor from the other app (GetAccounts will)
                Assert.AreEqual(2, _appA.UserTokenCacheInternal.Accessor.GetAllAppMetadata().Count());
                Assert.IsTrue(_appA.UserTokenCacheInternal.Accessor.GetAllAppMetadata().All(am => am.FamilyId == "1"));
                Assert.AreEqual("1", _appA.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single().FamilyId);
            }
        }

        /// <summary>
        /// A is part of the family, B is not. B fails gracefully trying to get a token silently
        /// </summary>
        [TestMethod]
        public async Task FociAndNonFociAppsCoexistAsync()
        {
            using (_harness = new MockHttpAndServiceBundle())
            {
                InitApps();

                // Act
                await InteractiveAsync(_appA, ServerTokenResponse.FociToken).ConfigureAwait(false);

                // B cannot acquire a token interactivelty, but will try to use FRT
                var ex = AssertException.TaskThrows<MsalUiRequiredException>(() => SilentAsync(_appB, ServerTokenResponse.ErrorClientMismatch));
                Assert.AreEqual(MsalError.NoTokensFoundError, ex.ErrorCode);

                // B can resume acquiring tokens silently via the normal RT, after a non
                await InteractiveAsync(_appB, ServerTokenResponse.NonFociToken).ConfigureAwait(false);
                await SilentAsync(_appB, ServerTokenResponse.NonFociToken).ConfigureAwait(false);

                // Assert
                await AssertAccountsAsync().ConfigureAwait(false);
                AssertAppHasRT(_appB);
                AssertFRTExists();
            }
        }

        /// <summary>
        /// A and B are in the family. When B tries to refresh the FRT, an error occurs (e.g. MFA required).
        /// This error has to be surfaced. This is a different scenario than receiving "client_mismatch" error
        /// </summary>
        [TestMethod]
        [WorkItem(1067)] // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1067
        public async Task FociDoesNotHideRTRefreshErrorsAsync()
        {
            using (_harness = new MockHttpAndServiceBundle())
            {
                InitApps();

                // Act
                await InteractiveAsync(_appA, ServerTokenResponse.FociToken).ConfigureAwait(false);

                // B cannot acquire a token interactivelty, but will try to use FRT
                var ex = AssertException.TaskThrows<MsalUiRequiredException>(() => SilentAsync(_appB, ServerTokenResponse.OtherError));
                Assert.AreEqual(MsalError.InvalidGrantError, ex.ErrorCode);
                Assert.IsTrue(!String.IsNullOrEmpty(ex.CorrelationId));

                // B performs interactive auth and everything goes back to normal - both A and B can silently sing in
                await InteractiveAsync(_appB, ServerTokenResponse.FociToken).ConfigureAwait(false);
                await SilentAsync(_appB, ServerTokenResponse.FociToken).ConfigureAwait(false);
                await SilentAsync(_appA, ServerTokenResponse.FociToken).ConfigureAwait(false);

                // Assert
                await AssertAccountsAsync().ConfigureAwait(false);
                AssertFRTExists();

            }
        }

        /// <summary>
        /// B is not part of the family but has an RT. B joins the family. B starts using the FRT.
        /// </summary>
        [TestMethod]
        public async Task FociAppWithTokensJoinsFamilyAsync()
        {
            using (_harness = new MockHttpAndServiceBundle())
            {
                InitApps();

                // A is in the family and has brought down an FRT. B has brought down it's RT.
                await InteractiveAsync(_appA, ServerTokenResponse.FociToken).ConfigureAwait(false);
                await InteractiveAsync(_appB, ServerTokenResponse.NonFociToken).ConfigureAwait(false);

                // B refreshes it's RT and gets an FRT response
                await SilentAsync(_appB, ServerTokenResponse.FociToken).ConfigureAwait(false);

                // remove B's RT
                var art = _appB.UserTokenCacheInternal.Accessor.GetAllRefreshTokens()
                    .Single(rt => _appB.AppConfig.ClientId == rt.ClientId && string.IsNullOrEmpty(rt.FamilyId));
                _appB.UserTokenCacheInternal.Accessor.DeleteRefreshToken(art.GetKey());

                // B can still use the FRT 
                await SilentAsync(_appB, ServerTokenResponse.FociToken).ConfigureAwait(false);

                // Assert
                await AssertAccountsAsync().ConfigureAwait(false);
                AssertFRTExists();
            }
        }

        /// <summary>
        /// A and B apps part of the family. B leaves the family. B fails gracefully trying to get a token silently
        /// </summary>
        [TestMethod]
        public async Task FociAppLeavesFamilyAsync()
        {
            using (_harness = new MockHttpAndServiceBundle())
            {
                InitApps();

                // A and B are part of the family
                await InteractiveAsync(_appA, ServerTokenResponse.FociToken).ConfigureAwait(false);
                await SilentAsync(_appB, ServerTokenResponse.FociToken).ConfigureAwait(false);

                // B leaves the family -> STS will not refresh its token based on the FRT
                AssertException.TaskThrows<MsalUiRequiredException>(() => SilentAsync(_appB, ServerTokenResponse.ErrorClientMismatch));

                // B can resume acquiring tokens silently via the normal RT, after an interactive flow
                await InteractiveAsync(_appB, ServerTokenResponse.NonFociToken).ConfigureAwait(false);
                await SilentAsync(_appB, ServerTokenResponse.NonFociToken).ConfigureAwait(false);

                // Assert
                await AssertAccountsAsync().ConfigureAwait(false);

                AssertAppHasRT(_appB);
                AssertFRTExists();
            }
        }


        private void AssertAppMetadata(PublicClientApplication app, bool partOfFamily)
        {
            Assert.IsNotNull(
                app.UserTokenCacheInternal.Accessor.GetAllAppMetadata()
                   .Single(m => m.ClientId == app.AppConfig.ClientId &&
                                partOfFamily == !string.IsNullOrEmpty(m.FamilyId)));
        }

        private async Task SilentAsync(
            PublicClientApplication app,
            ServerTokenResponse serverTokenResponse)
        {
            var account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();

            // 2 network calls - one for endpoint discovery on the tenanted authority, one to refresh the token
            if (!_instanceAndEndpointRequestPerformed)
            {
                _instanceAndEndpointRequestPerformed = true;
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();

                _harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityUtidTenant);
            }

            _harness.HttpManager.AddMockHandler(
                new MockHttpMessageHandler()
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage =
                    IsError(serverTokenResponse) ?
                        MockHelpers.CreateInvalidGrantTokenResponseMessage(GetSubError(serverTokenResponse)) :
                        MockHelpers.CreateSuccessTokenResponseMessage(
                            MsalTestConstants.UniqueId,
                            MsalTestConstants.DisplayableId,
                            MsalTestConstants.Scope.ToArray(),
                            foci: serverTokenResponse == ServerTokenResponse.FociToken)
                });

            AuthenticationResult resultB = await app.AcquireTokenSilent(MsalTestConstants.Scope, account)
                .WithForceRefresh(true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(resultB.AccessToken);
            AssertAppMetadata(app, serverTokenResponse == ServerTokenResponse.FociToken);

        }

        private static string GetSubError(ServerTokenResponse response)
        {
            if (response == ServerTokenResponse.ErrorClientMismatch)
            {
                return "client_mismatch";
            }

            if (response == ServerTokenResponse.OtherError)
            {
                return null;
            }

            throw new InvalidOperationException("Test error: response type is not an error");
        }

        private static bool IsError(ServerTokenResponse response)
        {
            return response == ServerTokenResponse.ErrorClientMismatch ||
                response == ServerTokenResponse.OtherError;
        }

        private async Task InteractiveAsync(PublicClientApplication app, ServerTokenResponse serverTokenResponse)
        {
            if (serverTokenResponse == ServerTokenResponse.ErrorClientMismatch)
            {
                throw new NotImplementedException("test error");
            }

            if (!_instanceAndEndpointRequestPerformed)
            {
                _instanceAndEndpointRequestPerformed = true;

                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                _harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityUtidTenant);
            }

            MsalMockHelpers.ConfigureMockWebUI(
                app.ServiceBundle.PlatformProxy,
                new AuthorizationResult(AuthorizationStatus.Success, app.AppConfig.RedirectUri + "?code=some-code"));

            _harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                MsalTestConstants.AuthorityUtidTenant,
                foci: serverTokenResponse == ServerTokenResponse.FociToken);

            // Acquire token interactively for A
            AuthenticationResult result = await app.AcquireTokenInteractive(MsalTestConstants.Scope).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(result.Account);
            AssertAppMetadata(app, serverTokenResponse == ServerTokenResponse.FociToken);

        }

        private void InitApps()
        {

            _appA = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                                        .WithHttpManager(_harness.HttpManager)
                                                                        .WithAuthority(MsalTestConstants.AuthorityUtidTenant)
                                                                        .BuildConcrete();

            _appB = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId2)
                                                                        .WithHttpManager(_harness.HttpManager)
                                                                        .WithAuthority(MsalTestConstants.AuthorityUtidTenant)
                                                                        .BuildConcrete();
            ConfigureCacheSerialization(_appA);
            ConfigureCacheSerialization(_appB);
        }


        private void ConfigureCacheSerialization(IPublicClientApplication pca)
        {
            pca.UserTokenCache.SetBeforeAccess(notificationArgs =>
            {
                byte[] bytes = Encoding.UTF8.GetBytes(_inMemoryCache);
                notificationArgs.TokenCache.DeserializeMsalV3(bytes);
            });

            pca.UserTokenCache.SetAfterAccess(notificationArgs =>
            {
                if (notificationArgs.HasStateChanged)
                {
                    byte[] bytes = notificationArgs.TokenCache.SerializeMsalV3();
                    _inMemoryCache = Encoding.UTF8.GetString(bytes);
                }
            });
        }


        private async Task AssertAccountsAsync()
        {
            Assert.AreEqual(1, (await _appA.GetAccountsAsync().ConfigureAwait(false)).Count());
            Assert.AreEqual(1, (await _appB.GetAccountsAsync().ConfigureAwait(false)).Count());
        }


        private void AssertFRTExists()
        {
            Assert.IsTrue(_appA.UserTokenCacheInternal.Accessor.GetAllRefreshTokens()
            .Any(rt => !string.IsNullOrEmpty(rt.FamilyId)),
            "The FRT still exists");
        }

        private void AssertAppHasRT(PublicClientApplication app)
        {
            Assert.IsTrue(app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens()
                .Any(rt => rt.ClientId == _appB.AppConfig.ClientId && string.IsNullOrEmpty(rt.FamilyId)),
                 "App B has a normal RT associated");

        }
    }

#endif
}
