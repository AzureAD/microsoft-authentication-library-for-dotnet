// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
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

        [TestMethod]
        public async Task RegionalAuthGetSuccessfulResponseAsync()
        {
            var cca = CreateApp();
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            var result = await cca.AcquireTokenForClient(s_keyvaultScope)
                .WithAzureRegion(true)
                .WithExtraQueryParameters(_dict)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task RegionalAuthWithExperimentalFeaturesFalseAsync()
        {
            var claims = GetClaims();
            var cca = ConfidentialClientApplicationBuilder.Create(PublicCloudConfidentialClientID)
                .WithClientAssertion(GetSignedClientAssertionUsingMsalInternal(PublicCloudConfidentialClientID, claims))
                .WithAuthority(PublicCloudTestAuthority)
                .WithTestLogging()
                .Build();
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            try
            {
                await cca.AcquireTokenForClient(s_keyvaultScope)
                .WithAzureRegion(true)
                .WithExtraQueryParameters(_dict)
                .ExecuteAsync()
                .ConfigureAwait(false);

                Assert.Fail("This request should fail as Experiment feature is not set to true.");
            }
            catch (MsalClientException e)
            {
                Assert.AreEqual(MsalError.ExperimentalFeature, e.ErrorCode);
            }
        }

        [TestMethod]
        public async Task RegionalAuthRegionUndiscoveredAsync()
        {
            TestCommon.ResetInternalStaticCaches();
            var cca = CreateApp();
            Environment.SetEnvironmentVariable(TestConstants.RegionName, "");

            try
            {
                await cca.AcquireTokenForClient(s_keyvaultScope)
                .WithAzureRegion(true)
                .WithExtraQueryParameters(_dict)
                .ExecuteAsync()
                .ConfigureAwait(false);

                // If this is triggered that means the region was either discovered or not cleared from the cache.
                Assert.Fail("The region should not get discovered.");
            }
            catch (MsalClientException e)
            {
                Assert.AreEqual(MsalError.RegionDiscoveryFailed, e.ErrorCode);
            }
        }

        private IConfidentialClientApplication CreateApp()
        {
            var claims = GetClaims();

            return ConfidentialClientApplicationBuilder.Create(PublicCloudConfidentialClientID)
                .WithClientAssertion(GetSignedClientAssertionUsingMsalInternal(PublicCloudConfidentialClientID, claims))
                .WithAuthority(PublicCloudTestAuthority)
                .WithTestLogging()
                .WithExperimentalFeatures(true)
                .Build();
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
    }
}
