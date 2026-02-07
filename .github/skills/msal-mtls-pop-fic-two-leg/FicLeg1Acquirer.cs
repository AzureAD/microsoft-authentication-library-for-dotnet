// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPopFicTwoLeg
{
    /// <summary>
    /// Handles Leg 1 of the FIC two-leg flow: acquiring an exchange token.
    /// This token will be used as an assertion in Leg 2.
    /// </summary>
    /// <remarks>
    /// Leg 1 uses standard certificate-based authentication to acquire a token
    /// scoped to "api://AzureADTokenExchange/.default". This token is then
    /// exchanged in Leg 2 for a token to the target resource.
    /// </remarks>
    public class FicLeg1Acquirer : IDisposable
    {
        private const string TokenExchangeScope = "api://AzureADTokenExchange/.default";
        private readonly IConfidentialClientApplication _app;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FicLeg1Acquirer"/> class.
        /// </summary>
        /// <param name="clientId">The client (application) ID.</param>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="certificate">The X.509 certificate for authentication and PoP binding.</param>
        /// <param name="region">The Azure region for regional mTLS endpoint (e.g., "eastus", "westus3").</param>
        /// <param name="sendX5C">Whether to send the X5C certificate chain (required for SNI scenarios).</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null or empty.</exception>
        public FicLeg1Acquirer(
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
        /// Acquires an exchange token for use in Leg 2.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// The access token from the <see cref="AuthenticationResult"/>.
        /// This token should be used as the assertion in Leg 2.
        /// </returns>
        /// <exception cref="MsalServiceException">Thrown when token acquisition fails.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
        public async Task<string> GetExchangeTokenAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FicLeg1Acquirer));

            var result = await _app
                .AcquireTokenForClient(new[] { TokenExchangeScope })
                .WithMtlsProofOfPossession()
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.AccessToken;
        }

        /// <summary>
        /// Acquires an exchange token and returns the full AuthenticationResult.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// The complete <see cref="AuthenticationResult"/> including token, certificate, and metadata.
        /// </returns>
        /// <exception cref="MsalServiceException">Thrown when token acquisition fails.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
        public async Task<AuthenticationResult> GetExchangeTokenWithDetailsAsync(
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FicLeg1Acquirer));

            var result = await _app
                .AcquireTokenForClient(new[] { TokenExchangeScope })
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
