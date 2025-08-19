// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.Labs;
using Microsoft.Identity.Client.Labs.Internal;
using Microsoft.Identity.Client.Labs.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Client.Labs.Tests.Unit
{
    [TestClass]
    public class AccountMapAggregatorTests
    {
        // Renamed to avoid hiding Microsoft.Extensions.Options.Options
        private static IOptions<LabsOptions> CreateOpts(bool enableConvention = true) =>
            Microsoft.Extensions.Options.Options.Create(
                new LabsOptions { EnableConventionFallback = enableConvention });

        [TestMethod]
        public void Username_From_Provider_Is_Used()
        {
            var provider = new FakeAccountMapProvider(new Dictionary<(AuthType, CloudType, Scenario), string>
            {
                { (AuthType.Basic, CloudType.Public, Scenario.Basic), "cld_basic_public_basic_uname" }
            });

            var agg = new AccountMapAggregator(new[] { provider }, CreateOpts());
            var name = agg.GetUsernameSecret(AuthType.Basic, CloudType.Public, Scenario.Basic);

            Assert.AreEqual("cld_basic_public_basic_uname", name);
        }

        [TestMethod]
        public void Username_Convention_When_Missing_And_Enabled()
        {
            var agg = new AccountMapAggregator(Array.Empty<IAccountMapProvider>(), CreateOpts(enableConvention: true));
            var name = agg.GetUsernameSecret(AuthType.Federated, CloudType.Public, Scenario.Obo);

            Assert.AreEqual("cld_federated_public_obo_uname", name);
        }

        [TestMethod]
        public void Username_Throws_When_Missing_And_Convention_Disabled()
        {
            var agg = new AccountMapAggregator(Array.Empty<IAccountMapProvider>(), CreateOpts(enableConvention: false));
            Assert.ThrowsException<KeyNotFoundException>(() =>
                agg.GetUsernameSecret(AuthType.Basic, CloudType.Public, Scenario.Obo));
        }
    }
}
