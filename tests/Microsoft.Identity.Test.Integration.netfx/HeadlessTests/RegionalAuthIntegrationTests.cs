// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class RegionalAuthIntegrationTests
    {
        private static readonly string[] s_keyvaultScope = { "https://vault.azure.net/.default" };

        private const string PublicCloudConfidentialClientID = "16dab2ba-145d-4b1b-8569-bf4b9aed4dc8";
        private const string PublicCloudTestAuthority = "https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47";
        private KeyVaultSecretsProvider _keyVault;
        private Dictionary<string, string> _dict = new Dictionary<string, string>
        {
            ["allowestsrnonmsi"] = "true"
        };

        private const string RegionalHost = "centralus.login.microsoft.com";
        private const string GlobalHost = "login.microsoftonline.com";
        private IConfidentialClientApplication _confidentialClientApplication;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

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
        public async Task AcquireTokenToRegionalEndpointAsync()
        {
            // Arrange
            var factory = new HttpSnifferClientFactory();
            _confidentialClientApplication = BuildCCA(factory);

            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);
            AuthenticationResult result = await GetAuthenticationResultAsync().ConfigureAwait(false); // regional endpoint
            AssertTokenSourceIsIdp(result);
            AssertValidHost(true, factory);
            AssertTelemetry(factory, $"{TelemetryConstants.HttpTelemetrySchemaVersion}|1004,{CacheInfoTelemetry.NoCachedAT:D},centralus,3,4|0,1,0,0,0,0");
        }

        [TestMethod]
        public async Task InvalidRegion_GoesToInvalidAuthority_Async()
        {
            // Arrange
            var factory = new HttpSnifferClientFactory();
            _confidentialClientApplication = BuildCCA(factory, "invalid");

            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);
            AuthenticationResult result = await GetAuthenticationResultAsync().ConfigureAwait(false); // regional endpoint
            AssertTokenSourceIsIdp(result);
            Assert.AreEqual(
              "https://invalid.login.microsoft.com/72f988bf-86f1-41af-91ab-2d7cd011db47/oauth2/v2.0/token?allowestsrnonmsi=true",
              factory.RequestsAndResponses.Single().Item1.RequestUri.ToString());

            AssertTelemetry(factory, $"{TelemetryConstants.HttpTelemetrySchemaVersion}|1004,{CacheInfoTelemetry.NoCachedAT:D},invalid,3,3|0,1,0,0,0,0");

            _confidentialClientApplication = BuildCCA(factory, TestConstants.Region);
            result = await GetAuthenticationResultAsync(withForceRefresh: true).ConfigureAwait(false); // regional endpoint
            AssertTokenSourceIsIdp(result);
            AssertValidHost(true, factory, 1);
            AssertTelemetry(factory, $"{TelemetryConstants.HttpTelemetrySchemaVersion}|1004,{CacheInfoTelemetry.ForceRefresh:D},centralus,2,1|0,1,0,0,0,0", 1);
           
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

        private IConfidentialClientApplication BuildCCA(HttpSnifferClientFactory factory, string region = ConfidentialClientApplication.AttemptRegionDiscovery)
        {
            var builder = ConfidentialClientApplicationBuilder.Create(PublicCloudConfidentialClientID)
                .WithClientAssertion(GetSignedClientAssertionUsingMsalInternal(PublicCloudConfidentialClientID, GetClaims()))
                .WithAuthority(PublicCloudTestAuthority)
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
            bool withForceRefresh = false)
        {
            ResetMetrics();
            var result = await _confidentialClientApplication.AcquireTokenForClient(s_keyvaultScope)
                            .WithExtraQueryParameters(_dict)
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

        private static IDictionary<string, string> GetClaims(bool useDefaultClaims = true)
        {
            if (useDefaultClaims)
            {
                DateTime validFrom = DateTime.UtcNow;
                var nbf = ConvertToTimeT(validFrom);
                var exp = ConvertToTimeT(validFrom + TimeSpan.FromSeconds(TestConstants.JwtToAadLifetimeInSeconds));

                return new Dictionary<string, string>()
                {
                { "aud", TestConstants.ClientCredentialAudience },
                { "exp", exp.ToString(CultureInfo.InvariantCulture) },
                { "iss", PublicCloudConfidentialClientID.ToString(CultureInfo.InvariantCulture) },
                { "jti", Guid.NewGuid().ToString() },
                { "nbf", nbf.ToString(CultureInfo.InvariantCulture) },
                { "sub", PublicCloudConfidentialClientID.ToString(CultureInfo.InvariantCulture) },
                { "ip", "192.168.2.1" }
                };
            }
            else
            {
                return new Dictionary<string, string>()
                {
                    { "ip", "192.168.2.1" }
                };
            }
        }

        private static string GetSignedClientAssertionUsingMsalInternal(string clientId, IDictionary<string, string> claims)
        {
#if NET_CORE
            var manager = new Client.Platforms.netcore.NetCoreCryptographyManager();
#else
            var manager = new Client.Platforms.net45.NetDesktopCryptographyManager();
#endif
            var jwtToken = new Client.Internal.JsonWebToken(manager, clientId, TestConstants.ClientCredentialAudience, claims);
            var clientCredential = ClientCredentialWrapper.CreateWithCertificate(GetCertificate(), claims);
            return jwtToken.Sign(clientCredential, false);
        }

        private static X509Certificate2 GetCertificate(bool useRSACert = false)
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByThumbprint(useRSACert ?
                TestConstants.RSATestCertThumbprint :
                TestConstants.AutomationTestThumbprint);
            if (cert == null)
            {
                throw new InvalidOperationException(
                    "Test setup error - cannot find a certificate in the My store for KeyVault. This is available for Microsoft employees only.");
            }

            return cert;
        }

        private void ResetMetrics()
        {
            Metrics.TotalAccessTokensFromIdP = 0;
            Metrics.TotalAccessTokensFromCache = 0;
            Metrics.TotalAccessTokensFromBroker = 0;
            Metrics.TotalDurationInMs = 0;
        }
    }
}
