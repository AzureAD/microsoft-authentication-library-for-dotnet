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
    /// (<see cref="KnownCloudData"/>) and the single owner of the audience→scope ("/.default") rule
    /// (<see cref="CloudSettingsExtensions.TokenExchangeScope(CloudSettings)"/>). A consumer (ID Web, or a
    /// direct MSAL caller) resolves the cloud-specific exchange scope from the cloud configuration and then
    /// acquires the FIC assertion token for it.
    ///
    /// These tests exercise that end-to-end within MSAL: resolve the scope from the cloud config
    /// (built-in <see cref="KnownCloudConfiguration"/> for auto-resolve, or an
    /// <see cref="InMemoryCloudConfiguration"/> override to adjust/replace it), acquire a token for that
    /// scope via a real <see cref="IConfidentialClientApplication"/>, and mock ONLY the outbound HTTP —
    /// asserting the token request MSAL actually put on the wire carried the correct cloud-specific scope.
    /// The two cases cover the two axes: auto-resolve from the built-in defaults, and a caller override.
    /// </summary>
    [TestClass]
    public class CrossCloudTokenExchangeTests : TestBase
    {
        private const string PublicHost = "login.microsoftonline.com";
        private const string UsGovHost = "login.microsoftonline.us";

        private const string PublicAuthority = "https://login.microsoftonline.com/" + TestConstants.TenantId + "/";
        private const string UsGovAuthority = "https://login.microsoftonline.us/" + TestConstants.TenantId + "/";

        private const string CustomExchangeAudience = "api://MyCustomTokenExchange";

        [TestMethod]
        public Task PublicCloud_DefaultEndpoint_SendsPublicExchangeScopeAsync()
            => RunCrossCloudExchangeScenarioAsync(
                scenario: "Public cloud, default endpoint (auto-resolve from MSAL KnownCloudConfiguration)",
                authority: PublicAuthority,
                host: PublicHost,
                cloudConfig: KnownCloudConfiguration.Default,
                expectedExchangeScope: "api://AzureADTokenExchange/.default");

        [TestMethod]
        public Task UsGovCloud_CustomEndpoint_OverrideWinsAsync()
            => RunCrossCloudExchangeScenarioAsync(
                scenario: "US Gov cloud, caller override (InMemoryCloudConfiguration replaces the shipped USGov audience)",
                authority: UsGovAuthority,
                host: UsGovHost,
                cloudConfig: new InMemoryCloudConfiguration(fallback: KnownCloudConfiguration.Default)
                    .AddOrUpdate(UsGovHost, new Dictionary<string, string>
                    {
                        [MsalCloudKeys.TokenExchangeAudience] = CustomExchangeAudience,
                    }),
                expectedExchangeScope: CustomExchangeAudience + "/.default");

        /// <summary>
        /// Resolves the cloud-specific FIC token-exchange scope from <paramref name="cloudConfig"/>, acquires
        /// a token for it via a real confidential client on <paramref name="authority"/>, and asserts (via the
        /// mock HTTP handler's <c>ExpectedPostData</c>) that the outgoing token request carried
        /// <paramref name="expectedExchangeScope"/> as its <c>scope</c>.
        /// </summary>
        private async Task RunCrossCloudExchangeScenarioAsync(
            string scenario,
            string authority,
            string host,
            ICloudConfiguration cloudConfig,
            string expectedExchangeScope)
        {
            // 1. Resolve the exchange scope the same way a consumer would: from MSAL's cloud configuration,
            //    keyed by the request's authority host. MSAL owns the "/.default" suffix computation.
            CloudSettings settings = cloudConfig.GetSettingsByAuthorityHost(host);
            Assert.IsNotNull(settings, $"[{scenario}] expected cloud settings for host '{host}'.");

            string exchangeScope = settings.TokenExchangeScope();
            Assert.AreEqual(
                expectedExchangeScope,
                exchangeScope,
                $"[{scenario}] resolved exchange scope from the cloud configuration did not match.");

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
