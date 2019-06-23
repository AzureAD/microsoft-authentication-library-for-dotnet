// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    public class InstanceProviderTests : TestBase
    {
        [TestMethod]
        public void StaticProviderPreservesStateAcrossInstances()
        {
            // Arrange
            StaticMetadataProvider staticMetadataProvider1 = new StaticMetadataProvider();
            StaticMetadataProvider staticMetadataProvider2 = new StaticMetadataProvider();
            staticMetadataProvider1.AddMetadata("env", new InstanceDiscoveryMetadataEntry());

            // Act
            var result = staticMetadataProvider2.GetMetadata("env");
            staticMetadataProvider2.Clear();
            var result2 = staticMetadataProvider2.GetMetadata("env");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result2);
        }

        [TestMethod]
        public void KnownMetadataProvider_RespondsIfEnvironmentsAreKnown()
        {
            // Arrange
            KnownMetadataProvider knownMetadataProvider = new KnownMetadataProvider();

            var result = knownMetadataProvider.GetMetadata(
                 "login.microsoftonline.com", null);
            Assert.IsNotNull(result);

            result = knownMetadataProvider.GetMetadata(
                "login.microsoftonline.com", Enumerable.Empty<string>());
            Assert.IsNotNull(result);

            result = knownMetadataProvider.GetMetadata(
                "login.microsoftonline.com", new[] { "login.microsoftonline.com" });
            Assert.IsNotNull(result);

            result = knownMetadataProvider.GetMetadata(
                "login.microsoftonline.com", new[] { "login.microsoftonline.com" });
            Assert.IsNotNull(result);

            result = knownMetadataProvider.GetMetadata(
                "login.microsoftonline.com", new[] { "login.windows.net", "login.microsoft.com", "login.partner.microsoftonline.cn" });
                Assert.IsNotNull(result);

            result = knownMetadataProvider.GetMetadata(
                "login.partner.microsoftonline.cn", new[] { "login.windows.net", "login.microsoft.com", "login.partner.microsoftonline.cn" });
            Assert.IsNotNull(result);

            result = knownMetadataProvider.GetMetadata(
               "login.microsoftonline.com", new[] { "login.windows.net", "bogus", "login.partner.microsoftonline.cn" });
            Assert.IsNull(result);

            result = knownMetadataProvider.GetMetadata(
                "bogus", new[] { "login.windows.net", "login.microsoft.com", "login.partner.microsoftonline.cn" });
            Assert.IsNull(result);
        }
    }
}
