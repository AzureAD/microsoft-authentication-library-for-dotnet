// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace MsalMtlsPopFicTwoLeg
{
    /// <summary>
    /// Handles Leg 2 of the FIC two-leg flow: exchanging an assertion for a target token.
    /// Uses the exchange token from Leg 1 as a client assertion with certificate binding.
    /// </summary>
    /// <remarks>
    /// Leg 2 uses assertion-based authentication (WithClientAssertion) to exchange
    /// the Leg 1 token for a token scoped to the target resource.
    /// When TokenBindingCertificate is provided, MSAL uses jwt-pop client_assertion_type.
    /// </remarks>
    public class FicLeg2Exchanger : IDisposable
    {
        private readonly IConfidentialClientApplication _app;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FicLeg2Exchanger"/> class.
        /// </summary>
        /// <param name="clientId">The client (application) ID.</param>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="exchangeToken">
        /// The exchange token from Leg 1 to use as the assertion.
        /// Obtained from Leg 1's AcquireTokenForClient call.
        /// </param>
        /// <param name="bindingCertificate">
        /// The X.509 certificate for token binding.
        /// Must be the same certificate used in Leg 1.
        /// </param>
        /// <param name="region">The Azure region for regional mTLS endpoint (e.g., "eastus", "westus3").</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null or empty.</exception>
        public FicLeg2Exchanger(
            string clientId,
            string tenantId,
            string exchangeToken,
            X509Certificate2 bindingCertificate,
            string region)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentNullException(nameof(clientId));
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentNullException(nameof(tenantId));
            if (string.IsNullOrWhiteSpace(exchangeToken))
                throw new ArgumentNullException(nameof(exchangeToken));
            if (bindingCertificate == null)
                throw new ArgumentNullException(nameof(bindingCertificate));
            if (string.IsNullOrWhiteSpace(region))
                throw new ArgumentNullException(nameof(region));

            _app = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithExperimentalFeatures()  // Required for WithClientAssertion
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .WithAzureRegion(region)
                .WithClientAssertion((AssertionRequestOptions options, CancellationToken ct) =>
                {
                    // Build ClientSignedAssertion with token binding
                    // This enables jwt-pop client_assertion_type
                    return Task.FromResult(new ClientSignedAssertion
                    {
                        Assertion = exchangeToken,
                        TokenBindingCertificate = bindingCertificate
                    });
                })
                .Build();
        }

        /// <summary>
        /// Exchanges the assertion for a token scoped to the target resource.
        /// </summary>
        /// <param name="scopes">
        /// The target scopes (e.g., "https://graph.microsoft.com/.default").
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// The <see cref="AuthenticationResult"/> containing the target PoP token.
        /// Check <see cref="AuthenticationResult.TokenType"/> (should be "pop" if PoP was requested).
        /// </returns>
        /// <exception cref="MsalServiceException">Thrown when token acquisition fails.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
        /// <remarks>
        /// Use .WithMtlsProofOfPossession() if you want a PoP token for the target resource.
        /// Without it, you'll get a Bearer token (but still with jwt-pop assertion).
        /// </remarks>
        public async Task<AuthenticationResult> ExchangeForTargetTokenAsync(
            string[] scopes,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FicLeg2Exchanger));
            if (scopes == null || scopes.Length == 0)
                throw new ArgumentNullException(nameof(scopes));

            var result = await _app
                .AcquireTokenForClient(scopes)
                .WithMtlsProofOfPossession()  // Request PoP token for target resource
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Exchanges the assertion for a Bearer token (not PoP) scoped to the target resource.
        /// </summary>
        /// <param name="scopes">
        /// The target scopes (e.g., "https://storage.azure.com/.default").
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// The <see cref="AuthenticationResult"/> containing the target Bearer token.
        /// The assertion still uses jwt-pop, but the final token is Bearer.
        /// </returns>
        /// <exception cref="MsalServiceException">Thrown when token acquisition fails.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
        public async Task<AuthenticationResult> ExchangeForBearerTokenAsync(
            string[] scopes,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FicLeg2Exchanger));
            if (scopes == null || scopes.Length == 0)
                throw new ArgumentNullException(nameof(scopes));

            // Don't call WithMtlsProofOfPossession() to get Bearer token
            var result = await _app
                .AcquireTokenForClient(scopes)
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
