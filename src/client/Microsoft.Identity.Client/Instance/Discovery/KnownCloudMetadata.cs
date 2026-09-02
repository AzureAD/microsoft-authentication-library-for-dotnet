// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    /// <summary>
    /// MSAL's built-in, publicly known cloud metadata, resolved by authority host.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Exposes the cloud-specific values MSAL ships for every publicly known Azure cloud (today: the FIC
    /// token-exchange audience — see <see cref="CloudMetadataKeyNames"/>), keyed by authority host. Access
    /// the built-in data through <see cref="Default"/>.
    /// </para>
    /// <para>
    /// The built-in data is projected from the single internal source of truth (<c>KnownCloudData</c>),
    /// which MSAL's internal instance-discovery pipeline also derives from, so the alias and cloud-specific
    /// magic strings live in exactly one place.
    /// </para>
    /// <para>
    /// This type is shaped as an instance behind <see cref="Default"/> (rather than a static class) so a
    /// future caller-supplied override can be introduced additively, without a second "default" concept.
    /// </para>
    /// </remarks>
    public sealed class KnownCloudMetadata
    {
        private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> s_byHost =
            BuildByHost();

        private KnownCloudMetadata()
        {
        }

        /// <summary>
        /// The built-in known-clouds lookup covering all publicly known Azure cloud environments.
        /// </summary>
        public static KnownCloudMetadata Default { get; } = new KnownCloudMetadata();

        /// <summary>
        /// Gets the cloud-specific metadata for the given authority host.
        /// </summary>
        /// <param name="authorityHost">
        /// The authority host name (e.g., "login.microsoftonline.com", "login.microsoftonline.us"). Lookup
        /// is case-insensitive.
        /// </param>
        /// <returns>
        /// A read-only, case-insensitive dictionary of cloud-specific values (keyed by
        /// <see cref="CloudMetadataKeyNames"/>) for hosts MSAL ships a value for; or <c>null</c> for a
        /// null/empty host, an unknown host, or a known host that has no cloud-specific value to expose. The
        /// returned dictionary is a <see cref="ReadOnlyDictionary{TKey, TValue}"/> and cannot be mutated.
        /// </returns>
        public IReadOnlyDictionary<string, string> GetByAuthorityHost(string authorityHost)
        {
            if (string.IsNullOrEmpty(authorityHost))
            {
                return null;
            }

            s_byHost.TryGetValue(authorityHost, out IReadOnlyDictionary<string, string> values);
            return values;
        }

        private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> BuildByHost()
        {
            var byHost = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            foreach (KnownCloudEntry entry in KnownCloudData.Entries)
            {
                // Only clouds with a shipped cloud-specific value are exposed. A known cloud without a FIC
                // audience yields no entry here, so GetByAuthorityHost returns null for it — the same
                // "no value available" outcome as an unknown host.
                if (string.IsNullOrEmpty(entry.FederatedCredentialAudience))
                {
                    continue;
                }

                var values = new ReadOnlyDictionary<string, string>(
                    new Dictionary<string, string>(1, StringComparer.OrdinalIgnoreCase)
                    {
                        [CloudMetadataKeyNames.FederatedCredentialAudience] = entry.FederatedCredentialAudience,
                    });

                foreach (string alias in entry.Aliases)
                {
                    byHost[alias] = values;
                }
            }

            return byHost;
        }
    }
}
