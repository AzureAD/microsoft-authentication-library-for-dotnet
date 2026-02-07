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
    /// Handles Leg 1 of the FIC two-leg token exchange: acquiring an mTLS PoP token
    /// for api://AzureADTokenExchange using either Managed Identity or Confidential Client.
    /// </summary>
    public sealed class FicLeg1Acquirer : IDisposable
    {
        private const string TokenExchangeResource = "api://AzureADTokenExchange";
        private const string TokenExchangeScope = "api://AzureADTokenExchange/.default";

        private readonly object _app;
        private readonly bool _isManagedIdentity;
        private bool _disposed;

        private FicLeg1Acquirer(object app, bool isManagedIdentity)
        {
            _app = app;
            _isManagedIdentity = isManagedIdentity;
        }

        /// <summary>
        /// Creates a Leg 1 acquirer for Managed Identity scenarios.
        /// </summary>
        /// <param name="managedIdentityId">The managed identity configuration (system or user-assigned).</param>
        /// <returns>A new FicLeg1Acquirer instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when managedIdentityId is null.</exception>
        public static FicLeg1Acquirer CreateForManagedIdentity(ManagedIdentityId managedIdentityId)
        {
            ArgumentNullException.ThrowIfNull(managedIdentityId);

            IManagedIdentityApplication app = ManagedIdentityApplicationBuilder
                .Create(managedIdentityId)
                .Build();

            return new FicLeg1Acquirer(app, isManagedIdentity: true);
        }

        /// <summary>
        /// Creates a Leg 1 acquirer for Confidential Client scenarios with certificate-based SNI.
        /// </summary>
        /// <param name="clientId">The application (client) ID.</param>
        /// <param name="authority">The authority URL (e.g., https://login.microsoftonline.com/tenant-id).</param>
        /// <param name="certificate">The X509 certificate for client authentication.</param>
        /// <param name="region">The Azure region for SNI (e.g., "westus3").</param>
        /// <returns>A new FicLeg1Acquirer instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
        public static FicLeg1Acquirer CreateForConfidentialClient(
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

            return new FicLeg1Acquirer(app, isManagedIdentity: false);
        }

        /// <summary>
        /// Acquires Leg 1 mTLS PoP token for api://AzureADTokenExchange.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The authentication result containing the PoP token and binding certificate.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        /// <exception cref="MsalServiceException">Thrown when token acquisition fails.</exception>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_isManagedIdentity)
            {
                var app = (IManagedIdentityApplication)_app;
                return await app
                    .AcquireTokenForManagedIdentity(TokenExchangeResource)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                var app = (IConfidentialClientApplication)_app;
                return await app
                    .AcquireTokenForClient(new[] { TokenExchangeScope })
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets whether this acquirer uses Managed Identity or Confidential Client.
        /// </summary>
        public bool IsManagedIdentity => _isManagedIdentity;

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
