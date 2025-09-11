// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Microsoft.Identity.Client.Labs.Internal
{
    /// <summary>
    /// Secret store implementation backed by Azure Key Vault.
    /// </summary>
    internal sealed class KeyVaultSecretStore : ISecretStore
    {
        private readonly SecretClient _kv;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultSecretStore"/> class.
        /// </summary>
        /// <param name="keyVaultUri">The Key Vault URI.</param>
        public KeyVaultSecretStore(Uri keyVaultUri)
        {
            if (keyVaultUri is null)
                throw new ArgumentNullException(nameof(keyVaultUri));
            _kv = new SecretClient(keyVaultUri, new DefaultAzureCredential());
        }

        /// <inheritdoc />
        public async Task<string> GetAsync(string secretName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
                throw new ArgumentException("Secret name cannot be null or empty.", nameof(secretName));

            // IMPORTANT: specify the cancellation token by name to avoid the 'version' parameter
            var response = await _kv
                .GetSecretAsync(secretName, version: null, cancellationToken: ct)
                .ConfigureAwait(false);

            return response.Value.Value;
        }
    }
}
