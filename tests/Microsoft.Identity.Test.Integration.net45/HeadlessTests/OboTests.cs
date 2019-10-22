// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class OboTests
    {
        private static readonly string[] s_scopes = { "User.Read" };
        private static readonly string[] s_oboServiceScope = { "api://23c64cd8-21e4-41dd-9756-ab9e2c23f58c/access_as_user" };
        const string PublicClientID = "be9b0186-7dfd-448a-a944-f771029105bf";
        const string OboConfidentialClientID = "23c64cd8-21e4-41dd-9756-ab9e2c23f58c";

        private static InMemoryTokenCache s_inMemoryTokenCache = new InMemoryTokenCache();
        private string _confidentialClientSecret;

        private readonly KeyVaultSecretsProvider _keyVault = new KeyVaultSecretsProvider();

        #region Test Hooks
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
            if (string.IsNullOrEmpty(_confidentialClientSecret))
            {
                _confidentialClientSecret = _keyVault.GetSecret(TestConstants.MsalOBOKeyVaultUri).Value;
            }
        }

        #endregion

        [TestMethod]
        public async Task OBO_WithCache_MultipleUsers_Async()
        {
            var aadUser1 = (await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false)).User;
            var aadUser2 = (await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV2, true).ConfigureAwait(false)).User;
            var adfsUser = (await LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019).ConfigureAwait(false)).User;

            await RunOnBehalfOfTestAsync(adfsUser, false).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(aadUser1, false).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(aadUser1, true).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(aadUser2, false).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(adfsUser, true).ConfigureAwait(false);
            await RunOnBehalfOfTestAsync(aadUser2, true).ConfigureAwait(false);
        }

        private async Task<IConfidentialClientApplication> RunOnBehalfOfTestAsync(LabUser user, bool silentCallShouldSucceed)
        {
            SecureString securePassword = new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword;
            AuthenticationResult authResult;

            var pca = PublicClientApplicationBuilder
                .Create(PublicClientID)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .Build();
            s_inMemoryTokenCache.Bind(pca.UserTokenCache);

            try
            {
                authResult = await pca
                    .AcquireTokenSilent(s_oboServiceScope, user.Upn)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                Assert.IsFalse(silentCallShouldSucceed, "ATS should have found a token, but it didn't");
                authResult = await pca
                    .AcquireTokenByUsernamePassword(s_oboServiceScope, user.Upn, securePassword)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }

            MsalAssert.AssertAuthResult(authResult, user);
            Assert.IsTrue(authResult.Scopes.Any(s => string.Equals(s, s_oboServiceScope.Single(), StringComparison.OrdinalIgnoreCase)));

            var cca = ConfidentialClientApplicationBuilder
                .Create(OboConfidentialClientID)
                .WithAuthority(new Uri("https://login.microsoftonline.com/" + authResult.TenantId), true)
                .WithClientSecret(_confidentialClientSecret)
                .Build();
            s_inMemoryTokenCache.Bind(cca.UserTokenCache);

            try
            {
                authResult = await cca
                  .AcquireTokenSilent(s_scopes, user.Upn)
                  .ExecuteAsync()
                  .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                Assert.IsFalse(silentCallShouldSucceed, "ATS should have found a token, but it didn't");

                authResult = await cca.AcquireTokenOnBehalfOf(s_scopes, new UserAssertion(authResult.AccessToken))
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
            }

            MsalAssert.AssertAuthResult(authResult, user);
            Assert.IsTrue(authResult.Scopes.Any(s => string.Equals(s, s_scopes.Single(), StringComparison.OrdinalIgnoreCase)));

            return cca;
        }
    }
}
