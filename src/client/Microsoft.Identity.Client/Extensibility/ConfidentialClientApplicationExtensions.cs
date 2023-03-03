// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Extensibility methods for <see cref="IConfidentialClientApplication"/>
    /// </summary>
    public static class ConfidentialClientApplicationExtensions
    {
        /// <summary>
        /// Stops an in progress long running OBO session by removing the tokens associated with the provided cache key.
        /// See https://aka.ms/msal-net-on-behalf-of.
        /// </summary>
        /// <param name="clientApp">Client app to remove tokens from</param>
        /// <param name="longRunningProcessSessionKey">OBO cache key used to remove the tokens</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="longRunningProcessSessionKey"/> is not set.</exception>
        public static async Task StopLongRunningWebApiAsync(this ILongRunningWebApi clientApp, string longRunningProcessSessionKey, CancellationToken cancellationToken = default)
        {
            await (clientApp as ConfidentialClientApplication).StopLongRunningWebApiAsync(longRunningProcessSessionKey, cancellationToken).ConfigureAwait(false);
        }
    }
}
