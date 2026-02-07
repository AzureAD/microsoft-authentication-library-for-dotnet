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
    /// Helper class for calling resources with tokens from FIC two-leg flows.
    /// Handles both Bearer and mTLS PoP tokens.
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
        /// Calls a resource endpoint using a token from Leg 2.
        /// Automatically handles Bearer vs. mTLS PoP tokens.
        /// </summary>
        /// <param name="authResult">The authentication result from Leg 2.</param>
        /// <param name="resourceUrl">The URL of the resource to call.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP response from the resource.</returns>
        /// <exception cref="ArgumentNullException">If authResult or resourceUrl is null.</exception>
        /// <exception cref="InvalidOperationException">If mTLS PoP token is missing BindingCertificate.</exception>
        public async Task<HttpResponseMessage> CallResourceAsync(
            AuthenticationResult authResult,
            string resourceUrl,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(authResult);
            ArgumentNullException.ThrowIfNull(resourceUrl);

            // For mTLS PoP tokens, add the binding certificate
            if (authResult.TokenType == Constants.MtlsPoPTokenType)
            {
                if (authResult.BindingCertificate == null)
                {
                    throw new InvalidOperationException(
                        "BindingCertificate is required for mTLS PoP calls. " +
                        "In FIC two-leg flows, this should be set to Leg 1's certificate.");
                }

                if (!_handler.ClientCertificates.Contains(authResult.BindingCertificate))
                {
                    _handler.ClientCertificates.Clear();
                    _handler.ClientCertificates.Add(authResult.BindingCertificate);
                }
            }

            // Set authorization header (always "Bearer" even for mTLS PoP)
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
        /// <param name="authResult">The authentication result from Leg 2.</param>
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
