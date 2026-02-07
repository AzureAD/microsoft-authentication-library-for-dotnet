// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MsalMtlsPopHelpers
{
    /// <summary>
    /// Production helper for calling protected resources with mTLS PoP tokens.
    /// Handles mTLS transport configuration and Authorization header formatting.
    /// </summary>
    /// <remarks>
    /// Based on patterns from ClientCredentialsMtlsPopTests and RFC 8705.
    /// </remarks>
    public class ResourceCaller : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly X509Certificate2 _certificate;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceCaller"/> class.
        /// </summary>
        /// <param name="certificate">X.509 certificate for mTLS transport (same cert used for PoP binding).</param>
        public ResourceCaller(X509Certificate2 certificate)
        {
            _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));

            // Configure HttpClient with mTLS
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(certificate);

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// Calls a protected resource with an mTLS PoP token.
        /// </summary>
        /// <param name="resourceUrl">Full URL of the protected resource (e.g., "https://keyvault.vault.azure.net/secrets/my-secret?api-version=7.4").</param>
        /// <param name="popToken">mTLS PoP access token from MSAL (result.AccessToken where result.TokenType == "mtls_pop").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Response body as a string.</returns>
        /// <exception cref="HttpRequestException">Thrown for HTTP errors (401, 403, 500, etc.).</exception>
        public async Task<string> CallResourceAsync(
            string resourceUrl,
            string popToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(resourceUrl))
                throw new ArgumentException("Resource URL cannot be null or empty.", nameof(resourceUrl));
            if (string.IsNullOrEmpty(popToken))
                throw new ArgumentException("PoP token cannot be null or empty.", nameof(popToken));

            using var request = new HttpRequestMessage(HttpMethod.Get, resourceUrl);
            
            // RFC 8705: Authorization header uses lowercase "pop"
            request.Headers.Add("Authorization", $"pop {popToken}");

            HttpResponseMessage response = await _httpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new HttpRequestException(
                    $"Resource call failed with status {response.StatusCode}. " +
                    $"Response: {errorBody}");
            }

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Calls a protected resource with a POST request and mTLS PoP token.
        /// </summary>
        /// <param name="resourceUrl">Full URL of the protected resource.</param>
        /// <param name="popToken">mTLS PoP access token from MSAL.</param>
        /// <param name="requestBody">HTTP request body content (JSON, XML, etc.).</param>
        /// <param name="contentType">Content-Type header value (e.g., "application/json").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Response body as a string.</returns>
        /// <exception cref="HttpRequestException">Thrown for HTTP errors.</exception>
        public async Task<string> PostResourceAsync(
            string resourceUrl,
            string popToken,
            string requestBody,
            string contentType = "application/json",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(resourceUrl))
                throw new ArgumentException("Resource URL cannot be null or empty.", nameof(resourceUrl));
            if (string.IsNullOrEmpty(popToken))
                throw new ArgumentException("PoP token cannot be null or empty.", nameof(popToken));

            using var request = new HttpRequestMessage(HttpMethod.Post, resourceUrl);
            request.Headers.Add("Authorization", $"pop {popToken}");
            request.Content = new StringContent(requestBody, System.Text.Encoding.UTF8, contentType);

            HttpResponseMessage response = await _httpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new HttpRequestException(
                    $"Resource POST failed with status {response.StatusCode}. " +
                    $"Response: {errorBody}");
            }

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Disposes the underlying HttpClient.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
