// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensions.Msal.Providers.Exceptions;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    /// <inheritdoc />
    /// <summary>
    /// TokenProviderChain creates an ordered list of probes that will determine if the environment will allow it to build
    /// a ICredentialProvider. The ICredentialProvider is then used to generate authentication credentials for the
    /// consumer.
    /// </summary>
    public class TokenProviderChain : ITokenProvider
    {
        private readonly IList<ITokenProvider> _providers;

        /// <summary>
        /// Create an instance of a TokenProviderChain providing a list of IProbes which will be executed in order to create a ICredentialProvider
        /// </summary>
        /// <param name="providers">providers to be executed in order to create a ICredentialProvider</param>
        /// <exception cref="ArgumentException">throws if no probes were provided or if no probe is able to build a ICredentialProvider</exception>
        public TokenProviderChain(IList<ITokenProvider> providers)
        {
            if (providers == null || !providers.Any())
            {
                throw new ArgumentException("must provide 1 or more IProbes");
            }

            _providers = providers;
        }

        /// <inheritdoc />
        public async Task<bool> IsAvailableAsync(CancellationToken cancel = default)
        {
            foreach (var p in _providers)
            {
                if (await p.IsAvailableAsync(cancel).ConfigureAwait(false))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public async Task<IToken> GetTokenAsync(IEnumerable<string> scopes, CancellationToken cancel = default)
        {
            ITokenProvider provider = null;
            foreach (var p in _providers)
            {
                if (!await p.IsAvailableAsync(cancel).ConfigureAwait(false))
                {
                    continue;
                }

                provider = p;
                break;
            }

            if (provider == null)
            {
                throw new NoProvidersAvailableException();
            }

            return await provider.GetTokenAsync(scopes, cancel).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IToken> GetTokenWithResourceUriAsync(string resourceUri, CancellationToken cancel = default)
        {
            ITokenProvider provider = null;
            foreach (var p in _providers)
            {
                if (!await p.IsAvailableAsync(cancel).ConfigureAwait(false))
                {
                    continue;
                }

                provider = p;
                break;
            }

            if (provider == null)
            {
                throw new NoProvidersAvailableException();
            }

            return await provider.GetTokenWithResourceUriAsync(resourceUri, cancel)
                .ConfigureAwait(false);
        }
    }
}
