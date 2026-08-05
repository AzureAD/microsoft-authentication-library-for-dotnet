// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
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
                CloudSettings settings = config.GetSettingsByAuthorityHost(host);
                Assert.IsNotNull(settings, $"Expected non-null settings for '{host}'");
            }
        }

        [TestMethod]
        public void KnownCloudConfiguration_AliasesResolveToSameInstance()
        {
            // Arrange
            var config = KnownCloudConfiguration.Default;

            // Act
            CloudSettings settings1 = config.GetSettingsByAuthorityHost("login.microsoftonline.com");
            CloudSettings settings2 = config.GetSettingsByAuthorityHost("login.windows.net");
            CloudSettings settings3 = config.GetSettingsByAuthorityHost("login.microsoft.com");
            CloudSettings settings4 = config.GetSettingsByAuthorityHost("sts.windows.net");

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
            CloudSettings lower = config.GetSettingsByAuthorityHost("login.microsoftonline.com");
            CloudSettings upper = config.GetSettingsByAuthorityHost("LOGIN.MICROSOFTONLINE.COM");
            CloudSettings mixed = config.GetSettingsByAuthorityHost("Login.MicrosoftOnline.Com");

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
            Assert.IsNull(config.GetSettingsByAuthorityHost("bogus.example.com"));
            Assert.IsNull(config.GetSettingsByAuthorityHost(""));
            Assert.IsNull(config.GetSettingsByAuthorityHost(null));
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
            CloudSettings settings = config.GetSettingsByAuthorityHost(host);

            // Assert
            Assert.IsNotNull(settings);
            Assert.AreEqual(expectedAudience, settings.TokenExchangeAudience());
        }

        [TestMethod]
        [DataRow("login.microsoftonline.com", "api://AzureADTokenExchange/.default")]
        [DataRow("login.partner.microsoftonline.cn", "api://AzureADTokenExchangeChina/.default")]
        [DataRow("login.microsoftonline.us", "api://AzureADTokenExchangeUSGov/.default")]
        public void KnownCloudConfiguration_TokenExchangeScope_AppendsDefaultSuffix(
            string host, string expectedScope)
        {
            // Arrange
            var config = KnownCloudConfiguration.Default;

            // Act
            CloudSettings settings = config.GetSettingsByAuthorityHost(host);

            // Assert — the audience is stored bare; the scope is computed with "/.default".
            Assert.IsNotNull(settings);
            Assert.IsFalse(settings.TokenExchangeAudience().EndsWith("/.default", System.StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual(expectedScope, settings.TokenExchangeScope());
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
            CloudSettings settings = config.GetSettingsByAuthorityHost(host);

            // Assert
            Assert.IsNotNull(settings);
            Assert.IsNull(settings.TokenExchangeAudience());
            Assert.IsNull(settings.TokenExchangeScope());
        }

        [TestMethod]
        public void KnownMetadataProvider_Aliases_MatchKnownCloudData()
        {
            // Arrange — KnownMetadataProvider projects its alias sets from KnownCloudData, the single
            // source of truth. This guards against drift between the internal metadata table and the data.
            // (The public KnownCloudConfiguration projects from the same data; preferred hosts are
            // deliberately not exposed on the public CloudSettings bag.)
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
                InstanceDiscoveryMetadataEntry metadata = knownMetadata.GetMetadata(host, null, _logger);
                KnownCloudEntry source = KnownCloudData.Entries.Single(
                    e => e.Aliases.Contains(host, System.StringComparer.OrdinalIgnoreCase));

                // Assert
                Assert.IsNotNull(metadata, $"KnownMetadata missing for '{host}'");
                CollectionAssert.AreEquivalent(source.Aliases, metadata.Aliases, $"Aliases mismatch for '{host}'");
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
        public void InMemoryCloudConfiguration_InjectsNewCloud_AndFallsBackToDefault()
        {
            // Arrange — register a brand-new cloud MSAL doesn't ship, layered over the built-in defaults.
            var provider = new InMemoryCloudConfiguration(fallback: KnownCloudConfiguration.Default)
                .AddOrUpdate("login.mynewcloud.example", new Dictionary<string, string>
                {
                    [MsalCloudKeys.TokenExchangeAudience] = "api://AzureADTokenExchangeMyCloud",
                });

            // Act
            CloudSettings injected = provider.GetSettingsByAuthorityHost("login.mynewcloud.example");
            CloudSettings known = provider.GetSettingsByAuthorityHost("login.microsoftonline.com");
            CloudSettings unknown = provider.GetSettingsByAuthorityHost("bogus.example.com");

            // Assert — the new cloud resolves, and the fallback still supplies MSAL's known clouds.
            Assert.IsNotNull(injected);
            Assert.AreEqual("api://AzureADTokenExchangeMyCloud", injected.TokenExchangeAudience());
            Assert.AreEqual("api://AzureADTokenExchangeMyCloud/.default", injected.TokenExchangeScope());
            Assert.IsNotNull(known);
            Assert.AreEqual("api://AzureADTokenExchange", known.TokenExchangeAudience());
            Assert.IsNull(unknown);
        }

        [TestMethod]
        public void InMemoryCloudConfiguration_AdjustsExistingCloud_OverrideWins()
        {
            // Arrange — override an existing (public) cloud's audience.
            var provider = new InMemoryCloudConfiguration(fallback: KnownCloudConfiguration.Default)
                .AddOrUpdate("login.microsoftonline.us", new Dictionary<string, string>
                {
                    [MsalCloudKeys.TokenExchangeAudience] = "api://AzureADTokenExchangeUSGovCustom",
                });

            // Act
            CloudSettings overridden = provider.GetSettingsByAuthorityHost("login.microsoftonline.us");

            // Assert — the registered value wins over the shipped USGov value.
            Assert.IsNotNull(overridden);
            Assert.AreEqual("api://AzureADTokenExchangeUSGovCustom", overridden.TokenExchangeAudience());
        }

        [TestMethod]
        public void InMemoryCloudConfiguration_NoFallback_ReturnsNullForUnregisteredHost()
        {
            // Arrange
            var provider = new InMemoryCloudConfiguration()
                .AddOrUpdate("login.mynewcloud.example", new Dictionary<string, string>
                {
                    [MsalCloudKeys.TokenExchangeAudience] = "api://AzureADTokenExchangeMyCloud",
                });

            // Act & Assert
            Assert.IsNotNull(provider.GetSettingsByAuthorityHost("login.mynewcloud.example"));
            Assert.IsNull(provider.GetSettingsByAuthorityHost("login.microsoftonline.com"));
        }

        [TestMethod]
        public void InMemoryCloudConfiguration_PerKeyOverride_KnownCloud_Wins()
        {
            // Adjust a single value of a cloud MSAL ships, via the per-key overload.
            var provider = new InMemoryCloudConfiguration(fallback: KnownCloudConfiguration.Default)
                .AddOrUpdate("login.microsoftonline.us", MsalCloudKeys.TokenExchangeAudience, "api://Custom");

            CloudSettings s = provider.GetSettingsByAuthorityHost("login.microsoftonline.us");

            Assert.IsNotNull(s);
            Assert.AreEqual("api://Custom", s.TokenExchangeAudience());
        }

        [TestMethod]
        public void InMemoryCloudConfiguration_PerKeyAdd_KnownCloud_KeepsFallbackValues()
        {
            // Add a brand-new key to a known cloud; the fallback's existing keys still resolve.
            var provider = new InMemoryCloudConfiguration(fallback: KnownCloudConfiguration.Default)
                .AddOrUpdate("login.microsoftonline.com", "future_key", "future_value");

            CloudSettings s = provider.GetSettingsByAuthorityHost("login.microsoftonline.com");

            Assert.IsNotNull(s);
            Assert.AreEqual("api://AzureADTokenExchange", s.TokenExchangeAudience()); // fallback preserved
            Assert.AreEqual("future_value", s.GetValueOrDefault("future_key"));       // new key added
        }

        [TestMethod]
        public void InMemoryCloudConfiguration_PerKey_MergesWithPriorValues_ForSameHost()
        {
            // A later per-key override wins; sibling keys registered earlier are preserved.
            var provider = new InMemoryCloudConfiguration()
                .AddOrUpdate("login.mynewcloud.example", new Dictionary<string, string>
                {
                    [MsalCloudKeys.TokenExchangeAudience] = "api://A",
                    ["other_key"] = "keep",
                })
                .AddOrUpdate("login.mynewcloud.example", MsalCloudKeys.TokenExchangeAudience, "api://B");

            CloudSettings s = provider.GetSettingsByAuthorityHost("login.mynewcloud.example");

            Assert.IsNotNull(s);
            Assert.AreEqual("api://B", s.TokenExchangeAudience());      // overridden
            Assert.AreEqual("keep", s.GetValueOrDefault("other_key")); // sibling preserved
        }

        [TestMethod]
        public void CloudSettings_Accessors_PresentMissingAndNullKey()
        {
            var settings = new CloudSettings(new Dictionary<string, string>
            {
                [MsalCloudKeys.TokenExchangeAudience] = "api://Present",
            });

            // Present key.
            Assert.IsTrue(settings.TryGetValue(MsalCloudKeys.TokenExchangeAudience, out string present));
            Assert.AreEqual("api://Present", present);
            Assert.AreEqual("api://Present", settings.GetValueOrDefault(MsalCloudKeys.TokenExchangeAudience));

            // Missing key → false / null, no throw.
            Assert.IsFalse(settings.TryGetValue("missing_key", out string missing));
            Assert.IsNull(missing);
            Assert.IsNull(settings.GetValueOrDefault("missing_key"));

            // Null key → false / null, no throw.
            Assert.IsFalse(settings.TryGetValue(null, out string nullKey));
            Assert.IsNull(nullKey);
            Assert.IsNull(settings.GetValueOrDefault(null));
        }

        [TestMethod]
        public void CloudSettings_NullValues_TreatedAsEmpty()
        {
            var settings = new CloudSettings(null);

            Assert.IsNotNull(settings.Values);
            Assert.IsEmpty(settings.Values);
            Assert.IsNull(settings.GetValueOrDefault(MsalCloudKeys.TokenExchangeAudience));
        }

        [TestMethod]
        public void InMemoryCloudConfiguration_AddOrUpdate_ValidatesArguments()
        {
            var provider = new InMemoryCloudConfiguration();

            // Per-cloud overload. Host is rejected for null/empty/whitespace (ArgumentException); a null
            // values bag is an ArgumentNullException. Kept consistent with the Abstractions and MISE twins.
            AssertException.Throws<System.ArgumentException>(
                () => provider.AddOrUpdate(null, new Dictionary<string, string>()));
            AssertException.Throws<System.ArgumentException>(
                () => provider.AddOrUpdate("", new Dictionary<string, string>()));
            AssertException.Throws<System.ArgumentException>(
                () => provider.AddOrUpdate("   ", new Dictionary<string, string>()));
            AssertException.Throws<System.ArgumentNullException>(
                () => provider.AddOrUpdate("login.example.com", (IReadOnlyDictionary<string, string>)null));

            // Per-key overload. Host and key are rejected for null/empty/whitespace (ArgumentException); a
            // null value is an ArgumentNullException.
            AssertException.Throws<System.ArgumentException>(
                () => provider.AddOrUpdate(null, MsalCloudKeys.TokenExchangeAudience, "api://X"));
            AssertException.Throws<System.ArgumentException>(
                () => provider.AddOrUpdate("", MsalCloudKeys.TokenExchangeAudience, "api://X"));
            AssertException.Throws<System.ArgumentException>(
                () => provider.AddOrUpdate("   ", MsalCloudKeys.TokenExchangeAudience, "api://X"));
            AssertException.Throws<System.ArgumentException>(
                () => provider.AddOrUpdate("login.example.com", null, "api://X"));
            AssertException.Throws<System.ArgumentException>(
                () => provider.AddOrUpdate("login.example.com", "", "api://X"));
            AssertException.Throws<System.ArgumentException>(
                () => provider.AddOrUpdate("login.example.com", "   ", "api://X"));
            AssertException.Throws<System.ArgumentNullException>(
                () => provider.AddOrUpdate("login.example.com", MsalCloudKeys.TokenExchangeAudience, null));
        }

        [TestMethod]
        public void InMemoryCloudConfiguration_PerKey_GrowsFromNullFallback_ForNoFicCloud()
        {
            // login.microsoftonline.de is a known cloud that ships no FIC audience (null). Adding the
            // audience via the per-key overload makes the computed scope resolve where it was null before.
            const string noFicHost = "login.microsoftonline.de";
            Assert.IsNull(
                KnownCloudConfiguration.Default.GetSettingsByAuthorityHost(noFicHost).TokenExchangeAudience(),
                "precondition: the known cloud should ship no FIC audience.");

            var provider = new InMemoryCloudConfiguration(fallback: KnownCloudConfiguration.Default)
                .AddOrUpdate(noFicHost, MsalCloudKeys.TokenExchangeAudience, "api://AzureADTokenExchangeGrown");

            CloudSettings s = provider.GetSettingsByAuthorityHost(noFicHost);

            Assert.IsNotNull(s);
            Assert.AreEqual("api://AzureADTokenExchangeGrown", s.TokenExchangeAudience());
            Assert.AreEqual("api://AzureADTokenExchangeGrown/.default", s.TokenExchangeScope());
        }

        [TestMethod]
        public void InMemoryCloudConfiguration_PerKeyOnly_NoFallback_BuildsCloudIncrementally()
        {
            // Build a cloud entirely from per-key calls with no fallback, then confirm it resolves and
            // unregistered hosts stay null.
            var provider = new InMemoryCloudConfiguration()
                .AddOrUpdate("login.mynewcloud.example", MsalCloudKeys.TokenExchangeAudience, "api://Built")
                .AddOrUpdate("login.mynewcloud.example", "other_key", "value");

            CloudSettings s = provider.GetSettingsByAuthorityHost("login.mynewcloud.example");

            Assert.IsNotNull(s);
            Assert.AreEqual("api://Built", s.TokenExchangeAudience());
            Assert.AreEqual("value", s.GetValueOrDefault("other_key"));
            Assert.IsNull(provider.GetSettingsByAuthorityHost("login.microsoftonline.com"));
        }
    }
}
