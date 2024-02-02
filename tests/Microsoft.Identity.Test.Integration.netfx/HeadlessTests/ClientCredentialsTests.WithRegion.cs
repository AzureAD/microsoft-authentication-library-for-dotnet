// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
#if NET_CORE
using Microsoft.Identity.Client.PlatformsCommon.Shared;
#endif
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class RegionalAuthIntegrationTests
    {
        private KeyVaultSecretsProvider _keyVault;

        private const string RegionalHost = "centralus.login.microsoft.com";
        private const string GlobalHost = "login.microsoftonline.com";
        private IConfidentialClientApplication _confidentialClientApplication;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();

            if (_keyVault == null)
            {
                _keyVault = new KeyVaultSecretsProvider();
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, null);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task AcquireTokenToRegionalEndpointAsync(bool instanceDiscoveryEnabled)
        {
            // Arrange
            var factory = new HttpSnifferClientFactory();
            var settings = ConfidentialAppSettings.GetSettings(Cloud.Public);
            settings.InstanceDiscoveryEndpoint = instanceDiscoveryEnabled;
            _confidentialClientApplication = BuildCCA(settings, factory);

            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);
            AuthenticationResult result = await GetAuthenticationResultAsync(settings.AppScopes).ConfigureAwait(false); // regional endpoint
            AssertTokenSourceIsIdp(result);
            AssertValidHost(true, factory);
            AssertTelemetry(factory, $"{TelemetryConstants.HttpTelemetrySchemaVersion}|1004,{CacheRefreshReason.NoCachedAccessToken:D},centralus,3,4|0,1,1");
            Assert.AreEqual(
                $"https://{RegionalHost}/{settings.TenantId}/oauth2/v2.0/token",
                result.AuthenticationResultMetadata.TokenEndpoint);
        }

        [TestMethod]
        public async Task InvalidRegion_GoesToInvalidAuthority_Async()
        {
            // Arrange
            var factory = new HttpSnifferClientFactory();
            var settings = ConfidentialAppSettings.GetSettings(Cloud.Public);
            _confidentialClientApplication = BuildCCA(settings, factory, true, "invalid");

            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            var ex = await Assert.ThrowsExceptionAsync<HttpRequestException>(
                async () => await GetAuthenticationResultAsync(settings.AppScopes).ConfigureAwait(false)).ConfigureAwait(false);

            Assert.IsTrue(ex is HttpRequestException);
        }

        private void AssertTelemetry(HttpSnifferClientFactory factory, string currentTelemetryHeader, int placement = 0)
        {
            var (req, res) = factory.RequestsAndResponses.Skip(placement).Single();
            Assert.AreEqual(currentTelemetryHeader, req.Headers.GetValues("x-client-current-telemetry").First());
        }

        private void AssertValidHost(
          bool isRegionalHost,
          HttpSnifferClientFactory factory,
          int placement = 0)
        {
            if (isRegionalHost)
            {
                var (req, res) = factory.RequestsAndResponses.Skip(placement).Single(x => x.Item1.RequestUri.Host == RegionalHost && x.Item2.StatusCode == HttpStatusCode.OK);
                Assert.AreEqual(RegionalHost, req.RequestUri.Host);
            }
            else
            {
                var (req, res) = factory.RequestsAndResponses.Skip(placement).Single(x => x.Item1.RequestUri.Host == GlobalHost && x.Item2.StatusCode == HttpStatusCode.OK);
                Assert.AreEqual(GlobalHost, req.RequestUri.Host);
            }
        }

        private void AssertTokenSourceIsIdp(
           AuthenticationResult result)
        {
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }

        private IConfidentialClientApplication BuildCCA(
            IConfidentialAppSettings settings,
            HttpSnifferClientFactory factory,
            bool useClaims = false,
            string region = ConfidentialClientApplication.AttemptRegionDiscovery)
        {
            var builder = ConfidentialClientApplicationBuilder.Create(settings.ClientId);
            if (useClaims)
            {
                builder.WithClientAssertion(GetSignedClientAssertionUsingMsalInternal(settings.ClientId, GetClaims(settings)));
            }
            else
            {
                builder.WithCertificate(settings.GetCertificate());
            }

            builder.WithAuthority($@"https://{settings.Environment}/{settings.TenantId}")
                .WithInstanceDiscovery(settings.InstanceDiscoveryEndpoint)
                .WithTestLogging()
                .WithExperimentalFeatures(true)
                .WithHttpClientFactory(factory);

            if (region != null)
            {
                builder.WithAzureRegion(region);
            }

            return builder.Build();
        }

        private async Task<AuthenticationResult> GetAuthenticationResultAsync(
            string[] scope,
            bool withForceRefresh = false)
        {
            var result = await _confidentialClientApplication.AcquireTokenForClient(scope)
                            .WithForceRefresh(withForceRefresh)
                            .ExecuteAsync()
                            .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.AccessToken);
            return result;
        }

        internal static long ConvertToTimeT(DateTime time)
        {
            var startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = time - startTime;
            return (long)diff.TotalSeconds;
        }

        private static IDictionary<string, string> GetClaims(IConfidentialAppSettings settings)
        {
            DateTime validFrom = DateTime.UtcNow;
            var nbf = ConvertToTimeT(validFrom);
            var exp = ConvertToTimeT(validFrom + TimeSpan.FromSeconds(TestConstants.JwtToAadLifetimeInSeconds));

            return new Dictionary<string, string>()
                {
                { "aud", $"https://{settings.Environment}/{settings.TenantId}/v2.0" },
                { "exp", exp.ToString(CultureInfo.InvariantCulture) },
                { "iss", settings.ClientId },
                { "jti", Guid.NewGuid().ToString() },
                { "nbf", nbf.ToString(CultureInfo.InvariantCulture) },
                { "sub", settings.ClientId },
                { "ip", "192.168.2.1" }
                };

        }

        private static string GetSignedClientAssertionUsingMsalInternal(string clientId, IDictionary<string, string> claims)
        {
            var manager = PlatformProxyFactory.CreatePlatformProxy(null).CryptographyManager;

            var jwtToken = new Client.Internal.JsonWebToken(manager, clientId, TestConstants.ClientCredentialAudience, claims);
            var cert = ConfidentialAppSettings.GetSettings(Cloud.Public).GetCertificate();

            return jwtToken.Sign(cert, Base64UrlHelpers.Encode(cert.GetCertHash()), true);
        }
    }
}