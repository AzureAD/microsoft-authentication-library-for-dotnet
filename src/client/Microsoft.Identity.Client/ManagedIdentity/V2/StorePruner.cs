// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Removes expired entries (NotAfter less than  now) for an alias from an open X509 store.
    /// </summary>
    internal static class StorePruner
    {
        /// <summary>
        /// Deletes only certificates that are actually expired (NotAfter less than nowUtc),
        /// scoped to the given alias (cache key) via FriendlyName.
        /// This ensures we only delete certificates that we created for that alias,
        /// i.e. MSAL specific mTLS certs.
        /// </summary>
        internal static void PruneExpiredForAlias(
            X509Store store,
            string aliasCacheKey,
            DateTime nowUtc,
            ILoggerAdapter logger)
        {
            X509Certificate2[] items;
            try
            {
                items = new X509Certificate2[store.Certificates.Count];
                // Safe snapshot for .NET Framework when removing
                store.Certificates.CopyTo(items, 0);
            }
            catch
            {
                // Fallback for providers/runtimes where CopyTo fails
                items = store.Certificates.Cast<X509Certificate2>().ToArray();
            }

            int removed = 0;

            foreach (var existing in items)
            {
                try
                {
                    if (!FriendlyNameCodec.TryDecode(existing.FriendlyName, out var alias, out _))
                        continue;
                    if (!StringComparer.Ordinal.Equals(alias, aliasCacheKey))
                        continue;

                    if (existing.NotAfter.ToUniversalTime() <= nowUtc)
                    {
                        { store.Remove(existing); removed++; }
                    }
                }
                finally
                {
                    existing.Dispose();
                }
            }

            logger?.Verbose(() => "[PersistentCert] PruneExpired completed for alias '" + aliasCacheKey + "'. Removed=" + removed + ".");
        }
    }
}
