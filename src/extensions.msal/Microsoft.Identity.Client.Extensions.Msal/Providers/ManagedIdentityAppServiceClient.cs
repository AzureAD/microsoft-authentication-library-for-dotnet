// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client.Extensions.Msal.Providers.Exceptions;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    internal sealed class ManagedIdentityAppServiceClient : ManagedIdentityClient
    {
        private readonly string _secret;

        public ManagedIdentityAppServiceClient(string endpoint, string secret, HttpClient client, int maxRetries = 10, ILogger logger = null) : base(endpoint, client, maxRetries, logger)
        {
            _secret = secret;
        }

        protected override HttpRequestMessage BuildTokenRequest(string resourceUri)
        {
            var requestUri = $"{Endpoint}?resource={resourceUri}&api-version={Constants.ManagedIdentityAppServiceApiVersion}";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Add("Secret", _secret);
            Log(Microsoft.Extensions.Logging.LogLevel.Information, $"sending token request: {requestUri}");
            return request;
        }

        protected override DateTimeOffset ParseExpiresOn(TokenResponse tokenResponse)
        {
            if (DateTimeOffset.TryParse(tokenResponse.ExpiresOn, out var dateTimeOffset))
            {
                return dateTimeOffset;
            }

            Log(Microsoft.Extensions.Logging.LogLevel.Error, $"failed to parse: {tokenResponse.ExpiresOn}");
            throw new FailedParseOfManagedIdentityExpirationException();
        }

        private void Log(Microsoft.Extensions.Logging.LogLevel level, string message, [CallerMemberName] string memberName = default)
        {
            Logger?.Log(level, $"{nameof(ManagedIdentityAppServiceClient)}.{memberName} :: {message}");
        }
    }
}
