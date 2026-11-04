// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    [Ignore]
    public class OnBehalfOfServicePrincipalTests
    {
        private const string OboClientTseClientId = "31bb6a4c-e472-47d1-897d-9e3a1bdc751f";
        private const string OboServiceTseClientId = "482c55e1-578e-4a1e-bb06-b3cfde53e14b";
        private const string OboServiceDownstreamApiTseClientId = "b0b1e288-5d87-49f7-8ba5-164304157c9c";
        private const string TseAuthenticationAuthority = "https://login.microsoftonline.com/d8d14ca6-f1fa-41c1-ae95-0db6258ec0a0";
        private const string TseCertificateThumbprint = "D831CA99C5DC7C835C7A410CF2B33BA79D6E61F1";

        /// <summary>
        /// Client -> Middletier -> RP
        /// This is OBO for SP without RT support.
        /// The middle-tier and downstream API use 1p apps.
        /// </summary>
        /// <remarks>
        /// For details see https://aadwiki.windows-int.net/index.php?title=App_OBO_aka._Service_Principal_OBO, which explains
        /// the structure of the access token received from OBO.
        /// </remarks>
        [TestMethod]
        public async Task NormalObo_TestAsync()
        {
            //An explanation of the OBO for service principal scenario can be found here https://aadwiki.windows-int.net/index.php?title=App_OBO_aka._Service_Principal_OBO

            using var cert = FindTseCertificate();

            IReadOnlyList<string> middleTierApiScopes = new List<string>() { OboServiceTseClientId + "/.default" };
            IReadOnlyList<string> downstreamApiScopes = new List<string>() { OboServiceDownstreamApiTseClientId + "/.default" };

            var clientConfidentialApp = ConfidentialClientApplicationBuilder
                                    .Create(OboClientTseClientId)
                                    .WithAuthority(TseAuthenticationAuthority)
                                    .WithCertificate(cert, sendX5C: true)
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
                .Create(OboServiceTseClientId)
                .WithAuthority(TseAuthenticationAuthority)
                .WithCertificate(cert, sendX5C: true)
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
        [TestMethod]
        public async Task LongRunningObo_TestAsync()
        {
            //An explanation of the OBO for service principal scenario can be found here https://aadwiki.windows-int.net/index.php?title=App_OBO_aka._Service_Principal_OBO

            using var cert = FindTseCertificate();

            IReadOnlyList<string> middleTierApiScopes = new List<string>() { OboServiceTseClientId + "/.default" };
            IReadOnlyList<string> downstreamApiScopes = new List<string>() { OboServiceDownstreamApiTseClientId + "/.default" };

            var clientConfidentialApp = ConfidentialClientApplicationBuilder
                                    .Create(OboClientTseClientId)
                                    .WithAuthority(TseAuthenticationAuthority)
                                    .WithCertificate(cert, sendX5C: true)
                                    .WithTestLogging()
                                    .Build();

            var middletierServiceApp = ConfidentialClientApplicationBuilder
                                    .Create(OboServiceTseClientId)
                                    .WithAuthority(TseAuthenticationAuthority)
                                    .WithCertificate(cert, sendX5C: true)
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
            Assert.HasCount(1, middletierServiceApp.UserTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.HasCount(1, middletierServiceApp.UserTokenCacheInternal.Accessor.GetAllRefreshTokens());
            Assert.HasCount(1, middletierServiceApp.UserTokenCacheInternal.Accessor.GetAllIdTokens());
            Assert.HasCount(1, middletierServiceApp.UserTokenCacheInternal.Accessor.GetAllAccounts());

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
            Assert.HasCount(1, middletierServiceApp.UserTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.HasCount(1, middletierServiceApp.UserTokenCacheInternal.Accessor.GetAllRefreshTokens());
            Assert.HasCount(1, middletierServiceApp.UserTokenCacheInternal.Accessor.GetAllIdTokens());
            Assert.HasCount(1, middletierServiceApp.UserTokenCacheInternal.Accessor.GetAllAccounts());

            Trace.WriteLine("5. Subsequent acquire token calls should return cached token.");
            authenticationResult = await middletierServiceApp
                .AcquireTokenInLongRunningProcess(downstreamApiScopes, cacheKey)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, authenticationResult.AuthenticationResultMetadata.TokenSource);
        }

        private static X509Certificate2 FindTseCertificate()
        {
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, TseCertificateThumbprint, validOnly: false);
            Assert.HasCount(1, certificates, "Install the LabAuthTse certificate in LocalMachine/My.");
            Assert.IsTrue(certificates[0].HasPrivateKey, "The LabAuthTse certificate must have its private key.");
            return certificates[0];
        }
    }
}
