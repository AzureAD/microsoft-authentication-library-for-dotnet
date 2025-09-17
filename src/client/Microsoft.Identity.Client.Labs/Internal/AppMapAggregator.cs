// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.Labs.Internal
{
    /// <summary>
    /// Aggregates application maps from multiple providers.
    /// </summary>
    internal sealed class AppMapAggregator
    {
        private readonly Dictionary<(CloudType, Scenario, AppKind), AppSecretKeys> _apps;

        public AppMapAggregator(IEnumerable<IAppMapProvider> providers)
        {
            _apps = new();

            foreach (var p in providers)
            {
                var map = p.GetAppMap();
                if (map is null)
                    continue;

                foreach (var kv in map)
                {
                    // last registered wins
                    _apps[kv.Key] = kv.Value;
                }
            }
        }

        public AppSecretKeys ResolveKeys(CloudType c, Scenario s, AppKind k)
            => _apps.TryGetValue((c, s, k), out var keys)
                ? keys
                : throw new KeyNotFoundException($"No app mapping for ({c},{s},{k}).");
    }
}
