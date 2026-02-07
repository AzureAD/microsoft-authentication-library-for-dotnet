// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace MtlsPopFicTwoLeg
{
    /// <summary>
    /// Helper class for Leg 1 of the FIC two-leg flow: acquiring assertion tokens.
    /// Supports both Managed Identity and Confidential Client as identity sources.
    /// </summary>
    public sealed class FicLeg1Acquirer : IDisposable
    {
        private readonly object _appBuilder;
        private readonly bool _isManagedIdentity;
        private bool _disposed;

        private FicLeg1Acquirer(object appBuilder, bool isManagedIdentity)
        {
            _appBuilder = appBuilder;
            _isManagedIdentity = isManagedIdentity;
        }

        /// <summary>
        /// Creates a Leg 1 acquirer for Managed Identity (System-Assigned or User-Assigned).
        /// Note: Managed Identity in Leg 1 typically acquires tokens WITHOUT PoP (vanilla bearer tokens).
        /// </summary>
        /// <param name="managedIdentityId">The managed identity to use (SystemAssigned, or UAMI by ClientId/ResourceId/ObjectId)</param>
        /// <returns>A configured Leg 1 acquirer</returns>
        public static FicLeg1Acquirer CreateForManagedIdentity(ManagedIdentityId managedIdentityId)
        {
            ArgumentNullException.ThrowIfNull(managedIdentityId);

            // Note: For Leg 1, we typically do NOT use WithMtlsProofOfPossession()
            // The assertion is usually a bearer token, and PoP is applied in Leg 2
            var app = ManagedIdentityApplicationBuilder
                .Create(managedIdentityId)
                .Build();

            return new FicLeg1Acquirer(app, isManagedIdentity: true);
        }

        /// <summary>
        /// Creates a Leg 1 acquirer for Confidential Client with certificate authentication.
        /// Note: Confidential Client in Leg 1 can optionally use PoP, but typically uses bearer tokens.
        /// </summary>
        /// <param name="clientId">The application (client) ID</param>
        /// <param name="tenantId">The Azure AD tenant ID</param>
        /// <param name="certificate">The certificate for authentication (must include private key)</param>
        /// <param name="region">Optional: Azure region for regional endpoints (e.g., "westus3")</param>
        /// <param name="enableMtlsPoP">Optional: Enable mTLS PoP for Leg 1 (rare, usually false)</param>
        /// <returns>A configured Leg 1 acquirer</returns>
        public static FicLeg1Acquirer CreateForConfidentialClient(
            string clientId,
            string tenantId,
            X509Certificate2 certificate,
            string? region = null,
            bool enableMtlsPoP = false)
        {
            ArgumentNullException.ThrowIfNull(clientId);
            ArgumentNullException.ThrowIfNull(tenantId);
            ArgumentNullException.ThrowIfNull(certificate);

            var builder = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                .WithCertificate(certificate);

            // Optionally enable mTLS PoP for Leg 1 (uncommon scenario)
            if (enableMtlsPoP)
            {
                builder.WithMtlsProofOfPossession();
            }

            if (!string.IsNullOrEmpty(region))
            {
                builder.WithAzureRegion(region);
            }

            var app = builder.Build();
            return new FicLeg1Acquirer(app, isManagedIdentity: false);
        }

        /// <summary>
        /// Acquires an assertion token (Leg 1).
        /// This token will be used in Leg 2 to exchange for an access token.
        /// </summary>
        /// <param name="resource">The resource URI or App ID URI (e.g., "api://your-app-id")</param>
        /// <param name="usePoP">Whether to use Proof of Possession (typically false for Leg 1)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The authentication result containing the assertion token</returns>
        public async Task<AuthenticationResult> AcquireAssertionAsync(
            string resource,
            bool usePoP = false,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(resource);

            if (_isManagedIdentity)
            {
                var app = (IManagedIdentityApplication)_appBuilder;
                var builder = app.AcquireTokenForManagedIdentity(resource);

                // Managed Identity in Leg 1: typically no PoP
                if (usePoP)
                {
                    builder.WithProofOfPossession();
                }

                return await builder
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                var app = (IConfidentialClientApplication)_appBuilder;
                string[] scopes = { $"{resource}/.default" };
                var builder = app.AcquireTokenForClient(scopes);

                // Confidential Client in Leg 1: optionally PoP
                if (usePoP)
                {
                    builder.WithProofOfPossession();
                }

                return await builder
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
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
