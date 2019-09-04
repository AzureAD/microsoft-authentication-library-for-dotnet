// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    /// <summary>
    ///     InternalManagedIdentityCredentialProvider will fetch AAD JWT tokens from the IMDS endpoint for the default client id or
    ///     a specified client id.
    /// </summary>
    internal class InternalManagedIdentityCredentialProvider
    {
        private static readonly HttpClient DefaultClient = new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(100) // 100 milliseconds -- make sure there is an extremely short timeout to ensure we fail fast
        };

        private readonly ManagedIdentityClient _client;
        private readonly ILogger _logger;

        internal InternalManagedIdentityCredentialProvider(string endpoint, HttpClient httpClient = null,
            string secret = null, string clientId = null, int maxRetries = 5, ILogger logger = null)
        {
            _logger = logger;
            Log(Microsoft.Extensions.Logging.LogLevel.Information, $"built with clientId: {clientId}, maxRetries: {maxRetries}, secret was present {!string.IsNullOrWhiteSpace(secret)}");
            if (string.IsNullOrWhiteSpace(secret))
            {
                _client = new ManagedIdentityVmClient(endpoint, httpClient ?? DefaultClient, clientId: clientId, maxRetries: maxRetries, logger: _logger);
            }
            else
            {
                _client = new ManagedIdentityAppServiceClient(endpoint, secret, httpClient ?? DefaultClient, maxRetries: maxRetries, logger: _logger);
            }
        }

        /// <summary>
        ///     GetTokenAsync returns a token for a given set of scopes
        /// </summary>
        /// <param name="resourceUri">Resource URI of the protected API requested to access</param>
        /// <param name="cancel">Cancellation token for the HTTP requests</param>
        /// <returns>A token with expiration</returns>
        public async Task<IToken> GetTokenAsync(string resourceUri, CancellationToken cancel)
        {
            return await _client.FetchTokenWithRetryAsync(resourceUri, cancel).ConfigureAwait(false);
        }

        private void Log(Microsoft.Extensions.Logging.LogLevel level, string message, [CallerMemberName] string memberName = default)
        {
            _logger?.Log(level, $"{nameof(InternalManagedIdentityCredentialProvider)}.{memberName} :: {message}");
        }
    }
}
