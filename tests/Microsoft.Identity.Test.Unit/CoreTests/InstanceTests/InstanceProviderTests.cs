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
        public void KnownCloudMetadata_ReturnsMetadataForCloudsWithFic()
        {
            // Arrange — every cloud that ships a FIC audience should resolve to a non-null bag.
            var metadata = KnownCloudMetadata.Default;

            string[] ficHosts = new[]
            {
                "login.microsoftonline.com",
                "login.windows.net",
                "login.microsoft.com",
                "sts.windows.net",
                "login.partner.microsoftonline.cn",
                "login.chinacloudapi.cn",
                "login.microsoftonline.us",
                "login.usgovcloudapi.net",
                "login-us.microsoftonline.com",
                "login.windows-ppe.net",
                "login.sovcloud-identity.fr",
                "login.sovcloud-identity.de",
                "login.sovcloud-identity.sg",
            };

            // Act & Assert
            foreach (string host in ficHosts)
            {
                IReadOnlyDictionary<string, string> values = metadata.GetByAuthorityHost(host);
                Assert.IsNotNull(values, $"Expected non-null metadata for '{host}'");
                Assert.IsTrue(
                    values.ContainsKey(CloudMetadataKeyNames.FederatedCredentialAudience),
                    $"Expected FIC audience key for '{host}'");
            }
        }

        [TestMethod]
        [DataRow("login.microsoftonline.de")]
        public void KnownCloudMetadata_ReturnsNullForCloudsWithoutFic(string host)
        {
            // Arrange — known clouds that ship no FIC audience expose no metadata, so lookup returns null
            // (the same "no value available" outcome as an unknown host — no KeyNotFound footgun).
            // login.microsoftonline.de is the decommissioned Microsoft Cloud Germany, which has no
            // token-exchange application.
            var metadata = KnownCloudMetadata.Default;

            // Act & Assert
            Assert.IsNull(metadata.GetByAuthorityHost(host));
        }

        [TestMethod]
        public void KnownCloudMetadata_AliasesResolveToSameInstance()
        {
            // Arrange
            var metadata = KnownCloudMetadata.Default;

            // Act
            IReadOnlyDictionary<string, string> values1 = metadata.GetByAuthorityHost("login.microsoftonline.com");
            IReadOnlyDictionary<string, string> values2 = metadata.GetByAuthorityHost("login.windows.net");
            IReadOnlyDictionary<string, string> values3 = metadata.GetByAuthorityHost("login.microsoft.com");
            IReadOnlyDictionary<string, string> values4 = metadata.GetByAuthorityHost("sts.windows.net");

            // Assert
            Assert.AreSame(values1, values2);
            Assert.AreSame(values2, values3);
            Assert.AreSame(values3, values4);
        }

        [TestMethod]
        public void KnownCloudMetadata_CaseInsensitiveLookup()
        {
            // Arrange
            var metadata = KnownCloudMetadata.Default;

            // Act
            IReadOnlyDictionary<string, string> lower = metadata.GetByAuthorityHost("login.microsoftonline.com");
            IReadOnlyDictionary<string, string> upper = metadata.GetByAuthorityHost("LOGIN.MICROSOFTONLINE.COM");
            IReadOnlyDictionary<string, string> mixed = metadata.GetByAuthorityHost("Login.MicrosoftOnline.Com");

            // Assert
            Assert.AreSame(lower, upper);
            Assert.AreSame(upper, mixed);
        }

        [TestMethod]
        public void KnownCloudMetadata_ReturnsNullForUnknownAndEmpty()
        {
            // Arrange
            var metadata = KnownCloudMetadata.Default;

            // Act & Assert
            Assert.IsNull(metadata.GetByAuthorityHost("bogus.example.com"));
            Assert.IsNull(metadata.GetByAuthorityHost(""));
            Assert.IsNull(metadata.GetByAuthorityHost(null));
        }

        [TestMethod]
        [DataRow("login.microsoftonline.com", "api://AzureADTokenExchange")]
        [DataRow("login.windows.net", "api://AzureADTokenExchange")]
        [DataRow("login.partner.microsoftonline.cn", "api://AzureADTokenExchangeChina")]
        [DataRow("login.microsoftonline.us", "api://AzureADTokenExchangeUSGov")]
        [DataRow("login.usgovcloudapi.net", "api://AzureADTokenExchangeUSGov")]
        [DataRow("login.sovcloud-identity.fr", "api://AzureADTokenExchangeFrance")]
        [DataRow("login.sovcloud-identity.de", "api://AzureADTokenExchangeGermany")]
        [DataRow("login.sovcloud-identity.sg", "api://AzureADTokenExchangeGovSG")]
        [DataRow("login.windows-ppe.net", "api://AzureADTokenExchangePpe")]
        [DataRow("login-us.microsoftonline.com", "api://AzureADTokenExchange")]
        public void KnownCloudMetadata_FederatedCredentialAudience_KnownClouds(
            string host, string expectedAudience)
        {
            // Arrange
            var metadata = KnownCloudMetadata.Default;

            // Act
            IReadOnlyDictionary<string, string> values = metadata.GetByAuthorityHost(host);

            // Assert — the returned bag, when non-null, always contains the FIC key.
            Assert.IsNotNull(values);
            Assert.AreEqual(expectedAudience, values[CloudMetadataKeyNames.FederatedCredentialAudience]);
        }

        [TestMethod]
        public void KnownCloudMetadata_ReturnedDictionary_IsReadOnly()
        {
            // Arrange
            var metadata = KnownCloudMetadata.Default;

            // Act
            IReadOnlyDictionary<string, string> values = metadata.GetByAuthorityHost("login.microsoftonline.com");

            // Assert — the built-in bag cannot be mutated through the mutable dictionary interface.
            Assert.IsNotNull(values);
            var mutable = values as IDictionary<string, string>;
            Assert.IsNotNull(mutable, "Expected the bag to implement IDictionary for this guard.");
            AssertException.Throws<System.NotSupportedException>(
                () => mutable[CloudMetadataKeyNames.FederatedCredentialAudience] = "api://Tampered");
        }

        [TestMethod]
        public void KnownCloudMetadata_DefaultIsSingleton()
        {
            // Act
            var instance1 = KnownCloudMetadata.Default;
            var instance2 = KnownCloudMetadata.Default;

            // Assert
            Assert.AreSame(instance1, instance2);
        }

        [TestMethod]
        [DataRow("api://AzureADTokenExchange", "api://AzureADTokenExchange/.default")]
        [DataRow("api://AzureADTokenExchangeChina", "api://AzureADTokenExchangeChina/.default")]
        [DataRow("api://AzureADTokenExchangeUSGov", "api://AzureADTokenExchangeUSGov/.default")]
        public void TokenExchangeScope_FromAudience_AppendsDefaultSuffix(string audience, string expectedScope)
        {
            // Act & Assert — the audience is stored bare; the scope is computed with "/.default".
            Assert.AreEqual(expectedScope, TokenExchangeScope.FromAudience(audience));
        }

        [TestMethod]
        public void TokenExchangeScope_FromAudience_DoesNotDoubleAppendSuffix()
        {
            // Act & Assert — an audience that already carries the suffix is returned unchanged (case-insensitive).
            Assert.AreEqual(
                "api://AzureADTokenExchange/.default",
                TokenExchangeScope.FromAudience("api://AzureADTokenExchange/.default"));
            Assert.AreEqual(
                "api://AzureADTokenExchange/.DEFAULT",
                TokenExchangeScope.FromAudience("api://AzureADTokenExchange/.DEFAULT"));
        }

        [TestMethod]
        public void TokenExchangeScope_FromAudience_NullOrEmpty_ReturnsNull()
        {
            // Act & Assert
            Assert.IsNull(TokenExchangeScope.FromAudience(null));
            Assert.IsNull(TokenExchangeScope.FromAudience(""));
        }

        [TestMethod]
        public void KnownMetadataProvider_Aliases_MatchKnownCloudData()
        {
            // Arrange — KnownMetadataProvider projects its alias sets from KnownCloudData, the single
            // source of truth. This guards against drift between the internal metadata table and the data.
            // (The public KnownCloudMetadata projects from the same data; preferred hosts are deliberately
            // not exposed on the public bag.)
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
    }
}
