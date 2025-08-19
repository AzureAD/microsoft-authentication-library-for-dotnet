// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.Labs;
using Microsoft.Identity.Client.Labs.Internal;
using Microsoft.Identity.Client.Labs.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Client.Labs.Tests.Unit
{
    [TestClass]
    public class PasswordSelectionPolicyTests
    {
        private static AccountMapAggregator MakeAgg(LabsOptions options)
        {
            var provider = new FakeAccountMapProvider(new Dictionary<(AuthType, CloudType, Scenario), string>
            {
                { (AuthType.Basic, CloudType.Public, Scenario.Obo), "cld_basic_public_obo_uname" }
            });

            return new AccountMapAggregator(new[] { provider }, Options.Create(options));
        }

        [TestMethod]
        public void Tuple_Override_Wins_Over_Cloud_And_Global()
        {
            var options = new LabsOptions
            {
                GlobalPasswordSecret = "global_pwd",
                PasswordSecretByCloud = new() { [CloudType.Public] = "cloud_pwd" },
                PasswordSecretByTuple = new() { ["basic.public.obo"] = "tuple_pwd" }
            };

            var agg = MakeAgg(options);
            var pwd = agg.GetPasswordSecret(AuthType.Basic, CloudType.Public, Scenario.Obo);

            Assert.AreEqual("tuple_pwd", pwd);
        }

        [TestMethod]
        public void Cloud_Override_Wins_Over_Global()
        {
            var options = new LabsOptions
            {
                GlobalPasswordSecret = "global_pwd",
                PasswordSecretByCloud = new() { [CloudType.Public] = "cloud_pwd" }
            };

            var agg = MakeAgg(options);
            var pwd = agg.GetPasswordSecret(AuthType.Basic, CloudType.Public, Scenario.Obo);

            Assert.AreEqual("cloud_pwd", pwd);
        }

        [TestMethod]
        public void Global_Is_Used_When_No_Overrides()
        {
            var options = new LabsOptions { GlobalPasswordSecret = "global_pwd" };

            var agg = MakeAgg(options);
            var pwd = agg.GetPasswordSecret(AuthType.Basic, CloudType.Public, Scenario.Obo);

            Assert.AreEqual("global_pwd", pwd);
        }

        [TestMethod]
        public void Convention_Used_When_No_Config_And_Enabled()
        {
            var options = new LabsOptions { EnableConventionFallback = true };

            var agg = MakeAgg(options);
            var pwd = agg.GetPasswordSecret(AuthType.Basic, CloudType.Public, Scenario.Obo);

            Assert.AreEqual("cld_basic_public_obo_pwd", pwd);
        }

        [TestMethod]
        public void Throws_When_No_Config_And_Convention_Disabled()
        {
            var options = new LabsOptions { EnableConventionFallback = false };
            var agg = MakeAgg(options);

            Assert.ThrowsException<KeyNotFoundException>(() =>
                agg.GetPasswordSecret(AuthType.Basic, CloudType.Public, Scenario.Obo));
        }
    }
}
