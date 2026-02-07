// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MtlsPopVanilla
{
    /// <summary>
    /// Helper class for calling protected resources using tokens acquired with mTLS PoP.
    /// Automatically configures HttpClient with the proper authentication header.
    /// </summary>
    public sealed class ResourceCaller : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationResult _authResult;
        private bool _disposed;

        /// <summary>
        /// Creates a resource caller using the specified authentication result.
        /// </summary>
        /// <param name="authResult">The authentication result containing the access token</param>
        public ResourceCaller(AuthenticationResult authResult)
        {
            ArgumentNullException.ThrowIfNull(authResult);
            _authResult = authResult;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Calls a protected resource endpoint.
        /// </summary>
        /// <param name="resourceUrl">The full URL of the resource endpoint</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The response body as a string</returns>
        public async Task<string> CallResourceAsync(
            string resourceUrl,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(resourceUrl);

            var request = new HttpRequestMessage(HttpMethod.Get, resourceUrl);
            
            // Set authorization header based on token type
            if (_authResult.TokenType == "pop")
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("PoP", _authResult.AccessToken);
            }
            else
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authResult.AccessToken);
            }

            var response = await _httpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Calls a protected resource endpoint with a custom HTTP method and optional request body.
        /// </summary>
        /// <param name="httpMethod">The HTTP method (GET, POST, PUT, DELETE, etc.)</param>
        /// <param name="resourceUrl">The full URL of the resource endpoint</param>
        /// <param name="requestBody">Optional request body content</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The response body as a string</returns>
        public async Task<string> CallResourceAsync(
            HttpMethod httpMethod,
            string resourceUrl,
            HttpContent? requestBody = null,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(httpMethod);
            ArgumentNullException.ThrowIfNull(resourceUrl);

            var request = new HttpRequestMessage(httpMethod, resourceUrl);
            
            // Set authorization header based on token type
            if (_authResult.TokenType == "pop")
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("PoP", _authResult.AccessToken);
            }
            else
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authResult.AccessToken);
            }

            if (requestBody != null)
            {
                request.Content = requestBody;
            }

            var response = await _httpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Disposes the underlying HttpClient.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}
