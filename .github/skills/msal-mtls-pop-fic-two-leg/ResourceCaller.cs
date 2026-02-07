// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPopHelpers
{
    /// <summary>
    /// Helper class for calling protected resources with mTLS PoP tokens.
    /// Handles Authorization header construction and TLS client certificate binding.
    /// </summary>
    /// <remarks>
    /// This class encapsulates the pattern for making HTTP requests to resources
    /// protected by mTLS Proof-of-Possession tokens. It automatically configures
    /// the HttpClient with the binding certificate and proper Authorization header.
    /// </remarks>
    public sealed class ResourceCaller : IDisposable
    {
        private readonly HttpClient _httpClient;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceCaller"/> class.
        /// </summary>
        /// <param name="authenticationResult">The authentication result containing the PoP token and binding certificate.</param>
        /// <exception cref="ArgumentNullException">Thrown when authenticationResult is null.</exception>
        /// <exception cref="ArgumentException">Thrown when BindingCertificate is null in the result.</exception>
        public ResourceCaller(AuthenticationResult authenticationResult)
        {
            ArgumentNullException.ThrowIfNull(authenticationResult);

            if (authenticationResult.BindingCertificate == null)
            {
                throw new ArgumentException(
                    "AuthenticationResult.BindingCertificate must not be null for mTLS PoP scenarios.",
                    nameof(authenticationResult));
            }

            // Configure HttpClientHandler with the binding certificate for mTLS
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(authenticationResult.BindingCertificate);

            _httpClient = new HttpClient(handler);

            // Set Authorization header with token type and access token
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                authenticationResult.TokenType,
                authenticationResult.AccessToken);
        }

        /// <summary>
        /// Calls a protected resource using GET method with the configured mTLS PoP token.
        /// </summary>
        /// <param name="resourceUrl">The URL of the protected resource to call.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The response content as a string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when resourceUrl is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
        public async Task<string> CallResourceAsync(
            string resourceUrl,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(resourceUrl);
            ObjectDisposedException.ThrowIf(_disposed, this);

            HttpResponseMessage response = await _httpClient
                .GetAsync(resourceUrl, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            string content = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            return content;
        }

        /// <summary>
        /// Calls a protected resource using the specified HTTP method with the configured mTLS PoP token.
        /// </summary>
        /// <param name="method">The HTTP method to use.</param>
        /// <param name="resourceUrl">The URL of the protected resource to call.</param>
        /// <param name="content">Optional HTTP content for the request body.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The HTTP response message.</returns>
        /// <exception cref="ArgumentNullException">Thrown when method or resourceUrl is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        public async Task<HttpResponseMessage> CallResourceAsync(
            HttpMethod method,
            string resourceUrl,
            HttpContent content = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(method);
            ArgumentNullException.ThrowIfNull(resourceUrl);
            ObjectDisposedException.ThrowIf(_disposed, this);

            var request = new HttpRequestMessage(method, resourceUrl)
            {
                Content = content
            };

            HttpResponseMessage response = await _httpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            return response;
        }

        /// <summary>
        /// Disposes the resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}
