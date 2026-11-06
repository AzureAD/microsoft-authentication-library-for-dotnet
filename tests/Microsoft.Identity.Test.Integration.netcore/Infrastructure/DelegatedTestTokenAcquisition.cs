// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    internal static class DelegatedTestTokenAcquisition
    {
        internal static async Task<AuthenticationResult> AcquireDelegatedTokenAsync(
            this IPublicClientApplication app, IEnumerable<string> scopes, string username)
        {
            string path = Environment.GetEnvironmentVariable("MSAL_V5_DELEGATED_TOKEN_FIXTURE");
            if (string.IsNullOrEmpty(path))
            {
                Assert.Inconclusive("Provision MSAL_V5_DELEGATED_TOKEN_FIXTURE with delegated refresh tokens obtained through interactive or device-code sign-in. Password and app-only setup are not supported.");
            }

            var entries = JsonSerializer.Deserialize<List<DelegatedTokenFixture>>(File.ReadAllText(path));
            Assert.IsNotNull(entries, "The delegated token fixture must contain an array.");
            var entry = entries.SingleOrDefault(item =>
                string.Equals(item.ClientId, app.AppConfig.ClientId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Username, username, StringComparison.OrdinalIgnoreCase));
            if (entry is null || string.IsNullOrEmpty(entry.RefreshToken))
            {
                Assert.Inconclusive("The delegated token fixture needs a consented refresh token for this exact client and user.");
            }

            var result = await ((IByRefreshToken)app).AcquireTokenByRefreshToken(scopes, entry.RefreshToken)
                .ExecuteAsync().ConfigureAwait(false);
            Assert.IsNotNull(result.Account, "The fixture must return a delegated user account, not an app-only token.");
            Assert.AreEqual(username, result.Account.Username, true, "The fixture must authenticate the requested delegated user.");
            return result;
        }

        internal static AcquireTokenSilentParameterBuilder AcquireBrokerTokenForTest(
            this IPublicClientApplication app, IEnumerable<string> scopes, string username)
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("MSAL_V5_BROKER_LAB_READY"), "1", StringComparison.Ordinal))
            {
                Assert.Inconclusive("Provision the requested broker account through interactive sign-in, then set MSAL_V5_BROKER_LAB_READY=1. Re-provision accounts between tests that remove them.");
            }

            return app.AcquireTokenSilent(scopes, username).WithForceRefresh(true);
        }

        private sealed class DelegatedTokenFixture
        {
            public DelegatedTokenFixture()
            {
            }

            public string ClientId { get; set; }
            public string Username { get; set; }
            public string RefreshToken { get; set; }
        }
    }
}
