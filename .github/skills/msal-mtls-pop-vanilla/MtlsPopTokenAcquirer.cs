// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPopHelpers
{
    /// <summary>
    /// Production helper for acquiring mTLS PoP tokens in vanilla (direct) flow.
    /// Encapsulates MSAL.NET configuration and token acquisition for SNI scenarios.
    /// </summary>
    /// <remarks>
    /// Based on test code: ClientCredentialsMtlsPopTests.Sni_Gets_Pop_Token_Successfully_TestAsync
    /// </remarks>
    public class MtlsPopTokenAcquirer
    {
        private readonly IConfidentialClientApplication _app;

        /// <summary>
        /// Initializes a new instance of the <see cref="MtlsPopTokenAcquirer"/> class.
        /// </summary>
        /// <param name="clientId">Azure AD application (client) ID.</param>
        /// <param name="tenantId">Azure AD tenant ID or domain name.</param>
        /// <param name="certificate">X.509 certificate with private key for client authentication and PoP binding.</param>
        /// <param name="azureRegion">Optional Azure region for regional endpoints (e.g., "westus3"). If null, uses global endpoint.</param>
        public MtlsPopTokenAcquirer(
            string clientId,
            string tenantId,
            X509Certificate2 certificate,
            string azureRegion = null)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("Client ID cannot be null or empty.", nameof(clientId));
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            var builder = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .WithCertificate(certificate, sendX5C: true); // sendX5C=true enables mTLS PoP

            if (!string.IsNullOrEmpty(azureRegion))
            {
                builder.WithAzureRegion(azureRegion);
            }

            _app = builder.Build();
        }

        /// <summary>
        /// Acquires an mTLS PoP token for the specified scopes.
        /// </summary>
        /// <param name="scopes">Target resource scopes (e.g., "https://vault.azure.net/.default").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An <see cref="AuthenticationResult"/> with TokenType="mtls_pop" and BindingCertificate set.</returns>
        /// <exception cref="MsalServiceException">Thrown for Azure AD service errors (invalid client, unauthorized scopes, etc.).</exception>
        /// <exception cref="MsalClientException">Thrown for client-side errors (certificate issues, network failures, etc.).</exception>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            string[] scopes,
            CancellationToken cancellationToken = default)
        {
            if (scopes == null || scopes.Length == 0)
                throw new ArgumentException("Scopes cannot be null or empty.", nameof(scopes));

            AuthenticationResult result = await _app
                .AcquireTokenForClient(scopes)
                .WithMtlsProofOfPossession() // Request mTLS PoP token
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            // Validate PoP token type
            if (result.TokenType != "mtls_pop")
            {
                throw new InvalidOperationException(
                    $"Expected token type 'mtls_pop', but got '{result.TokenType}'. " +
                    "Ensure sendX5C=true in certificate configuration.");
            }

            return result;
        }

        /// <summary>
        /// Clears the token cache for this application instance.
        /// </summary>
        /// <remarks>
        /// Useful for testing or when you need to force token refresh.
        /// </remarks>
        public async Task ClearCacheAsync()
        {
            var accounts = await _app.GetAccountsAsync().ConfigureAwait(false);
            foreach (var account in accounts)
            {
                await _app.RemoveAsync(account).ConfigureAwait(false);
            }
        }
    }
}
