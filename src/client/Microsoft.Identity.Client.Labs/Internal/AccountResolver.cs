// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Labs.Internal
{
    /// <summary>
    /// Default implementation of <see cref="IAccountResolver"/> that uses map providers and Key Vault.
    /// </summary>
    internal sealed class AccountResolver : IAccountResolver
    {
        private readonly AccountMapAggregator _agg;
        private readonly ISecretStore _store;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountResolver"/> class.
        /// </summary>
        public AccountResolver(AccountMapAggregator agg, ISecretStore store)
        {
            _agg = agg;
            _store = store;
        }

        /// <inheritdoc />
        public async Task<(string Username, string Password)> ResolveUserAsync(
            AuthType auth, CloudType cloud, Scenario scenario, CancellationToken ct = default)
        {
            var unameSecret = _agg.GetUsernameSecret(auth, cloud, scenario);
            var pwdSecret = _agg.GetPasswordSecret(auth, cloud, scenario);

            var username = await _store.GetAsync(unameSecret, ct).ConfigureAwait(false);
            var password = await _store.GetAsync(pwdSecret, ct).ConfigureAwait(false);

            return (username, password);
        }
    }
}
