// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPopVanilla
{
    /// <summary>
    /// Simplified token acquirer for MSAL.NET mTLS Proof-of-Possession (vanilla flow).
    /// This class wraps MSAL.NET's ConfidentialClientApplication to provide a simpler
    /// API for direct PoP token acquisition without token exchange.
    /// </summary>
    /// <remarks>
    /// Use this class for direct service-to-service authentication with PoP tokens.
    /// For token exchange scenarios (FIC two-leg), use FicLeg1Acquirer + FicLeg2Exchanger instead.
    /// </remarks>
    public class MtlsPopTokenAcquirer : IDisposable
    {
        private readonly IConfidentialClientApplication _app;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MtlsPopTokenAcquirer"/> class.
        /// </summary>
        /// <param name="clientId">The client (application) ID.</param>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="certificate">The X.509 certificate for authentication and PoP binding.</param>
        /// <param name="region">The Azure region for regional mTLS endpoint (e.g., "eastus", "westus3").</param>
        /// <param name="sendX5C">Whether to send the X5C certificate chain (required for SNI scenarios).</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null or empty.</exception>
        public MtlsPopTokenAcquirer(
            string clientId,
            string tenantId,
            X509Certificate2 certificate,
            string region,
            bool sendX5C = true)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentNullException(nameof(clientId));
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));
            if (string.IsNullOrWhiteSpace(region))
                throw new ArgumentNullException(nameof(region));

            _app = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .WithCertificate(certificate, sendX5C)
                .WithAzureRegion(region)
                .Build();
        }

        /// <summary>
        /// Acquires an mTLS PoP token for the specified scopes.
        /// </summary>
        /// <param name="scopes">The scopes to request (e.g., "https://graph.microsoft.com/.default").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// The <see cref="AuthenticationResult"/> containing the PoP access token and binding certificate.
        /// Check <see cref="AuthenticationResult.TokenType"/> (should be "pop") and 
        /// <see cref="AuthenticationResult.BindingCertificate"/> (the bound certificate).
        /// </returns>
        /// <exception cref="MsalServiceException">Thrown when token acquisition fails.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
        public async Task<AuthenticationResult> GetTokenAsync(
            string[] scopes,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MtlsPopTokenAcquirer));
            if (scopes == null || scopes.Length == 0)
                throw new ArgumentNullException(nameof(scopes));

            var result = await _app
                .AcquireTokenForClient(scopes)
                .WithMtlsProofOfPossession()
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Disposes the underlying MSAL application instance.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // Note: ConfidentialClientApplication doesn't implement IDisposable in current MSAL versions,
                // but this pattern is included for future compatibility and best practices.
                _disposed = true;
            }
        }
    }
}
