// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
#if NET_CORE
using Microsoft.Identity.Client.PlatformsCommon.Shared;
#endif
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
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
        // TODO: TENANT MIGRATION - These tests currently use original tenant configuration
        // Regional endpoints (eastus2.login.microsoft.com) return AADSTS100007 with new tenant
        // "Only managed identities and Microsoft internal service identities are supported"
        // Regional endpoints are restricted by Azure AD policy for regular app registrations
        
        private KeyVaultSecretsProvider _keyVault;

        private const string RegionalHost = "centralus.login.microsoft.com";
        private const string GlobalHost = "login.microsoftonline.com";
        private IConfidentialClientApplication _confidentialClientApplication;

        [TestInitialize]
        public void TestInitialize()
        {
            ApplicationBase.ResetStateForTest();

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

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task AcquireTokenToRegionalEndpointAsync(bool instanceDiscoveryEnabled)
        {
            // Arrange
            var factory = new HttpSnifferClientFactory();
            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.MsalAppAzureAdMultipleOrgsRegional).ConfigureAwait(false);
            var cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            string[] appScopes = new[] { "https://vault.azure.net/.default" };
            
            _confidentialClientApplication = BuildCCA(appConfig.AppId, appConfig.TenantId, appConfig.Authority, cert, factory, instanceDiscoveryEnabled);

            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);
            AuthenticationResult result = await GetAuthenticationResultAsync(appScopes).ConfigureAwait(false); // regional endpoint
            AssertTokenSourceIsIdp(result);
            AssertValidHost(true, factory);
            AssertTelemetry(factory, $"{TelemetryConstants.HttpTelemetrySchemaVersion}|1004,{CacheRefreshReason.NoCachedAccessToken:D},centralus,3,4|0,1,1,,");
            Assert.AreEqual(
                $"https://{RegionalHost}/{appConfig.TenantId}/oauth2/v2.0/token",
                result.AuthenticationResultMetadata.TokenEndpoint);
        }

        [TestMethod]
        public async Task InvalidRegion_GoesToInvalidAuthority_Async()
        {
            // Arrange
            var factory = new HttpSnifferClientFactory();
            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.MsalAppAzureAdMultipleOrgsRegional).ConfigureAwait(false);
            var cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            string[] appScopes = new[] { "https://vault.azure.net/.default" };
            
            _confidentialClientApplication = BuildCCA(appConfig.AppId, appConfig.TenantId, appConfig.Authority, cert, factory, true, true, "invalid");

            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            var ex = await Assert.ThrowsExceptionAsync<HttpRequestException>(
                async () => await GetAuthenticationResultAsync(appScopes).ConfigureAwait(false)).ConfigureAwait(false);

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
            string clientId,
            string tenantId,
            string authority,
            X509Certificate2 cert,
            HttpSnifferClientFactory factory,
            bool instanceDiscoveryEnabled = true,
            bool useClaims = false,
            string region = ConfidentialClientApplication.AttemptRegionDiscovery)
        {
            var builder = ConfidentialClientApplicationBuilder.Create(clientId);
            if (useClaims)
            {
                builder.WithClientAssertion(() => GetSignedClientAssertionUsingMsalInternal(clientId, GetClaims(clientId, tenantId, authority)));
            }
            else
            {
                builder.WithCertificate(cert);
            }

            builder.WithAuthority(authority)
                .WithInstanceDiscovery(instanceDiscoveryEnabled)
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

        private static IDictionary<string, string> GetClaims(string clientId, string tenantId, string authority)
        {
            DateTime validFrom = DateTime.UtcNow;
            var nbf = ConvertToTimeT(validFrom);
            var exp = ConvertToTimeT(validFrom + TimeSpan.FromSeconds(TestConstants.JwtToAadLifetimeInSeconds));

            return new Dictionary<string, string>()
                {
                { "aud", $"{authority}/v2.0" },
                { "exp", exp.ToString(CultureInfo.InvariantCulture) },
                { "iss", clientId },
                { "jti", Guid.NewGuid().ToString() },
                { "nbf", nbf.ToString(CultureInfo.InvariantCulture) },
                { "sub", clientId },
                { "ip", "192.168.2.1" }
                };

        }

        private static string GetSignedClientAssertionUsingMsalInternal(string clientId, IDictionary<string, string> claims)
        {
            var manager = PlatformProxyFactory.CreatePlatformProxy(null).CryptographyManager;

            var jwtToken = new JsonWebToken(manager, clientId, TestConstants.ClientCredentialAudience, claims);
            var cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            return jwtToken.Sign(cert, true, true);
        }
    }
}
