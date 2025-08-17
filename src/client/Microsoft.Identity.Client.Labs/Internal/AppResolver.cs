// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Labs.Internal
{
    /// <summary>
    /// Default implementation of <see cref="IAppResolver"/> that uses map providers and Key Vault.
    /// </summary>
    internal sealed class AppResolver : IAppResolver
    {
        private readonly AppMapAggregator _agg;
        private readonly ISecretStore _store;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppResolver"/> class.
        /// </summary>
        public AppResolver(AppMapAggregator agg, ISecretStore store)
        {
            _agg = agg;
            _store = store;
        }

        /// <inheritdoc />
        public async Task<AppCredentials> ResolveAppAsync(CloudType cloud, Scenario scenario, AppKind kind, CancellationToken ct = default)
        {
            var keys = _agg.ResolveKeys(cloud, scenario, kind);

            var clientId = await _store.GetAsync(keys.ClientIdSecret, ct).ConfigureAwait(false);

            string clientSecret = string.Empty;
            byte[] pfxBytes = Array.Empty<byte>();
            string pfxPwd = string.Empty;

            if (!string.IsNullOrEmpty(keys.ClientSecretSecret))
            {
                clientSecret = await _store.GetAsync(keys.ClientSecretSecret, ct).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(keys.PfxSecret))
            {
                var b64 = await _store.GetAsync(keys.PfxSecret, ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(b64))
                {
                    try
                    {
                        pfxBytes = Convert.FromBase64String(b64);
                    }
                    catch (FormatException ex)
                    {
                        throw new InvalidOperationException($"Secret '{keys.PfxSecret}' is not valid Base64.", ex);
                    }
                }
            }

            if (!string.IsNullOrEmpty(keys.PfxPasswordSecret))
            {
                pfxPwd = await _store.GetAsync(keys.PfxPasswordSecret, ct).ConfigureAwait(false);
            }

            return new AppCredentials(clientId, clientSecret, pfxBytes, pfxPwd);
        }
    }
}
