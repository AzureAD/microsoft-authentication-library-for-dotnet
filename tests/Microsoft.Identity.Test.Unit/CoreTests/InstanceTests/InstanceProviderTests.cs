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
        [DataRow("login.microsoftonline.com", "api://AzureADTokenExchange")]
        [DataRow("login.windows.net", "api://AzureADTokenExchange")]
        [DataRow("login.microsoft.com", "api://AzureADTokenExchange")]
        [DataRow("sts.windows.net", "api://AzureADTokenExchange")]
        [DataRow("login.microsoftonline.us", "api://AzureADTokenExchangeUSGov")]
        [DataRow("login.usgovcloudapi.net", "api://AzureADTokenExchangeUSGov")]
        [DataRow("login.partner.microsoftonline.cn", "api://AzureADTokenExchangeChina")]
        [DataRow("login.chinacloudapi.cn", "api://AzureADTokenExchangeChina")]
        [DataRow("login.sovcloud-identity.fr", "api://AzureADTokenExchangeFrance")]
        [DataRow("login.sovcloud-identity.de", "api://AzureADTokenExchangeGermany")]
        public void KnownMetadataProvider_TokenExchangeAudience_KnownClouds(string host, string expectedAudience)
        {
            // Act
            bool found = KnownMetadataProvider.TryGetTokenExchangeAudience(host, out string audience);

            // Assert
            Assert.IsTrue(found, $"Should resolve token exchange audience for {host}");
            Assert.AreEqual(expectedAudience, audience);
        }

        [TestMethod]
        [DataRow("LOGIN.MICROSOFTONLINE.COM", "api://AzureADTokenExchange")]
        [DataRow("Login.Microsoftonline.Us", "api://AzureADTokenExchangeUSGov")]
        public void KnownMetadataProvider_TokenExchangeAudience_CaseInsensitive(string host, string expectedAudience)
        {
            // Act
            bool found = KnownMetadataProvider.TryGetTokenExchangeAudience(host, out string audience);

            // Assert
            Assert.IsTrue(found);
            Assert.AreEqual(expectedAudience, audience);
        }

        [TestMethod]
        [DataRow("bogus")]
        [DataRow("")]
        [DataRow(null)]
        [DataRow("login.windows-ppe.net")]       // PPE — no token exchange audience defined
        [DataRow("login.microsoftonline.de")]     // Legacy Germany — no audience defined
        [DataRow("login.sovcloud-identity.sg")]   // GovSG — no audience defined
        public void KnownMetadataProvider_TokenExchangeAudience_UnknownOrUnsupported(string host)
        {
            // Act
            bool found = KnownMetadataProvider.TryGetTokenExchangeAudience(host, out string audience);

            // Assert
            Assert.IsFalse(found, $"Should not resolve token exchange audience for {host}");
            Assert.IsNull(audience);
        }

        [TestMethod]
        public void TokenExchangeAudience_NullForDeserializedEntries()
        {
            // Arrange — simulate a network-deserialized entry (no TokenExchangeAudience in JSON)
            var networkEntry = new InstanceDiscoveryMetadataEntry
            {
                PreferredNetwork = "login.example.com",
                PreferredCache = "login.example.com",
                Aliases = new[] { "login.example.com" }
            };

            // Assert
            Assert.IsNull(networkEntry.TokenExchangeAudience,
                "Entries created without explicit TokenExchangeAudience (e.g., from network JSON) should be null.");
        }

        [TestMethod]
        public void TokenExchangeAudience_SetOnKnownEntries()
        {
            // Arrange
            var allEntries = KnownMetadataProvider.GetAllEntriesForTest();

            // Act & Assert — verify that entries for clouds with known token exchange audiences have them set
            Assert.AreEqual("api://AzureADTokenExchange", allEntries["login.microsoftonline.com"].TokenExchangeAudience);
            Assert.AreEqual("api://AzureADTokenExchangeUSGov", allEntries["login.microsoftonline.us"].TokenExchangeAudience);
            Assert.AreEqual("api://AzureADTokenExchangeChina", allEntries["login.partner.microsoftonline.cn"].TokenExchangeAudience);
            Assert.AreEqual("api://AzureADTokenExchangeFrance", allEntries["login.sovcloud-identity.fr"].TokenExchangeAudience);
            Assert.AreEqual("api://AzureADTokenExchangeGermany", allEntries["login.sovcloud-identity.de"].TokenExchangeAudience);

            // Clouds without a known token exchange audience should have null
            Assert.IsNull(allEntries["login.windows-ppe.net"].TokenExchangeAudience);
            Assert.IsNull(allEntries["login.microsoftonline.de"].TokenExchangeAudience);
            Assert.IsNull(allEntries["login.sovcloud-identity.sg"].TokenExchangeAudience);
        }
    }
}
