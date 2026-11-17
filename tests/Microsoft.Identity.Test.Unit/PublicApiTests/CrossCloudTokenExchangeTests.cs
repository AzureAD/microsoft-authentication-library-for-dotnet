// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    /// <summary>
    /// Cross-cloud token-exchange (FIC) integration tests at the MSAL layer.
    ///
    /// MSAL is the store of publicly-known, cloud-specific FIC token-exchange magic strings
    /// (<see cref="KnownCloudData"/>, surfaced by <see cref="KnownCloudMetadata"/>) and the single owner of
    /// the audience→scope ("/.default") rule (<see cref="TokenExchangeScope"/>). A consumer (ID Web, or a
    /// direct MSAL caller) resolves the cloud-specific exchange audience and then acquires the FIC assertion
    /// token for the computed scope.
    ///
    /// These tests exercise that end-to-end within MSAL: obtain the cloud-specific audience (auto-resolved
    /// from the built-in <see cref="KnownCloudMetadata"/>, or supplied directly by the caller as an override),
    /// compute the scope with <see cref="TokenExchangeScope.FromAudience(string)"/>, acquire a token for that
    /// scope via a real <see cref="IConfidentialClientApplication"/>, and mock ONLY the outbound HTTP —
    /// asserting the token request MSAL actually put on the wire carried the correct cloud-specific scope.
    /// The two cases cover the two axes: auto-resolve from the built-in defaults, and a caller override.
    /// </summary>
    [TestClass]
    public class CrossCloudTokenExchangeTests : TestBase
    {
        private const string PublicHost = "login.microsoftonline.com";

        private const string PublicAuthority = "https://login.microsoftonline.com/" + TestConstants.TenantId + "/";
        private const string UsGovAuthority = "https://login.microsoftonline.us/" + TestConstants.TenantId + "/";

        private const string CustomExchangeAudience = "api://MyCustomTokenExchange";

        [TestMethod]
        public Task PublicCloud_DefaultEndpoint_SendsPublicExchangeScopeAsync()
        {
            // Auto-resolve the audience from MSAL's built-in known cloud metadata, keyed by authority host.
            IReadOnlyDictionary<string, string> values = KnownCloudMetadata.Default.GetByAuthorityHost(PublicHost);
            Assert.IsNotNull(values, "expected built-in metadata for the public cloud host.");
            string audience = values[CloudMetadataKeyNames.FederatedCredentialAudience];

            return RunCrossCloudExchangeScenarioAsync(
                scenario: "Public cloud, default endpoint (auto-resolve from MSAL KnownCloudMetadata)",
                authority: PublicAuthority,
                exchangeAudience: audience,
                expectedExchangeScope: "api://AzureADTokenExchange/.default");
        }

        [TestMethod]
        public Task UsGovCloud_CustomEndpoint_OverrideWinsAsync()
            // A raw-MSAL caller can override by supplying its own audience string directly; MSAL does not
            // self-consume the metadata key, so the caller owns "adjust/replace" at this layer.
            => RunCrossCloudExchangeScenarioAsync(
                scenario: "US Gov cloud, caller override (custom audience supplied directly by the caller)",
                authority: UsGovAuthority,
                exchangeAudience: CustomExchangeAudience,
                expectedExchangeScope: CustomExchangeAudience + "/.default");

        /// <summary>
        /// Computes the FIC token-exchange scope from <paramref name="exchangeAudience"/>, acquires a token
        /// for it via a real confidential client on <paramref name="authority"/>, and asserts (via the mock
        /// HTTP handler's <c>ExpectedPostData</c>) that the outgoing token request carried
        /// <paramref name="expectedExchangeScope"/> as its <c>scope</c>.
        /// </summary>
        private async Task RunCrossCloudExchangeScenarioAsync(
            string scenario,
            string authority,
            string exchangeAudience,
            string expectedExchangeScope)
        {
            // Compute the exchange scope the same way a consumer would: MSAL owns the "/.default" rule.
            string exchangeScope = TokenExchangeScope.FromAudience(exchangeAudience);
            Assert.AreEqual(
                expectedExchangeScope,
                exchangeScope,
                $"[{scenario}] resolved exchange scope did not match.");

            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithAuthority(authority, validateAuthority: true)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler(authority);

                // The FIC assertion acquisition is a client-credentials request whose 'scope' is the resolved
                // cloud-specific exchange scope. Assert exactly that on the wire.
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: new Dictionary<string, string>
                    {
                        { "scope", exchangeScope },
                    });

                AuthenticationResult result = await app
                    .AcquireTokenForClient(new[] { exchangeScope })
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result, $"[{scenario}] expected a non-null result.");
                Assert.AreEqual("header.payload.signature", result.AccessToken, $"[{scenario}] unexpected access token.");
                Assert.AreEqual(
                    TokenSource.IdentityProvider,
                    result.AuthenticationResultMetadata.TokenSource,
                    $"[{scenario}] expected the token to come from the identity provider (mocked STS).");
            }
        }
    }
}
