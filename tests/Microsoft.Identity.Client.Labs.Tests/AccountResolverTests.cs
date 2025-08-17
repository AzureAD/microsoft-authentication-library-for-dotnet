// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.Labs;
using Microsoft.Identity.Client.Labs.Internal;
using Microsoft.Identity.Client.Labs.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Client.Labs.Tests.Unit
{
    [TestClass]
    public class AccountResolverTests
    {
        [TestMethod]
        public async Task Resolves_User_From_Store_And_Policy()
        {
            var acctMap = new FakeAccountMapProvider(new Dictionary<(AuthType, CloudType, Scenario), string>
            {
                { (AuthType.Basic, CloudType.Public, Scenario.Obo), "cld_basic_public_obo_uname" }
            });

            var opts = Options.Create(new LabsOptions { GlobalPasswordSecret = "msidlab1_pwd" });
            var agg = new AccountMapAggregator(new[] { acctMap }, opts);

            var store = new FakeSecretStore(new Dictionary<string, string>
            {
                ["cld_basic_public_obo_uname"] = "ci-user@contoso.onmicrosoft.com",
                ["msidlab1_pwd"] = "P@ssw0rd!"
            });

            var resolver = new AccountResolver(agg, store);

            var (u, p) = await resolver.ResolveUserAsync(AuthType.Basic, CloudType.Public, Scenario.Obo).ConfigureAwait(false);

            Assert.AreEqual("ci-user@contoso.onmicrosoft.com", u);
            Assert.AreEqual("P@ssw0rd!", p);
        }

        [TestMethod]
        public async Task Uses_Convention_For_Username_When_Missing()
        {
            var acctMap = new FakeAccountMapProvider(new Dictionary<(AuthType, CloudType, Scenario), string>());
            var opts = Options.Create(new LabsOptions
            {
                GlobalPasswordSecret = "msidlab1_pwd",
                EnableConventionFallback = true
            });

            var agg = new AccountMapAggregator(new[] { acctMap }, opts);

            var store = new FakeSecretStore(new Dictionary<string, string>
            {
                ["cld_basic_public_basic_uname"] = "ci-basic@contoso.onmicrosoft.com",
                ["msidlab1_pwd"] = "pwd"
            });

            var resolver = new AccountResolver(agg, store);

            var (u, p) = await resolver.ResolveUserAsync(AuthType.Basic, CloudType.Public, Scenario.Basic).ConfigureAwait(false);

            Assert.AreEqual("ci-basic@contoso.onmicrosoft.com", u);
            Assert.AreEqual("pwd", p);
        }
    }
}
