// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    /// <summary>
    /// End-to-end integration tests that validate the Agentic API surface.
    /// These mirror the flows in <see cref="Agentic"/> but use the new
    /// <see cref="IAgenticApplication"/> / <see cref="AgenticApplicationBuilder"/> API.
    /// </summary>
    [TestClass]
    public class AgenticApiTest
    {
        // Same constants as the existing Agentic.cs tests
        private const string ClientId = "aab5089d-e764-47e3-9f28-cc11c2513821"; // platform app
        private const string TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";
        private const string AgentIdentity = "ab18ca07-d139-4840-8b3b-4be9610c6ed5";
        private const string UserUpn = "agentuser1@id4slab1.onmicrosoft.com";
        private const string Scope = "https://graph.microsoft.com/.default";

        #region Flow 1 – Agent gets app token (same as Agentic.AgentGetsAppTokenForGraphTest)

        [TestMethod]
        public async Task AgentGetsAppTokenUsingAgenticApiTest()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            IAgenticApplication agentApp = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(ClientId, cert, sendX5C: true)
                .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                .Build();

            // Acquire an app-only token — this internally:
            //  1. Gets an FMI credential from the platform CCA (cert + SN+I)
            //  2. Uses the FMI as a client assertion to get a token for Graph
            AuthenticationResult result = await agentApp
                .AcquireTokenForAgent(new[] { Scope })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result, "AuthenticationResult should not be null");
            Assert.IsNotNull(result.AccessToken, "AccessToken should not be null");
            Assert.IsTrue(result.ExpiresOn > DateTimeOffset.UtcNow, "Token should not be expired");

            Trace.WriteLine($"[AgenticApi] App token acquired from: {result.AuthenticationResultMetadata.TokenSource}");
        }

        #endregion

        #region Flow 2 – Agent acts on behalf of a user (same as Agentic.AgentUserIdentityGetsTokenForGraphTest)

        [TestMethod]
        public async Task AgentUserIdentityGetsTokenUsingAgenticApiTest()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            IAgenticApplication agentApp = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(ClientId, cert, sendX5C: true)
                .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                .Build();

            // Acquire a user-delegated token — this internally:
            //  1. Gets an FMI credential for the agent's client assertion
            //  2. Gets a User FIC token via AcquireTokenForClient + WithFmiPathForClientAssertion
            //  3. Rewrites the body to use grant_type=user_fic with the User FIC as assertion
            AuthenticationResult result = await agentApp
                .AcquireTokenForAgentOnBehalfOfUser(new[] { Scope }, UserUpn)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result, "AuthenticationResult should not be null");
            Assert.IsNotNull(result.AccessToken, "AccessToken should not be null");
            Assert.IsNotNull(result.Account, "Account should not be null after user flow");

            Trace.WriteLine($"[AgenticApi] User token acquired from: {result.AuthenticationResultMetadata.TokenSource}");

            // Validate silent (cached) acquisition — same pattern as the original Agentic.cs
            IAccount account = await agentApp
                .GetAccountAsync(result.Account.HomeAccountId.Identifier)
                .ConfigureAwait(false);

            Assert.IsNotNull(account, "Account retrieved from cache should not be null");

            AuthenticationResult silentResult = await agentApp
                .AcquireTokenSilent(new[] { Scope }, account)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(silentResult, "Silent result should not be null");
            Assert.AreEqual(
                TokenSource.Cache,
                silentResult.AuthenticationResultMetadata.TokenSource,
                "Token should be served from cache on the second call");

            Trace.WriteLine($"[AgenticApi] Silent token acquired from: {silentResult.AuthenticationResultMetadata.TokenSource}");
        }

        #endregion
    }
}
