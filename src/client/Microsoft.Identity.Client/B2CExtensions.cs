// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Extension methods for Azure AD B2C specific scenarios.
    /// </summary>
    public static class B2CExtensions
    {
        /// <summary>
        /// Get the <see cref="IAccount"/> by its identifier among the accounts available in the token cache,
        /// based on the user flow. This is for Azure AD B2C scenarios.
        /// </summary>
        /// <param name="app">Abstract class containing common API methods and properties. Both <see cref="T:PublicClientApplication"/> and <see cref="T:ConfidentialClientApplication"/>
        /// extend this class. For details see https://aka.ms/msal-net-client-applications. </param>
        /// <param name="userFlow">User flow identifier. The identifier is the user flow being targeted by the specific B2C authority/>.
        /// </param>
        public static async Task<IEnumerable<IAccount>> GetAccountsAsync(this IClientApplicationBase app, string userFlow)
        {
            if (string.IsNullOrWhiteSpace(userFlow))
            {
                throw new ArgumentException($"{nameof(userFlow)} should not be null or whitespace", nameof(userFlow));
            }

            var accounts = await app.GetAccountsAsync().ConfigureAwait(false);

            return accounts.Where(acc => acc.HomeAccountId.ObjectId.Split('.')[0].EndsWith(userFlow, StringComparison.OrdinalIgnoreCase));
        }
    }
}
