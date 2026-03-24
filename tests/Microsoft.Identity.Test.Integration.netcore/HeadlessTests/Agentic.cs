// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class Agentic
    {
        const string ClientId = "aab5089d-e764-47e3-9f28-cc11c2513821"; // agent app
        const string TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";
        const string AgentIdentity = "ab18ca07-d139-4840-8b3b-4be9610c6ed5";
        const string UserUpn = "agentuser1@id4slab1.onmicrosoft.com";
        private const string TokenExchangeUrl = "api://AzureADTokenExchange/.default";
        private const string Scope = "https://graph.microsoft.com/.default";

        #region UserFIC Primitive + App-only Tests (Lower-Level API)

        /// <summary>
        /// Validates the low-level UserFIC primitive: builds separate assertion and main CCAs,
        /// acquires a user_fic assertion via FMI, then exchanges it for a user-scoped Graph token
        /// using the UPN-based AcquireTokenByUserFederatedIdentityCredential overload.
        /// Also verifies that the resulting token is cached and can be retrieved silently.
        /// </summary>
        [TestMethod]
        public async Task AgentUserIdentityGetsTokenForGraphTest()
        {
            await AgentUserIdentityGetsTokenForGraphAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Validates app-only (no user) token acquisition for an agent app.
        /// The agent CCA uses an FMI-based client assertion to get a client credentials token
        /// for Graph, without any user identity involved.
        /// </summary>
        [TestMethod]
        public async Task AgentGetsAppTokenForGraphTest()
        {
            await AgentGetsAppTokenForGraph().ConfigureAwait(false);
        }

        private static async Task AgentGetsAppTokenForGraph()
        {
            var cca = ConfidentialClientApplicationBuilder
                        .Create(AgentIdentity)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                        .WithExperimentalFeatures(true)
                        .WithClientAssertion((AssertionRequestOptions _) => GetAppCredentialAsync(AgentIdentity))
                        .Build();

            var result = await cca.AcquireTokenForClient([Scope])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Trace.WriteLine($"FMI app credential from : {result.AuthenticationResultMetadata.TokenSource}");
        }

        private static async Task AgentUserIdentityGetsTokenForGraphAsync()
        {
            // Assertion app: acquires the user_fic assertion via FMI path
            var assertionApp = ConfidentialClientApplicationBuilder
                        .Create(AgentIdentity)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithExperimentalFeatures(true)
                        .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                        .WithClientAssertion(async (AssertionRequestOptions a) =>
                        {
                            Assert.AreEqual(AgentIdentity, a.ClientAssertionFmiPath);
                            var cred = await GetAppCredentialAsync(a.ClientAssertionFmiPath).ConfigureAwait(false);
                            return cred;
                        })
                        .Build();

            // Main app: acquires the final user token via user_fic grant
            var cca = ConfidentialClientApplicationBuilder
                        .Create(AgentIdentity)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                        .WithExperimentalFeatures(true)
                        .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)> { { "slice", ("first", false) } })
                        .WithClientAssertion((AssertionRequestOptions _) => GetAppCredentialAsync(AgentIdentity))
                        .Build();

            // Assertion provider using the assertion app with FMI path
            var assertionResult = await assertionApp
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPathForClientAssertion(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Trace.WriteLine($"User FIC credential from : {assertionResult.AuthenticationResultMetadata.TokenSource}");
            string assertion = assertionResult.AccessToken;

            var result = await (cca as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential([Scope], UserUpn, assertion)
                .ExecuteAsync()
                .ConfigureAwait(false);

            IAccount account = await cca.GetAccountAsync(result.Account.HomeAccountId.Identifier).ConfigureAwait(false);
            var result2 = await cca.AcquireTokenSilent([Scope], account).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource, "Token should be from cache");
        }

        #endregion

        #region AcquireTokenForAgent Tests (High-Level API)

        /// <summary>
        /// Tests the high-level AcquireTokenForAgent API with a UPN-based AgentIdentity.
        /// This exercises the full 3-leg flow (FMI credential → assertion → UserFIC exchange)
        /// orchestrated internally by AgentTokenRequest, using a blueprint CCA with SN+I certificate.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenForAgent_WithUpn_Test()
        {
            // Arrange: Blueprint CCA configured with certificate (SN+I) for FMI flows
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var blueprintCca = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            var agentId = Client.AgentIdentity.WithUsername(AgentIdentity, UserUpn);

            // Act: Use the high-level AcquireTokenForAgent API
            var result = await blueprintCca
                .AcquireTokenForAgent([Scope], agentId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsNotNull(result.AccessToken, "Access token should not be null");
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource, "Token should be from identity provider");

            Trace.WriteLine($"AcquireTokenForAgent (UPN) token source: {result.AuthenticationResultMetadata.TokenSource}");
        }

        /// <summary>
        /// Tests the high-level AcquireTokenForAgent API for app-only (no user) scenarios.
        /// Only Legs 1-2 are performed: the blueprint CCA fetches an FMI credential, then
        /// an internal agent CCA uses it to get a client credentials token for Graph.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenForAgent_AppOnly_Test()
        {
            // Arrange: Blueprint CCA configured with certificate (SN+I) for FMI flows
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var blueprintCca = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            var agentId = Client.AgentIdentity.AppOnly(AgentIdentity);

            // Act: Use the high-level AcquireTokenForAgent API for app-only scenario
            var result = await blueprintCca
                .AcquireTokenForAgent([Scope], agentId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsNotNull(result.AccessToken, "Access token should not be null");

            Trace.WriteLine($"AcquireTokenForAgent (AppOnly) token source: {result.AuthenticationResultMetadata.TokenSource}");
        }

        #endregion

        #region UserFIC Guid Overload Tests

        /// <summary>
        /// Tests the Guid (OID) overload of the low-level AcquireTokenByUserFederatedIdentityCredential primitive.
        /// First discovers the user's OID by calling the UPN-based flow, then acquires a fresh assertion
        /// and calls the Guid overload to verify it sends user_id (OID) instead of username (UPN).
        /// </summary>
        [TestMethod]
        public async Task UserFic_WithGuidObjectId_Test()
        {
            // Arrange: First obtain a token via UPN to get the user's OID
            var assertionApp = ConfidentialClientApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                .WithClientAssertion(async (AssertionRequestOptions a) =>
                {
                    return await GetAppCredentialAsync(a.ClientAssertionFmiPath ?? AgentIdentity).ConfigureAwait(false);
                })
                .Build();

            var cca = ConfidentialClientApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                .WithExperimentalFeatures(true)
                .WithClientAssertion((AssertionRequestOptions _) => GetAppCredentialAsync(AgentIdentity))
                .Build();

            // Step 1: Get assertion via FMI path
            var assertionResult = await assertionApp
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPathForClientAssertion(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            string assertion1 = assertionResult.AccessToken;

            // Step 2: Get user token via UPN to discover the user's OID
            var upnResult = await (cca as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential([Scope], UserUpn, assertion1)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(upnResult.Account, "Account should not be null");

            // Extract the user OID from the account's HomeAccountId (format: oid.tid)
            string oidString = upnResult.Account.HomeAccountId.ObjectId;
            Assert.IsNotNull(oidString, "OID should not be null");
            Guid userOid = Guid.Parse(oidString);

            Trace.WriteLine($"Discovered user OID: {userOid}");

            // Step 3: Now acquire a NEW assertion (since the first one was consumed)
            var assertionResult2 = await assertionApp
                .AcquireTokenForClient([TokenExchangeUrl])
                .WithForceRefresh(true)
                .WithFmiPathForClientAssertion(AgentIdentity)
                .ExecuteAsync()
                .ConfigureAwait(false);

            string assertion2 = assertionResult2.AccessToken;

            // Act: Use the Guid overload of AcquireTokenByUserFederatedIdentityCredential
            var result = await (cca as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential([Scope], userOid, assertion2)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsNotNull(result.AccessToken, "Access token should not be null");
            Assert.AreEqual(oidString, result.Account.HomeAccountId.ObjectId, "OID should match");

            Trace.WriteLine($"UserFIC Guid overload token source: {result.AuthenticationResultMetadata.TokenSource}");
        }

        /// <summary>
        /// Tests the high-level AcquireTokenForAgent API with a Guid (OID)-based AgentIdentity.
        /// Discovers the user's OID via the UPN path first, then creates an AgentIdentity(agentAppId, userOid)
        /// and verifies the full 3-leg flow succeeds using the OID-based UserFIC exchange.
        /// </summary>
        [TestMethod]
        public async Task AcquireTokenForAgent_WithOid_Test()
        {
            // Arrange: First discover the user's OID by running a UPN-based flow
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var blueprintCca = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            // Get the OID via the UPN path
            var upnResult = await blueprintCca
                .AcquireTokenForAgent([Scope], Client.AgentIdentity.WithUsername(AgentIdentity, UserUpn))
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(upnResult.Account, "Account should not be null after UPN-based flow");
            Guid userOid = Guid.Parse(upnResult.Account.HomeAccountId.ObjectId);
            Trace.WriteLine($"Discovered user OID: {userOid}");

            // Act: Build a new blueprint CCA and use the OID-based AgentIdentity
            var blueprintCca2 = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithExperimentalFeatures(true)
                .WithCertificate(cert, sendX5C: true)
                .Build();

            var agentId = new Client.AgentIdentity(AgentIdentity, userOid);

            var result = await blueprintCca2
                .AcquireTokenForAgent([Scope], agentId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsNotNull(result.AccessToken, "Access token should not be null");
            Assert.AreEqual(
                upnResult.Account.HomeAccountId.ObjectId,
                result.Account.HomeAccountId.ObjectId,
                "OID should match between UPN and OID flows");

            Trace.WriteLine($"AcquireTokenForAgent (OID) token source: {result.AuthenticationResultMetadata.TokenSource}");
        }

        #endregion

        #region Shared Helpers

        private static async Task<string> GetAppCredentialAsync(string fmiPath)
        {
            Assert.IsNotNull(fmiPath, "fmiPath cannot be null");
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var cca1 = ConfidentialClientApplicationBuilder
                        .Create(ClientId)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                        .WithExperimentalFeatures(true)
                        .WithCertificate(cert, sendX5C: true) //sendX5c enables SN+I auth which is required for FMI flows                        
                        .Build();

            var result = await cca1.AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPath(fmiPath)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Trace.WriteLine($"FMI app credential from : {result.AuthenticationResultMetadata.TokenSource}");

            return result.AccessToken;
        }

        #endregion
    }
}
