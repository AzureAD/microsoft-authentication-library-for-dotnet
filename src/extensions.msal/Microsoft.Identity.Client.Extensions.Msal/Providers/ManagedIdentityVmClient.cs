// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client.Extensions.Msal.Providers.Exceptions;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    internal sealed class ManagedIdentityVmClient : ManagedIdentityClient
    {
        private readonly string _clientId;

        public ManagedIdentityVmClient(string endpoint, HttpClient client, string clientId = null, int maxRetries = 10, ILogger logger = null)
            : base(endpoint, client, maxRetries, logger)
        {
            _clientId = clientId;
        }

        protected override HttpRequestMessage BuildTokenRequest(string resourceUri)
        {
            var clientIdParameter = string.IsNullOrWhiteSpace(_clientId)
                ? string.Empty :
                $"&client_id={_clientId}";

            var requestUri = $"{Endpoint}?resource={resourceUri}{clientIdParameter}&api-version={Constants.ManagedIdentityVMApiVersion}";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Add("Metadata", "true");
            Log(Microsoft.Extensions.Logging.LogLevel.Information, $"sending token request: {requestUri}");
            return request;
        }

        protected override DateTimeOffset ParseExpiresOn(TokenResponse tokenResponse)
        {
            var startOfUnixTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            if (double.TryParse(tokenResponse.ExpiresOn, out var seconds))
            {
                return startOfUnixTime.AddSeconds(seconds);
            }

            Log(Microsoft.Extensions.Logging.LogLevel.Error, $"failed to parse: {tokenResponse.ExpiresOn}");
            throw new FailedParseOfManagedIdentityExpirationException();
        }

        private void Log(Microsoft.Extensions.Logging.LogLevel level, string message, [CallerMemberName] string memberName = default)
        {
            Logger?.Log(level, $"{nameof(ManagedIdentityVmClient)}.{memberName} :: {message}");
        }
    }
}
