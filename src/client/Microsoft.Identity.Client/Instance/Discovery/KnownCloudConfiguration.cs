// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    /// <summary>
    /// Default <see cref="ICloudConfiguration"/> implementation that provides cloud-specific metadata
    /// for all publicly known Azure cloud environments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The built-in cloud data is projected from the single internal source of truth
    /// (<see cref="KnownCloudData"/>), which the internal <c>KnownMetadataProvider</c> also derives from,
    /// so the alias/preferred-host magic strings live in exactly one place.
    /// </para>
    /// <para>
    /// Callers extend this with additional or internal-only clouds by composing a custom provider over
    /// <see cref="Default"/> — see <see cref="InMemoryCloudConfiguration"/> — and resolving values from
    /// that composed instance.
    /// </para>
    /// </remarks>
    public sealed class KnownCloudConfiguration : ICloudConfiguration
    {
        /// <summary>
        /// Singleton instance of the default cloud configuration.
        /// </summary>
        public static KnownCloudConfiguration Default { get; } = new KnownCloudConfiguration();

        private static readonly Dictionary<string, CloudSettings> s_cloudSettingsByAlias =
            new Dictionary<string, CloudSettings>(StringComparer.OrdinalIgnoreCase);

        static KnownCloudConfiguration()
        {
            foreach (KnownCloudEntry entry in KnownCloudData.Entries)
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // Preferred network/cache hosts are not projected into the public bag; the
                // instance-discovery pipeline reads them from KnownCloudData via KnownMetadataProvider.
                // They remain on KnownCloudEntry for internal use.
                if (!string.IsNullOrEmpty(entry.TokenExchangeAudience))
                {
                    values[MsalCloudKeys.TokenExchangeAudience] = entry.TokenExchangeAudience;
                }

                var settings = new CloudSettings(values);

                foreach (string alias in entry.Aliases)
                {
                    s_cloudSettingsByAlias[alias] = settings;
                }
            }
        }

        /// <inheritdoc/>
        public CloudSettings GetSettingsByAuthorityHost(string authorityHost)
        {
            if (string.IsNullOrEmpty(authorityHost))
            {
                return null;
            }

            s_cloudSettingsByAlias.TryGetValue(authorityHost, out CloudSettings settings);
            return settings;
        }
    }
}
