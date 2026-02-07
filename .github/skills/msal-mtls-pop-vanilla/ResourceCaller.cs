// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPop.Vanilla
{
    /// <summary>
    /// Helper class for calling protected resources using mTLS PoP tokens.
    /// Handles HttpClient configuration with mTLS binding certificate.
    /// </summary>
    /// <remarks>
    /// Production-ready implementation following MSAL.NET conventions:
    /// - ConfigureAwait(false) on all awaits
    /// - CancellationToken support with defaults
    /// - Proper IDisposable implementation
    /// - Input validation and disposal checks
    /// </remarks>
    public sealed class ResourceCaller : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessToken;
        private readonly string _tokenType;
        private bool _disposed;

        /// <summary>
        /// Creates a ResourceCaller from an mTLS PoP authentication result.
        /// </summary>
        /// <param name="authResult">Authentication result containing mTLS PoP token and binding certificate.</param>
        /// <exception cref="ArgumentNullException">Thrown when authResult is null.</exception>
        /// <exception cref="ArgumentException">Thrown when token type is not mtls_pop or BindingCertificate is null.</exception>
        public ResourceCaller(AuthenticationResult authResult)
        {
            ArgumentNullException.ThrowIfNull(authResult);

            if (string.IsNullOrEmpty(authResult.TokenType) || 
                !authResult.TokenType.Equals("mtls_pop", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Expected token type 'mtls_pop', but got '{authResult.TokenType}'. " +
                    "Ensure .WithMtlsProofOfPossession() was called during token acquisition.",
                    nameof(authResult));
            }

            if (authResult.BindingCertificate == null)
            {
                throw new ArgumentException(
                    "BindingCertificate is null. This certificate is required for mTLS calls. " +
                    "Ensure .WithMtlsProofOfPossession() was called before ExecuteAsync().",
                    nameof(authResult));
            }

            _accessToken = authResult.AccessToken;
            _tokenType = authResult.TokenType;

            // Configure HttpClientHandler with mTLS binding certificate
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(authResult.BindingCertificate);

            _httpClient = new HttpClient(handler);
            
            // Set PoP authorization header
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("PoP", _accessToken);
        }

        /// <summary>
        /// Calls a protected resource endpoint with mTLS PoP authentication.
        /// </summary>
        /// <param name="resourceUri">The URI of the protected resource to call.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Response body as a string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when resourceUri is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the caller has been disposed.</exception>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
        public async Task<string> CallResourceAsync(
            string resourceUri, 
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(resourceUri);
            ObjectDisposedException.ThrowIf(_disposed, this);

            var response = await _httpClient
                .GetAsync(resourceUri, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            return content;
        }

        /// <summary>
        /// Calls a protected resource endpoint with mTLS PoP authentication and returns the full response.
        /// </summary>
        /// <param name="resourceUri">The URI of the protected resource to call.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The complete HTTP response message.</returns>
        /// <exception cref="ArgumentNullException">Thrown when resourceUri is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the caller has been disposed.</exception>
        public async Task<HttpResponseMessage> CallResourceFullResponseAsync(
            string resourceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(resourceUri);
            ObjectDisposedException.ThrowIf(_disposed, this);

            var response = await _httpClient
                .GetAsync(resourceUri, cancellationToken)
                .ConfigureAwait(false);

            return response;
        }

        /// <summary>
        /// Disposes the ResourceCaller and its underlying HttpClient.
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
