// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace MtlsPopVanilla
{
    /// <summary>
    /// Helper class for acquiring tokens with mTLS Proof of Possession (vanilla flow).
    /// Supports both Managed Identity and Confidential Client scenarios.
    /// </summary>
    public sealed class MtlsPopTokenAcquirer : IDisposable
    {
        private readonly object _appBuilder;
        private readonly bool _isManagedIdentity;
        private bool _disposed;

        private MtlsPopTokenAcquirer(object appBuilder, bool isManagedIdentity)
        {
            _appBuilder = appBuilder;
            _isManagedIdentity = isManagedIdentity;
        }

        /// <summary>
        /// Creates a token acquirer for Managed Identity (System-Assigned or User-Assigned).
        /// </summary>
        /// <param name="managedIdentityId">The managed identity to use (SystemAssigned, or UAMI by ClientId/ResourceId/ObjectId)</param>
        /// <returns>A configured token acquirer</returns>
        public static MtlsPopTokenAcquirer CreateForManagedIdentity(ManagedIdentityId managedIdentityId)
        {
            ArgumentNullException.ThrowIfNull(managedIdentityId);

            var app = ManagedIdentityApplicationBuilder
                .Create(managedIdentityId)
                .WithMtlsProofOfPossession()
                .Build();

            return new MtlsPopTokenAcquirer(app, isManagedIdentity: true);
        }

        /// <summary>
        /// Creates a token acquirer for Confidential Client with certificate authentication.
        /// </summary>
        /// <param name="clientId">The application (client) ID</param>
        /// <param name="tenantId">The Azure AD tenant ID</param>
        /// <param name="certificate">The certificate for authentication (must include private key)</param>
        /// <param name="region">Optional: Azure region for regional endpoints (e.g., "westus3")</param>
        /// <returns>A configured token acquirer</returns>
        public static MtlsPopTokenAcquirer CreateForConfidentialClient(
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
                .WithMtlsProofOfPossession();

            if (!string.IsNullOrEmpty(region))
            {
                builder.WithAzureRegion(region);
            }

            var app = builder.Build();
            return new MtlsPopTokenAcquirer(app, isManagedIdentity: false);
        }

        /// <summary>
        /// Acquires a token for the specified resource.
        /// </summary>
        /// <param name="resource">The resource URI (e.g., "https://graph.microsoft.com")</param>
        /// <param name="usePoP">Whether to use Proof of Possession (true) or bearer tokens (false)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The authentication result containing the token and binding certificate</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            string resource,
            bool usePoP = true,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(resource);

            if (_isManagedIdentity)
            {
                var app = (IManagedIdentityApplication)_appBuilder;
                var builder = app.AcquireTokenForManagedIdentity(resource);

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
