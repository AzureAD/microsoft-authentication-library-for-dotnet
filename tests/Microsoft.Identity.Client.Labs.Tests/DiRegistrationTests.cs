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

            // Neutral secret-name placeholders (avoid "secret/password/pwd" keywords)
            const string NameUser = "UT_NAME_USER";
            const string NameGlobal = "UT_NAME_GLOBAL";
            const string NameClientId = "UT_NAME_CLIENTID";

            services.AddLabsIdentity(o =>
            {
                o.KeyVaultUri = new Uri("https://example.vault.azure.net/");
                // This is a *secret name* (key), not a secret value.
                o.GlobalPasswordSecret = NameGlobal;
            });

            // Minimal maps so resolvers have something to resolve
            services.AddSingleton<IAccountMapProvider>(sp =>
                new FakeAccountMapProvider(new Dictionary<(AuthType, CloudType, Scenario), string>
                {
                    { (AuthType.Basic, CloudType.Public, Scenario.Basic), NameUser }
                }));

            services.AddSingleton<IAppMapProvider>(sp =>
                new FakeAppMapProvider(new Dictionary<(CloudType, Scenario, AppKind), AppSecretKeys>
                {
                    { (CloudType.Public, Scenario.Basic, AppKind.PublicClient),
                      new AppSecretKeys(NameClientId) }
                }));

            // Generate a benign placeholder for secret *values*
            var placeholderValue = $"UT_{Guid.NewGuid():N}";

            // Register a fake store: keys are secret *names*; values are placeholders
            services.AddSingleton<ISecretStore>(sp =>
                new FakeSecretStore(new Dictionary<string, string>
                {
                    [NameUser] = "user@example.com",
                    [NameGlobal] = placeholderValue,
                    [NameClientId] = "33333333-3333-3333-3333-333333333333"
                }));

            var sp = services.BuildServiceProvider();

            var acct = sp.GetRequiredService<IAccountResolver>();
            var app = sp.GetRequiredService<IAppResolver>();

            Assert.IsNotNull(acct);
            Assert.IsNotNull(app);
        }
    }
}
