// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Client.Helpers
{
    /// <summary>
    /// Helper class for calling resources with mTLS PoP tokens.
    /// Handles HttpClient lifecycle and certificate binding.
    /// </summary>
    public class ResourceCaller : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClientHandler _handler;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the ResourceCaller class.
        /// </summary>
        public ResourceCaller()
        {
            _handler = new HttpClientHandler();
            _httpClient = new HttpClient(_handler);
        }

        /// <summary>
        /// Calls a resource endpoint using an mTLS PoP token.
        /// </summary>
        /// <param name="authResult">The authentication result containing the token and binding certificate.</param>
        /// <param name="resourceUrl">The URL of the resource to call.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP response from the resource.</returns>
        /// <exception cref="ArgumentNullException">If authResult or resourceUrl is null.</exception>
        /// <exception cref="InvalidOperationException">If BindingCertificate is missing.</exception>
        public async Task<HttpResponseMessage> CallResourceAsync(
            AuthenticationResult authResult,
            string resourceUrl,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(authResult);
            ArgumentNullException.ThrowIfNull(resourceUrl);

            if (authResult.BindingCertificate == null)
            {
                throw new InvalidOperationException(
                    "BindingCertificate is required for mTLS PoP calls. " +
                    "Ensure you used .WithMtlsProofOfPossession() when acquiring the token.");
            }

            // Add the binding certificate for TLS handshake
            if (!_handler.ClientCertificates.Contains(authResult.BindingCertificate))
            {
                _handler.ClientCertificates.Clear();
                _handler.ClientCertificates.Add(authResult.BindingCertificate);
            }

            // Set authorization header (still uses "Bearer" despite mTLS)
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

            // Call the resource
            var response = await _httpClient.GetAsync(resourceUrl, cancellationToken)
                .ConfigureAwait(false);

            return response;
        }

        /// <summary>
        /// Calls a resource endpoint and returns the response content as a string.
        /// </summary>
        /// <param name="authResult">The authentication result containing the token and binding certificate.</param>
        /// <param name="resourceUrl">The URL of the resource to call.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The response content as a string.</returns>
        /// <exception cref="HttpRequestException">If the HTTP request fails.</exception>
        public async Task<string> CallResourceAndGetContentAsync(
            AuthenticationResult authResult,
            string resourceUrl,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var response = await CallResourceAsync(authResult, resourceUrl, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Disposes the HTTP client and handler.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _httpClient?.Dispose();
            _handler?.Dispose();
            _disposed = true;
        }
    }
}
