// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class OnBehalfOfServicePrincipalTests
    {
        //The following client ids are for applications that are within PPE
        private const string OBOClientPpeClientID = "9793041b-9078-4942-b1d2-babdc472cc0c";
        private const string OBOServicePpeClientID = "c84e9c32-0bc9-4a73-af05-9efe9982a322";
        private const string OBOServiceDownStreamApiPpeClientID = "23d08a1e-1249-4f7c-b5a5-cb11f29b6923";
        private const string PPEAuthenticationAuthority = "https://login.windows-ppe.net/94430a9c-83e9-4f08-bbb0-64fccd0661fc";

        /// <summary>
        /// Client -> Middletier -> RP
        /// This is OBO for SP without RT support.
        /// Currently this is supported only by 1p, i.e. Client (3P) -> Middletier (1p) -> RP (1p)
        /// </summary>
        /// <remarks>
        /// For details see https://aadwiki.windows-int.net/index.php?title=App_OBO_aka._Service_Principal_OBO, which explains
        /// the structure of the access token received from OBO.
        /// </remarks>
        [RunOn(TargetFrameworks.NetCore | TargetFrameworks.NetFx | TargetFrameworks.NetStandard)]
        public async Task NormalObo_TestAsync()
        {
            //An explanation of the OBO for service principal scenario can be found here https://aadwiki.windows-int.net/index.php?title=App_OBO_aka._Service_Principal_OBO

            var settings = ConfidentialAppSettings.GetSettings(Cloud.Public);
            var cert = settings.GetCertificate();

            IReadOnlyList<string> middleTierApiScopes = new List<string>() { OBOServicePpeClientID + "/.default" };
            IReadOnlyList<string> downstreamApiScopes = new List<string>() { OBOServiceDownStreamApiPpeClientID + "/.default" };

            var clientConfidentialApp = ConfidentialClientApplicationBuilder
                                    .Create(OBOClientPpeClientID)
                                    .WithAuthority(PPEAuthenticationAuthority)
                                    .WithCertificate(cert)
                                    .WithTestLogging()
                                    .Build();

            var authenticationResult = await clientConfidentialApp
                .AcquireTokenForClient(middleTierApiScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            string appToken = authenticationResult.AccessToken;
            var userAssertion = new UserAssertion(appToken);
            string atHash = userAssertion.AssertionHash;

            var middletierServiceApp = ConfidentialClientApplicationBuilder
                .Create(OBOServicePpeClientID)
                .WithAuthority(PPEAuthenticationAuthority)
                .WithCertificate(cert)
                .Build();
            var userCacheRecorder = middletierServiceApp.UserTokenCache.RecordAccess();

            authenticationResult = await middletierServiceApp
                .AcquireTokenOnBehalfOf(downstreamApiScopes, userAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(authenticationResult.AccessToken);
            Assert.IsFalse(userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, authenticationResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(
                userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheExpiry,
                "When writing the OBO token response, MSAL should ignore the RT and propose expiry");

            authenticationResult = await middletierServiceApp
                .AcquireTokenOnBehalfOf(downstreamApiScopes, userAssertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(authenticationResult.AccessToken);
            Assert.IsFalse(userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(atHash, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.Cache, authenticationResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNull(
                userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheExpiry,
                "The cache expiry is not set because the node did not change");
        }

        // Will need to be updated.
        // See https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3913
        [RunOn(TargetFrameworks.NetCore)]
        public async Task LongRunningObo_TestAsync()
        {
            //An explanation of the OBO for service principal scenario can be found here https://aadwiki.windows-int.net/index.php?title=App_OBO_aka._Service_Principal_OBO

            var settings = ConfidentialAppSettings.GetSettings(Cloud.Public);
            var cert = settings.GetCertificate();

            IReadOnlyList<string> middleTierApiScopes = new List<string>() { OBOServicePpeClientID + "/.default" };
            IReadOnlyList<string> downstreamApiScopes = new List<string>() { OBOServiceDownStreamApiPpeClientID + "/.default" };


            var clientConfidentialApp = ConfidentialClientApplicationBuilder
                                    .Create(OBOClientPpeClientID)
                                    .WithAuthority(PPEAuthenticationAuthority)
                                    .WithCertificate(cert)
                                    .WithTestLogging()
                                    .Build();

            var middletierServiceApp = ConfidentialClientApplicationBuilder
                                    .Create(OBOServicePpeClientID)
                                    .WithAuthority(PPEAuthenticationAuthority)
                                    .WithCertificate(cert)
                                    .BuildConcrete();
            var userCacheRecorder = middletierServiceApp.UserTokenCache.RecordAccess();

            Trace.WriteLine("1. Upstream client gets an app token");
            var authenticationResult = await clientConfidentialApp
                .AcquireTokenForClient(middleTierApiScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);
            string clientToken = authenticationResult.AccessToken;

            Trace.WriteLine("2. MidTier kicks off the long running process by getting an OBO token");
            string cacheKey = null;
            authenticationResult = await middletierServiceApp.
                InitiateLongRunningProcessInWebApi(downstreamApiScopes, clientToken, ref cacheKey)
                .ExecuteAsync().ConfigureAwait(false);

            Assert.IsNotNull(authenticationResult.AccessToken);
            Assert.IsFalse(userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(cacheKey, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, authenticationResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNull(
                userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheExpiry,
                "The cache expiry is not set because there is an RT in the cache");
            Assert.AreEqual(1, middletierServiceApp.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, middletierServiceApp.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
            Assert.AreEqual(1, middletierServiceApp.UserTokenCacheInternal.Accessor.GetAllIdTokens().Count);
            Assert.AreEqual(1, middletierServiceApp.UserTokenCacheInternal.Accessor.GetAllAccounts().Count);

            Trace.WriteLine("3. Later, mid-tier needs the token again, and one is in the cache");
            authenticationResult = await middletierServiceApp
                .AcquireTokenInLongRunningProcess(downstreamApiScopes, cacheKey)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, authenticationResult.AuthenticationResultMetadata.TokenSource);

            Trace.WriteLine("4. After the original token expires, the mid-tier needs a token again. RT will be used.");
            TokenCacheHelper.ExpireAllAccessTokens(middletierServiceApp.UserTokenCache as ITokenCacheInternal);

            authenticationResult = await middletierServiceApp
               .AcquireTokenInLongRunningProcess(downstreamApiScopes, cacheKey)
               .ExecuteAsync()
               .ConfigureAwait(false);

            Assert.IsNotNull(authenticationResult.AccessToken);
            Assert.IsFalse(userCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(userCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(cacheKey, userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(TokenSource.IdentityProvider, authenticationResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNull(
                userCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheExpiry,
                "The cache expiry is not set because there is an RT in the cache");
            Assert.AreEqual(1, middletierServiceApp.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
            Assert.AreEqual(1, middletierServiceApp.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
            Assert.AreEqual(1, middletierServiceApp.UserTokenCacheInternal.Accessor.GetAllIdTokens().Count);
            Assert.AreEqual(1, middletierServiceApp.UserTokenCacheInternal.Accessor.GetAllAccounts().Count);

            Trace.WriteLine("5. Subsequent acquire token calls should return cached token.");
            authenticationResult = await middletierServiceApp
                .AcquireTokenInLongRunningProcess(downstreamApiScopes, cacheKey)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, authenticationResult.AuthenticationResultMetadata.TokenSource);
        }
    }
}
