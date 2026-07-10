// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    public class InstanceProviderTests : TestBase
    {
        private const string LoginMicrosoftOnlineCom = "login.microsoftonline.com";
        private readonly ILoggerAdapter _logger = new NullLogger();

        [TestMethod]
        public void StaticProviderPreservesStateAcrossInstances()
        {
            // Arrange
            NetworkCacheMetadataProvider staticMetadataProvider1 = new NetworkCacheMetadataProvider();
            NetworkCacheMetadataProvider staticMetadataProvider2 = new NetworkCacheMetadataProvider();
            staticMetadataProvider1.AddMetadata("env", new InstanceDiscoveryMetadataEntry());

            // Act
            InstanceDiscoveryMetadataEntry result = staticMetadataProvider2.GetMetadata("env", _logger);
            NetworkCacheMetadataProvider.ResetStaticCacheForTest();
            InstanceDiscoveryMetadataEntry result2 = staticMetadataProvider2.GetMetadata("env", _logger);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result2);
        }

        [TestMethod]
        public void StaticProviderClearsCacheWhenEntryLimitIsExceeded()
        {
            // Arrange
            NetworkCacheMetadataProvider.ResetStaticCacheForTest();
            var staticMetadataProvider = new NetworkCacheMetadataProvider();
            try
            {
                // Act
                for (int i = 0; i < NetworkCacheMetadataProvider.MaxCacheEntries; i++)
                {
                    staticMetadataProvider.AddMetadata($"env-{i}", new InstanceDiscoveryMetadataEntry());
                }

                // Assert
                Assert.IsNull(staticMetadataProvider.GetMetadata("env-0", _logger));
                Assert.IsNull(staticMetadataProvider.GetMetadata($"env-{NetworkCacheMetadataProvider.MaxCacheEntries - 1}", _logger));
            }
            finally
            {
                NetworkCacheMetadataProvider.ResetStaticCacheForTest();
            }
        }

        [TestMethod]
        public void KnownMetadataProvider_RespondsIfEnvironmentsAreKnown()
        {
            // Arrange
            KnownMetadataProvider knownMetadataProvider = new KnownMetadataProvider();

            InstanceDiscoveryMetadataEntry result = knownMetadataProvider.GetMetadata(
                 LoginMicrosoftOnlineCom, null, _logger);
            Assert.IsNotNull(result);

            result = knownMetadataProvider.GetMetadata(
                LoginMicrosoftOnlineCom, Enumerable.Empty<string>(), _logger);
            Assert.IsNotNull(result);

            result = knownMetadataProvider.GetMetadata(
                LoginMicrosoftOnlineCom, new[] { LoginMicrosoftOnlineCom }, _logger);
            Assert.IsNotNull(result);

            result = knownMetadataProvider.GetMetadata(
                LoginMicrosoftOnlineCom, new[] { LoginMicrosoftOnlineCom }, _logger);
            Assert.IsNotNull(result);

            result = knownMetadataProvider.GetMetadata(
                LoginMicrosoftOnlineCom, new[] { "login.windows.net", "login.microsoft.com", "login.partner.microsoftonline.cn" }, _logger);
            Assert.IsNotNull(result);

            result = knownMetadataProvider.GetMetadata(
                "login.partner.microsoftonline.cn", new[] { "login.windows.net", "login.microsoft.com", "login.partner.microsoftonline.cn" }, _logger);
            Assert.IsNotNull(result);

            result = knownMetadataProvider.GetMetadata(
                "login.windows-ppe.net", new[] { "login.windows-ppe.net", "sts.windows-ppe.net", "login.microsoft-ppe.com" }, _logger);
            Assert.IsNotNull(result);

            result = knownMetadataProvider.GetMetadata(
               LoginMicrosoftOnlineCom, new[] { "login.windows.net", "bogus", "login.partner.microsoftonline.cn" }, _logger);
            Assert.IsNull(result);

            result = knownMetadataProvider.GetMetadata(
                "bogus", new[] { "login.windows.net", "login.microsoft.com", "login.partner.microsoftonline.cn" }, _logger);
            Assert.IsNull(result);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\CustomInstanceMetadata.json")]
        public void UserMetadataProvider_RespondsIfEnvironmentsAreKnown()
        {
            // Arrange
            string instanceMetadataJson = File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("CustomInstanceMetadata.json"));
            InstanceDiscoveryResponse instanceDiscovery = JsonHelper.DeserializeFromJson<InstanceDiscoveryResponse>(instanceMetadataJson);

            UserMetadataProvider userMetadataProvider = new UserMetadataProvider(instanceDiscovery);

            // Act
            InstanceDiscoveryMetadataEntry result = userMetadataProvider.GetMetadataOrThrow("login.microsoftonline.com", _logger);

            // Assert
            Assert.AreEqual("login.microsoftonline.com", result.PreferredNetwork);
            Assert.AreEqual("login.windows.net", result.PreferredCache);
            Assert.IsTrue(Enumerable.SequenceEqual
                (new[] { "login.microsoftonline.com", "login.windows.net" },
                result.Aliases));

            InstanceDiscoveryMetadataEntry result2 = userMetadataProvider.GetMetadataOrThrow("login.windows.net", _logger);
            Assert.AreSame(result, result2);

            InstanceDiscoveryMetadataEntry result3 = userMetadataProvider.GetMetadataOrThrow("login.partner.microsoftonline.cn", _logger);
            Assert.IsNotNull(result3);

            MsalClientException ex;
            ex = Assert.Throws<MsalClientException>(() => userMetadataProvider.GetMetadataOrThrow("non_existent", _logger));
            Assert.AreEqual(MsalError.InvalidUserInstanceMetadata, ex.ErrorCode);
            ex = Assert.Throws<MsalClientException>(() => userMetadataProvider.GetMetadataOrThrow(null, _logger));
            Assert.AreEqual(MsalError.InvalidUserInstanceMetadata, ex.ErrorCode);
            ex = Assert.Throws<MsalClientException>(() => userMetadataProvider.GetMetadataOrThrow("", _logger));
            Assert.AreEqual(MsalError.InvalidUserInstanceMetadata, ex.ErrorCode);
        }

        [TestMethod]
        public void KnownMetadataProvider_IsKnown()
        {
            Assert.IsFalse(KnownMetadataProvider.IsKnownEnvironment(null));
            Assert.IsFalse(KnownMetadataProvider.IsKnownEnvironment(""));
            Assert.IsFalse(KnownMetadataProvider.IsKnownEnvironment("bogus"));

            Assert.IsTrue(KnownMetadataProvider.IsKnownEnvironment("login.microsoftonline.de"));
            Assert.IsTrue(KnownMetadataProvider.IsKnownEnvironment("LOGIN.microsoftonline.de"));
            
            // New sovereign clouds
            Assert.IsTrue(KnownMetadataProvider.IsKnownEnvironment("login.sovcloud-identity.fr"));
            Assert.IsTrue(KnownMetadataProvider.IsKnownEnvironment("LOGIN.sovcloud-identity.fr"));
            Assert.IsTrue(KnownMetadataProvider.IsKnownEnvironment("login.sovcloud-identity.de"));
            Assert.IsTrue(KnownMetadataProvider.IsKnownEnvironment("LOGIN.sovcloud-identity.de"));
            Assert.IsTrue(KnownMetadataProvider.IsKnownEnvironment("login.sovcloud-identity.sg"));
            Assert.IsTrue(KnownMetadataProvider.IsKnownEnvironment("LOGIN.sovcloud-identity.sg"));
        }

        [TestMethod]
        public void KnownMetadataProvider_publicEnvironment()
        {
            Assert.IsFalse(KnownMetadataProvider.IsPublicEnvironment(""));
            Assert.IsFalse(KnownMetadataProvider.IsPublicEnvironment(null));
            Assert.IsFalse(KnownMetadataProvider.IsPublicEnvironment("unknown"));
            Assert.IsFalse(KnownMetadataProvider.IsPublicEnvironment("login.microsoftonline.de"));

            Assert.IsTrue(KnownMetadataProvider.IsPublicEnvironment("login.microsoft.com"));
            Assert.IsTrue(KnownMetadataProvider.IsPublicEnvironment("login.microsoftonline.com"));
            Assert.IsTrue(KnownMetadataProvider.IsPublicEnvironment("Login.microsoftonline.com"));
            
            // New sovereign clouds should NOT be public environments
            Assert.IsFalse(KnownMetadataProvider.IsPublicEnvironment("login.sovcloud-identity.fr"));
            Assert.IsFalse(KnownMetadataProvider.IsPublicEnvironment("login.sovcloud-identity.de"));
            Assert.IsFalse(KnownMetadataProvider.IsPublicEnvironment("login.sovcloud-identity.sg"));
        }

        [TestMethod]
        [DataRow("login.sovcloud-identity.fr")]
        [DataRow("login.sovcloud-identity.de")]
        [DataRow("login.sovcloud-identity.sg")]
        public void KnownMetadataProvider_NewSovereignClouds(string host)
        {
            // Arrange
            KnownMetadataProvider knownMetadataProvider = new KnownMetadataProvider();

            // Act
            InstanceDiscoveryMetadataEntry result = knownMetadataProvider.GetMetadata(host, null, _logger);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(host, result.PreferredNetwork);
            Assert.AreEqual(host, result.PreferredCache);
            CollectionAssert.Contains(result.Aliases, host);
        }

        [TestMethod]
        public void KnownCloudConfiguration_ReturnsSettingsForAllKnownClouds()
        {
            // Arrange
            var config = KnownCloudConfiguration.Default;

            string[] knownHosts = new[]
            {
                "login.microsoftonline.com",
                "login.windows.net",
                "login.microsoft.com",
                "sts.windows.net",
                "login.partner.microsoftonline.cn",
                "login.chinacloudapi.cn",
                "login.microsoftonline.de",
                "login.microsoftonline.us",
                "login.usgovcloudapi.net",
                "login-us.microsoftonline.com",
                "login.windows-ppe.net",
                "sts.windows-ppe.net",
                "login.microsoft-ppe.com",
                "login.sovcloud-identity.fr",
                "login.sovcloud-identity.de",
                "login.sovcloud-identity.sg",
            };

            // Act & Assert
            foreach (string host in knownHosts)
            {
                CloudSettings settings = config.GetSettingsByAuthority(host);
                Assert.IsNotNull(settings, $"Expected non-null settings for '{host}'");
                Assert.IsNotNull(settings.PreferredNetwork, $"Expected PreferredNetwork for '{host}'");
                Assert.IsNotNull(settings.PreferredCache, $"Expected PreferredCache for '{host}'");
                Assert.IsNotNull(settings.Aliases, $"Expected Aliases for '{host}'");
                Assert.AreNotEqual(0, settings.Aliases.Count, $"Expected at least one alias for '{host}'");
            }
        }

        [TestMethod]
        public void KnownCloudConfiguration_AliasesResolveToSameInstance()
        {
            // Arrange
            var config = KnownCloudConfiguration.Default;

            // Act
            CloudSettings settings1 = config.GetSettingsByAuthority("login.microsoftonline.com");
            CloudSettings settings2 = config.GetSettingsByAuthority("login.windows.net");
            CloudSettings settings3 = config.GetSettingsByAuthority("login.microsoft.com");
            CloudSettings settings4 = config.GetSettingsByAuthority("sts.windows.net");

            // Assert
            Assert.AreSame(settings1, settings2);
            Assert.AreSame(settings2, settings3);
            Assert.AreSame(settings3, settings4);
        }

        [TestMethod]
        public void KnownCloudConfiguration_CaseInsensitiveLookup()
        {
            // Arrange
            var config = KnownCloudConfiguration.Default;

            // Act
            CloudSettings lower = config.GetSettingsByAuthority("login.microsoftonline.com");
            CloudSettings upper = config.GetSettingsByAuthority("LOGIN.MICROSOFTONLINE.COM");
            CloudSettings mixed = config.GetSettingsByAuthority("Login.MicrosoftOnline.Com");

            // Assert
            Assert.AreSame(lower, upper);
            Assert.AreSame(upper, mixed);
        }

        [TestMethod]
        public void KnownCloudConfiguration_ReturnsNullForUnknown()
        {
            // Arrange
            var config = KnownCloudConfiguration.Default;

            // Act & Assert
            Assert.IsNull(config.GetSettingsByAuthority("bogus.example.com"));
            Assert.IsNull(config.GetSettingsByAuthority(""));
            Assert.IsNull(config.GetSettingsByAuthority(null));
        }

        [TestMethod]
        [DataRow("login.microsoftonline.com", "api://AzureADTokenExchange")]
        [DataRow("login.windows.net", "api://AzureADTokenExchange")]
        [DataRow("login.partner.microsoftonline.cn", "api://AzureADTokenExchangeChina")]
        [DataRow("login.microsoftonline.us", "api://AzureADTokenExchangeUSGov")]
        [DataRow("login.usgovcloudapi.net", "api://AzureADTokenExchangeUSGov")]
        [DataRow("login.sovcloud-identity.fr", "api://AzureADTokenExchangeFrance")]
        [DataRow("login.sovcloud-identity.de", "api://AzureADTokenExchangeGermany")]
        public void KnownCloudConfiguration_TokenExchangeAudience_KnownClouds(
            string host, string expectedAudience)
        {
            // Arrange
            var config = KnownCloudConfiguration.Default;

            // Act
            CloudSettings settings = config.GetSettingsByAuthority(host);

            // Assert
            Assert.IsNotNull(settings);
            Assert.AreEqual(expectedAudience, settings.TokenExchangeAudience);
        }

        [TestMethod]
        [DataRow("login.microsoftonline.de")]
        [DataRow("login-us.microsoftonline.com")]
        [DataRow("login.windows-ppe.net")]
        [DataRow("login.sovcloud-identity.sg")]
        public void KnownCloudConfiguration_TokenExchangeAudience_NullForCloudsWithoutFic(string host)
        {
            // Arrange
            var config = KnownCloudConfiguration.Default;

            // Act
            CloudSettings settings = config.GetSettingsByAuthority(host);

            // Assert
            Assert.IsNotNull(settings);
            Assert.IsNull(settings.TokenExchangeAudience);
        }

        [TestMethod]
        public void KnownCloudConfiguration_PreferredNetworkAndCache_MatchKnownMetadata()
        {
            // Arrange — verify consistency with KnownMetadataProvider
            var cloudConfig = KnownCloudConfiguration.Default;
            var knownMetadata = new KnownMetadataProvider();

            string[] primaryHosts = new[]
            {
                "login.microsoftonline.com",
                "login.partner.microsoftonline.cn",
                "login.microsoftonline.de",
                "login.microsoftonline.us",
                "login-us.microsoftonline.com",
                "login.windows-ppe.net",
                "login.sovcloud-identity.fr",
                "login.sovcloud-identity.de",
                "login.sovcloud-identity.sg",
            };

            foreach (string host in primaryHosts)
            {
                // Act
                CloudSettings cloud = cloudConfig.GetSettingsByAuthority(host);
                InstanceDiscoveryMetadataEntry metadata = knownMetadata.GetMetadata(host, null, _logger);

                // Assert
                Assert.IsNotNull(cloud, $"CloudSettings missing for '{host}'");
                Assert.IsNotNull(metadata, $"KnownMetadata missing for '{host}'");
                Assert.AreEqual(metadata.PreferredNetwork, cloud.PreferredNetwork, $"PreferredNetwork mismatch for '{host}'");
                Assert.AreEqual(metadata.PreferredCache, cloud.PreferredCache, $"PreferredCache mismatch for '{host}'");
                CollectionAssert.AreEquivalent(metadata.Aliases, (System.Collections.ICollection)cloud.Aliases, $"Aliases mismatch for '{host}'");
            }
        }

        [TestMethod]
        public void KnownCloudConfiguration_DefaultIsSingleton()
        {
            // Act
            var instance1 = KnownCloudConfiguration.Default;
            var instance2 = KnownCloudConfiguration.Default;

            // Assert
            Assert.AreSame(instance1, instance2);
        }

        [TestMethod]
        public void WithCloudConfiguration_SetsOnApplicationConfiguration()
        {
            // Arrange
            var customConfig = new TestCloudConfiguration();

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create("client-id")
                .WithClientSecret("secret")
                .WithCloudConfiguration(customConfig)
                .Build();

            // Assert
            var cca = (ConfidentialClientApplication)app;
            Assert.AreSame(customConfig, cca.ServiceBundle.Config.CloudConfiguration);
        }

        private class TestCloudConfiguration : ICloudConfiguration
        {
            public CloudSettings GetSettingsByAuthority(string authorityHost)
            {
                if (string.Equals(authorityHost, "custom.cloud.example", System.StringComparison.OrdinalIgnoreCase))
                {
                    return new CloudSettings
                    {
                        PreferredNetwork = "custom.cloud.example",
                        PreferredCache = "custom.cloud.example",
                        Aliases = new[] { "custom.cloud.example" },
                        TokenExchangeAudience = "api://CustomTokenExchange",
                    };
                }

                return null;
            }
        }
    }
}
