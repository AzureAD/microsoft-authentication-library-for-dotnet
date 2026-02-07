// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MtlsPopFicTwoLeg
{
    /// <summary>
    /// Helper class for Leg 2 of the FIC two-leg flow: exchanging assertion tokens for access tokens.
    /// Always uses Confidential Client with mTLS PoP.
    /// </summary>
    public sealed class FicLeg2Exchanger : IDisposable
    {
        private readonly IConfidentialClientApplication _app;
        private bool _disposed;

        private FicLeg2Exchanger(IConfidentialClientApplication app)
        {
            _app = app;
        }

        /// <summary>
        /// Creates a Leg 2 exchanger using Confidential Client with certificate authentication.
        /// This is the only supported identity type for Leg 2 (Managed Identity cannot perform Leg 2).
        /// </summary>
        /// <param name="clientId">The application (client) ID</param>
        /// <param name="tenantId">The Azure AD tenant ID</param>
        /// <param name="certificate">The certificate for authentication (must include private key)</param>
        /// <param name="region">Optional: Azure region for regional endpoints (e.g., "westus3")</param>
        /// <returns>A configured Leg 2 exchanger</returns>
        public static FicLeg2Exchanger Create(
            string clientId,
            string tenantId,
            X509Certificate2 certificate,
            string? region = null)
        {
            ArgumentNullException.ThrowIfNull(clientId);
            ArgumentNullException.ThrowIfNull(tenantId);
            ArgumentNullException.ThrowIfNull(certificate);

            var builder = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                .WithCertificate(certificate)
                .WithMtlsProofOfPossession();  // Always enable mTLS PoP for Leg 2

            if (!string.IsNullOrEmpty(region))
            {
                builder.WithAzureRegion(region);
            }

            var app = builder.Build();
            return new FicLeg2Exchanger(app);
        }

        /// <summary>
        /// Exchanges an assertion token (from Leg 1) for an access token with mTLS PoP (Leg 2).
        /// </summary>
        /// <param name="assertionToken">The assertion token acquired in Leg 1</param>
        /// <param name="resource">The target resource URI (e.g., "https://graph.microsoft.com")</param>
        /// <param name="usePoP">Whether to use Proof of Possession (typically true for Leg 2)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The authentication result containing the access token and binding certificate</returns>
        public async Task<AuthenticationResult> ExchangeAssertionForAccessTokenAsync(
            string assertionToken,
            string resource,
            bool usePoP = true,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(assertionToken);
            ArgumentNullException.ThrowIfNull(resource);

            string[] scopes = { $"{resource}/.default" };

            // Use the assertion token as client assertion for the token exchange
            var builder = _app
                .AcquireTokenForClient(scopes)
                .WithClientAssertion(assertionToken);

            // Apply PoP to the access token (Leg 2)
            if (usePoP)
            {
                builder.WithProofOfPossession();
            }

            return await builder
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Exchanges an assertion token for an access token with custom claims.
        /// Useful for requesting specific claims or capabilities in the access token.
        /// </summary>
        /// <param name="assertionToken">The assertion token acquired in Leg 1</param>
        /// <param name="resource">The target resource URI</param>
        /// <param name="claims">JSON string of additional claims to request</param>
        /// <param name="usePoP">Whether to use Proof of Possession (typically true for Leg 2)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The authentication result containing the access token and binding certificate</returns>
        public async Task<AuthenticationResult> ExchangeAssertionForAccessTokenWithClaimsAsync(
            string assertionToken,
            string resource,
            string claims,
            bool usePoP = true,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(assertionToken);
            ArgumentNullException.ThrowIfNull(resource);
            ArgumentNullException.ThrowIfNull(claims);

            string[] scopes = { $"{resource}/.default" };

            var builder = _app
                .AcquireTokenForClient(scopes)
                .WithClientAssertion(assertionToken)
                .WithClaims(claims);

            if (usePoP)
            {
                builder.WithProofOfPossession();
            }

            return await builder
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Disposes the underlying MSAL application.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }
    }
}
