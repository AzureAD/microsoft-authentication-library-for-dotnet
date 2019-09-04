// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client.Extensions.Msal.Providers.Exceptions;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    internal abstract class ManagedIdentityClient
    {
        private readonly int _maxRetries;
        private readonly HttpClient _client;
        protected readonly ILogger Logger;

        internal ManagedIdentityClient(string endpoint, HttpClient client, int maxRetries = 5, ILogger logger = null)
        {
            Endpoint = endpoint;
            _client = client;
            _maxRetries = maxRetries;
            Logger = logger;
        }

        protected abstract HttpRequestMessage BuildTokenRequest(string resourceUri);

        protected abstract DateTimeOffset ParseExpiresOn(TokenResponse tokenResponse);


        public async Task<IToken> FetchTokenWithRetryAsync(string resourceUri, CancellationToken cancel)
        {
            var strategy = new RetryWithExponentialBackoff(_maxRetries, 50, 60000);
            HttpResponseMessage res = null;
            await strategy.RunAsync(async () =>
            {
                Log(Microsoft.Extensions.Logging.LogLevel.Information, $"fetching resource uri {resourceUri}");
                var req = BuildTokenRequest(resourceUri);
                res = await _client.SendAsync(req, HttpCompletionOption.ResponseContentRead, cancel).ConfigureAwait(false);

                var intCode = (int)res.StatusCode;
                Log(Microsoft.Extensions.Logging.LogLevel.Information, $"received status code {intCode}");
                switch (intCode) {
                case 404:
                case 429:
                case var _ when intCode >= 500:
                    throw new TransientManagedIdentityException($"encountered transient managed identity service error with status code {intCode}");
                case 400:
                    throw new BadRequestManagedIdentityException();
                }
            }).ConfigureAwait(false);

            var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            if(string.IsNullOrEmpty(json))
            {
                Log(Microsoft.Extensions.Logging.LogLevel.Information, "received empty body");
                return null;
            }

            var tokenRes = TokenResponse.Parse(json);
            return new AccessTokenWithExpiration { ExpiresOn = ParseExpiresOn(tokenRes), AccessToken = tokenRes.AccessToken };
        }

        protected string Endpoint { get; }

        private void Log(Microsoft.Extensions.Logging.LogLevel level, string message, [CallerMemberName] string memberName = default)
        {
            Logger?.Log(level, $"{nameof(ManagedIdentityClient)}.{memberName} :: {message}");
        }
    }
}
