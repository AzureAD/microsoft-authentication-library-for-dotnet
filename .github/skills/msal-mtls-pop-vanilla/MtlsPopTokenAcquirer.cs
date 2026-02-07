// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPopHelpers
{
    /// <summary>
    /// Wrapper for acquiring mTLS PoP tokens using either Managed Identity or Confidential Client.
    /// Provides a unified interface for token acquisition across different authentication methods.
    /// </summary>
    public sealed class MtlsPopTokenAcquirer : IDisposable
    {
        private readonly object _app;
        private readonly bool _isManagedIdentity;
        private bool _disposed;

        private MtlsPopTokenAcquirer(object app, bool isManagedIdentity)
        {
            _app = app;
            _isManagedIdentity = isManagedIdentity;
        }

        /// <summary>
        /// Creates an acquirer for Managed Identity scenarios.
        /// </summary>
        /// <param name="managedIdentityId">The managed identity configuration (system or user-assigned).</param>
        /// <returns>A new MtlsPopTokenAcquirer instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when managedIdentityId is null.</exception>
        public static MtlsPopTokenAcquirer CreateForManagedIdentity(ManagedIdentityId managedIdentityId)
        {
            ArgumentNullException.ThrowIfNull(managedIdentityId);

            IManagedIdentityApplication app = ManagedIdentityApplicationBuilder
                .Create(managedIdentityId)
                .Build();

            return new MtlsPopTokenAcquirer(app, isManagedIdentity: true);
        }

        /// <summary>
        /// Creates an acquirer for Confidential Client scenarios with certificate-based SNI.
        /// </summary>
        /// <param name="clientId">The application (client) ID.</param>
        /// <param name="authority">The authority URL (e.g., https://login.microsoftonline.com/tenant-id).</param>
        /// <param name="certificate">The X509 certificate for client authentication.</param>
        /// <param name="region">The Azure region for SNI (e.g., "westus3").</param>
        /// <returns>A new MtlsPopTokenAcquirer instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
        public static MtlsPopTokenAcquirer CreateForConfidentialClient(
            string clientId,
            string authority,
            X509Certificate2 certificate,
            string region)
        {
            ArgumentNullException.ThrowIfNull(clientId);
            ArgumentNullException.ThrowIfNull(authority);
            ArgumentNullException.ThrowIfNull(certificate);
            ArgumentNullException.ThrowIfNull(region);

            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(authority)
                .WithAzureRegion(region)
                .WithCertificate(certificate, sendX5C: true)
                .Build();

            return new MtlsPopTokenAcquirer(app, isManagedIdentity: false);
        }

        /// <summary>
        /// Acquires an mTLS PoP token for the specified resource (Managed Identity) or scopes (Confidential Client).
        /// </summary>
        /// <param name="resourceOrScopes">
        /// For Managed Identity: A single resource string (e.g., "https://graph.microsoft.com").
        /// For Confidential Client: An array of scopes (e.g., ["https://vault.azure.net/.default"]).
        /// </param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The authentication result containing the PoP token and binding certificate.</returns>
        /// <exception cref="ArgumentNullException">Thrown when resourceOrScopes is null.</exception>
        /// <exception cref="ArgumentException">Thrown when resourceOrScopes format doesn't match the authentication method.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            object resourceOrScopes,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(resourceOrScopes);
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_isManagedIdentity)
            {
                if (resourceOrScopes is not string resource)
                {
                    throw new ArgumentException(
                        "For Managed Identity, resourceOrScopes must be a string representing the resource.",
                        nameof(resourceOrScopes));
                }

                var app = (IManagedIdentityApplication)_app;
                return await app
                    .AcquireTokenForManagedIdentity(resource)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                if (resourceOrScopes is not IEnumerable<string> scopes)
                {
                    throw new ArgumentException(
                        "For Confidential Client, resourceOrScopes must be an IEnumerable<string> of scopes.",
                        nameof(resourceOrScopes));
                }

                var app = (IConfidentialClientApplication)_app;
                return await app
                    .AcquireTokenForClient(scopes)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Disposes the resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // MSAL application instances don't require explicit disposal
                _disposed = true;
            }
        }
    }
}
