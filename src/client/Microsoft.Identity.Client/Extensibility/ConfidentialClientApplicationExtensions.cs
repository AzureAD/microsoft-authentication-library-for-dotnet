// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Extensibility methods for <see cref="IConfidentialClientApplication"/>.
    /// </summary>
    public static class ConfidentialClientApplicationExtensions
    {
        /// <summary>
        /// Stops an in-progress long-running on-behalf-of session by removing the tokens associated with the provided cache key.
        /// See <see href="https://aka.ms/msal-net-long-running-obo">Long-running OBO in MSAL.NET</see>.
        /// </summary>
        /// <param name="clientApp">Client application to remove tokens from.</param>
        /// <param name="longRunningProcessSessionKey">OBO cache key used to remove the tokens.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if tokens are removed from the cache; false, otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="longRunningProcessSessionKey"/> is not set.</exception>
        public static async Task<bool> StopLongRunningProcessInWebApiAsync(this ILongRunningWebApi clientApp, string longRunningProcessSessionKey, CancellationToken cancellationToken = default)
        {
            return await ((ConfidentialClientApplication) clientApp).StopLongRunningProcessInWebApiAsync(longRunningProcessSessionKey, cancellationToken).ConfigureAwait(false);
        }
    }
}
