// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    internal static class DelegatedTestTokenAcquisition
    {
        internal static async Task<AuthenticationResult> AcquireDelegatedTokenAsync(
            this IPublicClientApplication app, IEnumerable<string> scopes, string username)
        {
            if (!DelegatedTokenFixtureProvider.TryGetRefreshToken(app.AppConfig.ClientId, username, out string refreshToken))
            {
                Assert.Inconclusive("Provision MSAL_V5_DELEGATED_TOKEN_FIXTURE with a consented delegated refresh token for this exact client and user obtained through interactive or device-code sign-in. Password and app-only setup are not supported.");
            }

            var result = await ((IByRefreshToken)app).AcquireTokenByRefreshToken(scopes, refreshToken)
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
    }
}
