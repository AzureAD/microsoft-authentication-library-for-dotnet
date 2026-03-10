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
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    /// <summary>
    /// E2E integration tests that validate the new CCA-based agentic API.
    /// Compare with <see cref="Agentic"/> (the original baseline) to see the difference:
    ///
    /// OLD (Agentic.cs baseline):
    ///   - Manually creates a platform CCA + assertion callback to get FIC
    ///   - Creates a second CCA with the agent identity + FIC as client assertion
    ///   - Calls AcquireTokenForClient on the agent CCA
    ///
    /// NEW (this file):
    ///   - Creates a single platform CCA with certificate
    ///   - Calls cca.AcquireTokenForAgent(agentId, scopes) — all FIC orchestration is internal
    /// </summary>
    [TestClass]
    public class AgenticCcaE2ETest
    {
        // Same constants as Agentic.cs so the flows are directly comparable
        private const string PlatformClientId = "aab5089d-e764-47e3-9f28-cc11c2513821"; // platform (host) app
        private const string TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";
        private const string AgentIdentity = "ab18ca07-d139-4840-8b3b-4be9610c6ed5";
        private const string UserUpn = "agentuser1@id4slab1.onmicrosoft.com";
        private const string TokenExchangeUrl = "api://AzureADTokenExchange/.default";
        private const string Scope = "https://graph.microsoft.com/.default";

        #region Case 1a – Certificate credential via AcquireTokenForAgent

        /// <summary>
        /// Mirrors <see cref="Agentic.AgentGetsAppTokenForGraphTest"/> but uses the new
        /// <see cref="IConfidentialClientApplication.AcquireTokenForAgent"/> API.
        ///
        /// Instead of manually wiring a platform CCA ? FIC ? agent CCA ? token,
        /// the caller just does:
        ///   cca.AcquireTokenForAgent(agentId, scopes).ExecuteAsync()
        /// </summary>
        [TestMethod]
        public async Task AgentGetsAppTokenWithCertificate_ViaAcquireTokenForAgentTest()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            // Single platform CCA — no need for a separate agent CCA
            var cca = ConfidentialClientApplicationBuilder
                .Create(PlatformClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithCertificate(cert, sendX5C: true)
                .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                .WithExperimentalFeatures(true)
                .Build();

            // One call does the full two-step flow internally:
            //   1. AcquireTokenForClient(api://AzureADTokenExchange/.default).WithFmiPath(agentId)
            //   2. Creates an internal agent CCA that uses the FIC as client assertion
            //   3. AcquireTokenForClient(scopes) on the agent CCA
            var result = await cca
                .AcquireTokenForAgent(AgentIdentity, [Scope])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result, "AuthenticationResult should not be null");
            Assert.IsNotNull(result.AccessToken, "AccessToken should not be null");
            Assert.IsTrue(result.ExpiresOn > DateTimeOffset.UtcNow, "Token should not be expired");

            Trace.WriteLine($"[CCA Case 1a] App token acquired from: {result.AuthenticationResultMetadata.TokenSource}");
        }

        #endregion

        #region Case 1b – Client assertion credential via AcquireTokenForAgent

        /// <summary>
        /// Same as Case 1a but the platform CCA authenticates with a
        /// <c>WithClientAssertion</c> callback instead of <c>WithCertificate</c>.
        ///
        /// The callback uses <see cref="AssertionRequestOptions"/> to get
        /// <c>ClientID</c> and <c>TokenEndpoint</c> at runtime — no need to
        /// hardcode the audience or client ID in the closure.
        ///
        /// AcquireTokenForAgent works identically regardless of how the
        /// platform CCA authenticates.
        /// </summary>
        [TestMethod]
        public async Task AgentGetsAppTokenWithAssertion_ViaAcquireTokenForAgentTest()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            // Platform CCA configured with WithClientAssertion — no WithCertificate here.
            // The callback only captures the cert; everything else comes from
            // AssertionRequestOptions at runtime.
            var cca = ConfidentialClientApplicationBuilder
                .Create(PlatformClientId)
                .WithAuthority($"https://login.microsoftonline.com/{TenantId}")
                .WithExperimentalFeatures(true)
                .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                .WithClientAssertion((AssertionRequestOptions opts) =>
                {
                    // opts.ClientID  = the platform app's client ID (issuer/subject)
                    // opts.TokenEndpoint = the exact token endpoint URL (audience)
                    // No hardcoded strings needed — MSAL tells us everything.
                    string jwt = CreateSignedJwt(opts.ClientID, opts.TokenEndpoint, cert);
                    return Task.FromResult(jwt);
                })
                .Build();

            // Same single call as Case 1a — proves the new API is
            // credential-agnostic.
            var result = await cca
                .AcquireTokenForAgent(AgentIdentity, [Scope])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result, "AuthenticationResult should not be null");
            Assert.IsNotNull(result.AccessToken, "AccessToken should not be null");
            Assert.IsTrue(result.ExpiresOn > DateTimeOffset.UtcNow, "Token should not be expired");

            Trace.WriteLine($"[CCA Case 1b] App token acquired from: {result.AuthenticationResultMetadata.TokenSource}");
        }

        #endregion

        #region E2E – Agent acts on behalf of a user (user_fic grant, same as baseline)

        /// <summary>
        /// Mirrors <see cref="Agentic.AgentUserIdentityGetsTokenForGraphTest"/> exactly.
        /// The user-delegated flow still uses the CCA + OnBeforeTokenRequest pattern.
        /// </summary>
        [TestMethod]
        public async Task AgentUserIdentityGetsTokenForGraph_ViaOnBehalfOfUserTest()
        {
            var cca = ConfidentialClientApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                .WithExperimentalFeatures(true)
                .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)> { { "slice", ("first", false) } })
                .WithClientAssertion((AssertionRequestOptions _) => GetAppCredentialAsync(AgentIdentity))
                .Build();

            var result = await (cca as IByUsernameAndPassword).AcquireTokenByUsernamePassword([Scope], UserUpn, "no_password")
                .OnBeforeTokenRequest(
                async (request) =>
                {
                    string userFicAssertion = await GetUserFic().ConfigureAwait(false);
                    request.BodyParameters["user_federated_identity_credential"] = userFicAssertion;
                    request.BodyParameters["grant_type"] = "user_fic";

                    // remove the password
                    request.BodyParameters.Remove("password");

                    if (request.BodyParameters.TryGetValue("client_secret", out var secret)
                            && secret.Equals("default", StringComparison.OrdinalIgnoreCase))
                    {
                        request.BodyParameters.Remove("client_secret");
                    }
                }
                )
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result, "AuthenticationResult should not be null");
            Assert.IsNotNull(result.AccessToken, "AccessToken should not be null");
            Assert.IsNotNull(result.Account, "Account should not be null after user flow");

            Trace.WriteLine($"[CCA E2E user] User token acquired from: {result.AuthenticationResultMetadata.TokenSource}");

            // Validate silent (cached) acquisition
            IAccount account = await cca.GetAccountAsync(result.Account.HomeAccountId.Identifier).ConfigureAwait(false);
            Assert.IsNotNull(account, "Account retrieved from cache should not be null");

            var result2 = await cca.AcquireTokenSilent([Scope], account).ExecuteAsync().ConfigureAwait(false);
            Assert.IsTrue(result2.AuthenticationResultMetadata.TokenSource == TokenSource.Cache, "Token should be from cache");

            Trace.WriteLine($"[CCA E2E user] Silent token acquired from: {result2.AuthenticationResultMetadata.TokenSource}");
        }

        #endregion

        #region E2E – Agent on behalf of user via AcquireTokenForAgentOnBehalfOfUser (NEW simplified API)

        /// <summary>
        /// Mirrors <see cref="Agentic.AgentUserIdentityGetsTokenForGraphTest"/> but uses the new
        /// <see cref="IConfidentialClientApplication.AcquireTokenForAgentOnBehalfOfUser"/> API.
        ///
        /// COMPARE — OLD (Agentic.cs baseline, ~30 lines of ceremony):
        ///   1. Create CCA with agent identity + assertion callback
        ///   2. Cast to IByUsernameAndPassword
        ///   3. Call AcquireTokenByUsernamePassword with dummy password
        ///   4. OnBeforeTokenRequest ? manually get User FIC, rewrite grant_type, strip password, etc.
        ///
        /// NEW (this test, 1 call):
        ///   cca.AcquireTokenForAgentOnBehalfOfUser(agentId, scopes, upn).ExecuteAsync()
        ///
        /// All the User FIC acquisition, grant_type rewriting, and password stripping
        /// is handled internally by the CCA.
        /// </summary>
        [TestMethod]
        public async Task AgentUserIdentityGetsToken_ViaAcquireTokenForAgentOnBehalfOfUserTest()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            // Single platform CCA with certificate — same as Case 1a
            var cca = ConfidentialClientApplicationBuilder
                .Create(PlatformClientId)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithCertificate(cert, sendX5C: true)
                .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                .WithExperimentalFeatures(true)
                .Build();

            // One call hides the entire user_fic complexity:
            //   1. Gets FIC from platform cert ? agent CCA (cached)
            //   2. Gets User FIC via agent CCA + WithFmiPathForClientAssertion
            //   3. Rewrites the token request to user_fic grant with the User FIC
            var result = await cca
                .AcquireTokenForAgentOnBehalfOfUser(AgentIdentity, [Scope], UserUpn)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result, "AuthenticationResult should not be null");
            Assert.IsNotNull(result.AccessToken, "AccessToken should not be null");
            Assert.IsNotNull(result.Account, "Account should not be null after user flow");

            Trace.WriteLine($"[CCA new user API] User token acquired from: {result.AuthenticationResultMetadata.TokenSource}");

            // Note: Silent token acquisition via cca.AcquireTokenSilent is not available
            // here because the user-delegated token lives in the internal agent CCA's cache,
            // not the platform CCA's cache. Calling AcquireTokenForAgentOnBehalfOfUser again
            // will benefit from caching at the internal agent CCA level.
        }

        #endregion

        #region Helpers (same as Agentic.cs baseline)

        private static async Task<string> GetAppCredentialAsync(string fmiPath)
        {
            Assert.IsNotNull(fmiPath, "fmiPath cannot be null");
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var cca1 = ConfidentialClientApplicationBuilder
                        .Create(PlatformClientId)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                        .WithExperimentalFeatures(true)
                        .WithCertificate(cert, sendX5C: true)
                        .Build();

            var result = await cca1.AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPath(fmiPath)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Trace.WriteLine($"FMI app credential from: {result.AuthenticationResultMetadata.TokenSource}");

            return result.AccessToken;
        }

        private static async Task<string> GetUserFic()
        {
            var cca1 = ConfidentialClientApplicationBuilder
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

            var result = await cca1.AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPathForClientAssertion(AgentIdentity)
                .ExecuteAsync().ConfigureAwait(false);

            Trace.WriteLine($"User FIC credential from: {result.AuthenticationResultMetadata.TokenSource}");

            return result.AccessToken;
        }

        /// <summary>
        /// Builds a signed client assertion JWT using the Wilson library.
        /// This is the same JWT that <c>WithCertificate</c> would create internally,
        /// but done manually so we can pass it via <c>WithClientAssertion</c>.
        /// </summary>
        private static string CreateSignedJwt(string clientId, string audience, X509Certificate2 cert)
        {
            var claims = new Dictionary<string, object>
            {
                { "aud", audience },
                { "iss", clientId },
                { "jti", Guid.NewGuid().ToString() },
                { "sub", clientId }
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = claims,
                SigningCredentials = new X509SigningCredentials(cert)
            };

            return new JsonWebTokenHandler().CreateToken(tokenDescriptor);
        }

        #endregion
    }
}
