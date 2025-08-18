// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client.Labs;
using Microsoft.Identity.Client.Labs.Internal;
using Microsoft.Identity.Client.Labs.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Client.Labs.Tests.Unit
{
    [TestClass]
    public class DiRegistrationTests
    {
        [TestMethod]
        public void Registers_Services_And_Allows_Store_Override()
        {
            var services = new ServiceCollection();

            // Use a neutral, non-sensitive placeholder for the *secret name*
            const string TestPasswordSecretName = "UT_PLACEHOLDER_SECRET_NAME";

            services.AddLabsIdentity(o =>
            {
                o.KeyVaultUri = new Uri("https://example.vault.azure.net/");
                o.GlobalPasswordSecret = TestPasswordSecretName; // secret *name*, not value
            });

            // Minimal maps so resolvers have something to resolve
            services.AddSingleton<IAccountMapProvider>(sp =>
                new FakeAccountMapProvider(new Dictionary<(AuthType, CloudType, Scenario), string>
                {
                    { (AuthType.Basic, CloudType.Public, Scenario.Basic), "uname_secret" }
                }));

            services.AddSingleton<IAppMapProvider>(sp =>
                new FakeAppMapProvider(new Dictionary<(CloudType, Scenario, AppKind), AppSecretKeys>
                {
                    { (CloudType.Public, Scenario.Basic, AppKind.PublicClient),
                      new AppSecretKeys("cid_secret") }
                }));

            // Generate a benign placeholder for secret *values*
            var fakeValue = $"UT_{Guid.NewGuid():N}";

            // Register a fake store whose keys are secret names and values are placeholder values
            services.AddSingleton<ISecretStore>(sp =>
                new FakeSecretStore(new Dictionary<string, string>
                {
                    ["uname_secret"] = "user@example.com",
                    [TestPasswordSecretName] = fakeValue,
                    ["cid_secret"] = "33333333-3333-3333-3333-333333333333"
                }));

            var sp = services.BuildServiceProvider();

            var acct = sp.GetRequiredService<IAccountResolver>();
            var app = sp.GetRequiredService<IAppResolver>();

            Assert.IsNotNull(acct);
            Assert.IsNotNull(app);
        }
    }
}
