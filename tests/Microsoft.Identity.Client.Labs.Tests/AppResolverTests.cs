// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Labs;
using Microsoft.Identity.Client.Labs.Internal;
using Microsoft.Identity.Client.Labs.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Client.Labs.Tests.Unit
{
    [TestClass]
    public class AppResolverTests
    {
        [TestMethod]
        public async Task Resolves_App_With_Secret()
        {
            var appMap = new FakeAppMapProvider(new Dictionary<(CloudType, Scenario, AppKind), AppSecretKeys>
            {
                { (CloudType.Public, Scenario.Obo, AppKind.ConfidentialClient),
                  new AppSecretKeys("cid", "csec") }
            });

            // Generate a non-sensitive test value
            var expectedClientSecret = $"UT_{Guid.NewGuid():N}";

            var store = new FakeSecretStore(new Dictionary<string, string>
            {
                ["cid"] = "11111111-1111-1111-1111-111111111111",
                ["csec"] = expectedClientSecret
            });

            var agg = new AppMapAggregator(new[] { appMap });
            var resolver = new AppResolver(agg, store);

            var app = await resolver.ResolveAppAsync(CloudType.Public, Scenario.Obo, AppKind.ConfidentialClient)
                .ConfigureAwait(false);

            Assert.AreEqual("11111111-1111-1111-1111-111111111111", app.ClientId);
            Assert.AreEqual(expectedClientSecret, app.ClientSecret);
            CollectionAssert.AreEqual(Array.Empty<byte>(), app.PfxBytes);
            Assert.AreEqual(string.Empty, app.PfxPassword);
        }

        [TestMethod]
        public async Task Resolves_App_With_Pfx()
        {
            var pfxBytes = new byte[] { 1, 2, 3, 4, 5 };
            var b64 = Convert.ToBase64String(pfxBytes);

            var appMap = new FakeAppMapProvider(new Dictionary<(CloudType, Scenario, AppKind), AppSecretKeys>
            {
                { (CloudType.Public, Scenario.Obo, AppKind.WebApi),
                  new AppSecretKeys("api_cid", "", "api_pfx", "api_pfxpwd") }
            });

            var expectedPfxPassword = $"UTPFX_{Guid.NewGuid():N}";

            var store = new FakeSecretStore(new Dictionary<string, string>
            {
                ["api_cid"] = "22222222-2222-2222-2222-222222222222",
                ["api_pfx"] = b64,
                ["api_pfxpwd"] = expectedPfxPassword
            });

            var agg = new AppMapAggregator(new[] { appMap });
            var resolver = new AppResolver(agg, store);

            var app = await resolver.ResolveAppAsync(CloudType.Public, Scenario.Obo, AppKind.WebApi)
                .ConfigureAwait(false);

            Assert.AreEqual("22222222-2222-2222-2222-222222222222", app.ClientId);
            CollectionAssert.AreEqual(pfxBytes, app.PfxBytes);
            Assert.AreEqual(expectedPfxPassword, app.PfxPassword);
        }

        [TestMethod]
        public async Task Throws_On_Invalid_Base64_Pfx()
        {
            var appMap = new FakeAppMapProvider(new Dictionary<(CloudType, Scenario, AppKind), AppSecretKeys>
            {
                { (CloudType.Public, Scenario.Obo, AppKind.WebApi),
                  new AppSecretKeys("api_cid", "", "api_pfx", "api_pfxpwd") }
            });

            var store = new FakeSecretStore(new Dictionary<string, string>
            {
                ["api_cid"] = "cid",
                ["api_pfx"] = "NOT-BASE64",
                ["api_pfxpwd"] = "UT_IGNORE_THIS_VALUE" // benign literal
            });

            var agg = new AppMapAggregator(new[] { appMap });
            var resolver = new AppResolver(agg, store);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await resolver.ResolveAppAsync(CloudType.Public, Scenario.Obo, AppKind.WebApi).ConfigureAwait(false))
                .ConfigureAwait(false);
        }
    }
}
