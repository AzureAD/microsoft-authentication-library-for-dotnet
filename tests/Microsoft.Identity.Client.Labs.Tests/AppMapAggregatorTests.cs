// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.Labs;
using Microsoft.Identity.Client.Labs.Internal;
using Microsoft.Identity.Client.Labs.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Client.Labs.Tests.Unit
{
    [TestClass]
    public class AppMapAggregatorTests
    {
        [TestMethod]
        public void Resolves_App_Keys()
        {
            var map = new Dictionary<(CloudType, Scenario, AppKind), AppSecretKeys>
            {
                { (CloudType.Public, Scenario.Obo, AppKind.ConfidentialClient),
                  new AppSecretKeys("cid", "csec", "pfx", "pfxpwd") }
            };

            var provider = new FakeAppMapProvider(map);
            var agg = new AppMapAggregator(new[] { provider });

            var keys = agg.ResolveKeys(CloudType.Public, Scenario.Obo, AppKind.ConfidentialClient);

            Assert.AreEqual("cid", keys.ClientIdSecret);
            Assert.AreEqual("csec", keys.ClientSecretSecret);
            Assert.AreEqual("pfx", keys.PfxSecret);
            Assert.AreEqual("pfxpwd", keys.PfxPasswordSecret);
        }

        [TestMethod]
        public void Throws_When_App_Key_Missing()
        {
            var provider = new FakeAppMapProvider(new Dictionary<(CloudType, Scenario, AppKind), AppSecretKeys>());
            var agg = new AppMapAggregator(new[] { provider });

            Assert.ThrowsException<KeyNotFoundException>(() =>
                agg.ResolveKeys(CloudType.Public, Scenario.Cca, AppKind.ConfidentialClient));
        }
    }
}
