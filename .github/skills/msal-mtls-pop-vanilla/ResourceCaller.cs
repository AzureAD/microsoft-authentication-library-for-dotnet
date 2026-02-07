// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPopVanilla
{
    /// <summary>
    /// Helper class for calling protected resources using mTLS PoP tokens.
    /// Combines the PoP access token with the binding certificate for mTLS handshake.
    /// </summary>
    /// <remarks>
    /// This class handles the complete resource call including:
    /// - Setting the Authorization header with the PoP token
    /// - Attaching the client certificate for mTLS handshake
    /// - Proper disposal of HTTP resources
    /// </remarks>
    public class ResourceCaller : IDisposable
    {
        private readonly string _accessToken;
        private readonly X509Certificate2 _bindingCertificate;
        private readonly HttpClient _httpClient;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceCaller"/> class.
        /// </summary>
        /// <param name="authResult">
        /// The <see cref="AuthenticationResult"/> from MSAL containing the PoP token.
        /// Must have TokenType = "pop" and non-null BindingCertificate.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when authResult is null.</exception>
        /// <exception cref="ArgumentException">Thrown when token is not a PoP token or certificate is missing.</exception>
        public ResourceCaller(AuthenticationResult authResult)
        {
            if (authResult == null)
                throw new ArgumentNullException(nameof(authResult));
            if (string.IsNullOrEmpty(authResult.AccessToken))
                throw new ArgumentException("Access token is null or empty.", nameof(authResult));
            if (authResult.BindingCertificate == null)
                throw new ArgumentException("BindingCertificate is required for mTLS PoP calls.", nameof(authResult));

            _accessToken = authResult.AccessToken;
            _bindingCertificate = authResult.BindingCertificate;

            // Create HttpClient with certificate handler for mTLS
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(_bindingCertificate);
            _httpClient = new HttpClient(handler);

            // Set Authorization header with PoP token
            // Note: The scheme may vary by resource - some use "PoP", others use "Bearer"
            // Consult the target API documentation for the correct authentication scheme
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("PoP", _accessToken);
        }

        /// <summary>
        /// Calls a protected resource with the PoP token and mTLS binding.
        /// </summary>
        /// <param name="resourceUrl">The URL of the protected resource.</param>
        /// <param name="method">The HTTP method (GET, POST, etc.).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP response from the resource.</returns>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
        public async Task<HttpResponseMessage> CallResourceAsync(
            string resourceUrl,
            HttpMethod method,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ResourceCaller));
            if (string.IsNullOrWhiteSpace(resourceUrl))
                throw new ArgumentNullException(nameof(resourceUrl));
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            var request = new HttpRequestMessage(method, resourceUrl);
            
            var response = await _httpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            return response;
        }

        /// <summary>
        /// Calls a protected resource with the PoP token and mTLS binding (POST with content).
        /// </summary>
        /// <param name="resourceUrl">The URL of the protected resource.</param>
        /// <param name="content">The HTTP content to send.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP response from the resource.</returns>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
        public async Task<HttpResponseMessage> PostResourceAsync(
            string resourceUrl,
            HttpContent content,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ResourceCaller));
            if (string.IsNullOrWhiteSpace(resourceUrl))
                throw new ArgumentNullException(nameof(resourceUrl));

            var response = await _httpClient
                .PostAsync(resourceUrl, content, cancellationToken)
                .ConfigureAwait(false);

            return response;
        }

        /// <summary>
        /// Disposes the HTTP client and releases resources.
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
