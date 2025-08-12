// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Unit.TestUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    /// <summary>
    /// Tests for token revocation scenarios with Managed Identity
    /// </summary>
    [TestClass]
    public class TokenRevocationTests : TestBase
    {
        [ClassInitialize]
        public static async Task ClassInit(TestContext context)
        {
            await ConfigService.InitializeAsync("token-revocation", true).ConfigureAwait(false);
        }
        
        [TestMethod]
        [Description("Tests that a revoked token is properly replaced when a claims challenge is received")]
        public async Task ServiceFabric_WithTokenRevocation_RetrievesNewToken_Async()
        {
            // Get the test scenario configuration using the TestScenario API
            var scenario = ConfigService.GetScenario("serviceFabricRevocation");
            
            // Get the resource and endpoints from the scenario helper
            string resource = scenario.Resource;
            string serviceUri = scenario.CreateIdentityProviderUri();
            string revocationEndpoint = scenario.CreateRevocationEndpointUri();
            
            // Build managed identity application with client capabilities for token revocation support
            var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithClientCapabilities(["cp1"]) // Client capability needed for token revocation
                .WithHttpClientFactory(ConfigService.HttpClientFactory);

            // Set the Service Fabric environment variable to point to our test service
            using (new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", serviceUri);
                Environment.SetEnvironmentVariable("IDENTITY_HEADER", "service-fabric-test-header");

                IManagedIdentityApplication mi = miBuilder.Build();

                // PHASE 1: Initial token acquisition
                // Get the initial token - should succeed and be cached
                var result1 = await mi.AcquireTokenForManagedIdentity(resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // ASSERT - PHASE 1
                Assert.IsNotNull(result1);
                Assert.IsNotNull(result1.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);
                string initialToken = result1.AccessToken;

                // Verify we can get the same token from cache
                var resultFromCache = await mi.AcquireTokenForManagedIdentity(resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                
                Assert.AreEqual(TokenSource.Cache, resultFromCache.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(initialToken, resultFromCache.AccessToken);

                // PHASE 2: Simulate token revocation scenario
                // This simulates what would happen when a token is revoked and a resource rejects it
                string claimsChallenge = await SimulateTokenRejectionAndGetClaimsChallengeAsync(
                    initialToken, 
                    revocationEndpoint, 
                    ConfigService.HttpClientFactory.GetHttpClient()).ConfigureAwait(false);
                
                // PHASE 3: Get a new token using the claims challenge
                // Use the claims challenge we got from the simulated resource rejection
                var result2 = await mi.AcquireTokenForManagedIdentity(resource)
                    .WithClaims(claimsChallenge)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // ASSERT - PHASE 3
                Assert.IsNotNull(result2);
                Assert.IsNotNull(result2.AccessToken);
                
                // The token should come from the identity provider, not the cache
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);
                
                // The new token should be different from the original one
                Assert.AreNotEqual(initialToken, result2.AccessToken, "Token should be different after revocation");

                // Verify the revoked token is no longer returned from cache, and new token is used instead
                var resultAfterRevocation = await mi.AcquireTokenForManagedIdentity(resource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                
                Assert.AreEqual(TokenSource.Cache, resultAfterRevocation.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(result2.AccessToken, resultAfterRevocation.AccessToken);
                Assert.AreNotEqual(initialToken, resultAfterRevocation.AccessToken);
            }
        }

        /// <summary>
        /// Simulates calling a resource with a token that has been revoked, and getting back a claims challenge
        /// </summary>
        /// <param name="token">The token to use in the request</param>
        /// <param name="revocationEndpoint">The endpoint that simulates token revocation</param>
        /// <param name="httpClient">HttpClient to use for the request</param>
        /// <returns>The claims challenge string returned by the service</returns>
        private static async Task<string> SimulateTokenRejectionAndGetClaimsChallengeAsync(
            string token, 
            string revocationEndpoint, 
            HttpClient httpClient)
        {
            // Create a request to the revocation endpoint with the token in the Authorization header
            var request = new HttpRequestMessage(HttpMethod.Get, revocationEndpoint);
            request.Headers.Add("Authorization", $"Bearer {token}");
            
            // Send the request to simulate accessing a resource with a revoked token
            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            
            // We expect a 401 Unauthorized response with a WWW-Authenticate header containing the claims challenge
            if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException($"Expected 401 Unauthorized from revocation simulation endpoint, but got {response.StatusCode}");
            }

            // Parse the WWW-Authenticate header to get the claims challenge
            WwwAuthenticateParameters authParams = WwwAuthenticateParameters.CreateFromWwwAuthenticateHeaderValue(
                response.Headers.WwwAuthenticate.ToString());
            
            return authParams.Claims;
        }
    }
}
