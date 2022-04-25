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
        private readonly ICoreLogger _logger = new NullLogger();

        [TestMethod]
        public void StaticProviderPreservesStateAcrossInstances()
        {
            // Arrange
            NetworkCacheMetadataProvider staticMetadataProvider1 = new NetworkCacheMetadataProvider();
            NetworkCacheMetadataProvider staticMetadataProvider2 = new NetworkCacheMetadataProvider();
            staticMetadataProvider1.AddMetadata("env", new InstanceDiscoveryMetadataEntry());

            // Act
            InstanceDiscoveryMetadataEntry result = staticMetadataProvider2.GetMetadata("env", _logger);
            staticMetadataProvider2.Clear();
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
            ex = Assert.ThrowsException<MsalClientException>(() => userMetadataProvider.GetMetadataOrThrow("non_existent", _logger));
            Assert.AreEqual(MsalError.InvalidUserInstanceMetadata, ex.ErrorCode);
            ex = Assert.ThrowsException<MsalClientException>(() => userMetadataProvider.GetMetadataOrThrow(null, _logger));
            Assert.AreEqual(MsalError.InvalidUserInstanceMetadata, ex.ErrorCode);
            ex = Assert.ThrowsException<MsalClientException>(() => userMetadataProvider.GetMetadataOrThrow("", _logger));
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
        }
    }
}
