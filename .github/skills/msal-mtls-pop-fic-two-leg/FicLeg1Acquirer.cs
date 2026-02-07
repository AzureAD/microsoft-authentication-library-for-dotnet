// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Client.Helpers
{
    /// <summary>
    /// Helper class for Leg 1 token acquisition in FIC two-leg flows.
    /// Supports both Managed Identity and Confidential Client authentication.
    /// Always targets api://AzureADTokenExchange with mTLS PoP.
    /// </summary>
    public class FicLeg1Acquirer : IDisposable
    {
        private const string TokenExchangeScopeMsi = "api://AzureADTokenExchange";
        private const string TokenExchangeScopeConfidential = "api://AzureADTokenExchange/.default";

        private readonly object _app;
        private readonly bool _isManagedIdentity;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance with a Managed Identity application.
        /// </summary>
        /// <param name="app">The managed identity application.</param>
        public FicLeg1Acquirer(IManagedIdentityApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);
            _app = app;
            _isManagedIdentity = true;
        }

        /// <summary>
        /// Initializes a new instance with a Confidential Client application.
        /// </summary>
        /// <param name="app">The confidential client application.</param>
        public FicLeg1Acquirer(IConfidentialClientApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);
            _app = app;
            _isManagedIdentity = false;
        }

        /// <summary>
        /// Acquires Leg 1 token for api://AzureADTokenExchange with mTLS PoP.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Authentication result with mTLS PoP token and binding certificate.</returns>
        /// <exception cref="InvalidOperationException">If token type or certificate is invalid.</exception>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            AuthenticationResult result;

            if (_isManagedIdentity)
            {
                var app = (IManagedIdentityApplication)_app;

                result = await app
                    .AcquireTokenForManagedIdentity(TokenExchangeScopeMsi)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                var app = (IConfidentialClientApplication)_app;

                result = await app
                    .AcquireTokenForClient(new[] { TokenExchangeScopeConfidential })
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            // Validate result
            if (result.TokenType != Constants.MtlsPoPTokenType)
            {
                throw new InvalidOperationException(
                    $"Expected token type '{Constants.MtlsPoPTokenType}' but got '{result.TokenType}'. " +
                    "Ensure Leg 1 used .WithMtlsProofOfPossession().");
            }

            if (result.BindingCertificate == null)
            {
                throw new InvalidOperationException(
                    "BindingCertificate is null in Leg 1 result. This should not happen with mTLS PoP tokens.");
            }

            return result;
        }

        /// <summary>
        /// Acquires Leg 1 token with optional tenant override.
        /// </summary>
        /// <param name="tenantId">Optional tenant ID to override the default tenant.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Authentication result with mTLS PoP token and binding certificate.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(tenantId);

            if (_isManagedIdentity)
            {
                // MSI doesn't support tenant override in the same way
                // Use WithTenantId on the request builder if needed
                var app = (IManagedIdentityApplication)_app;

                var result = await app
                    .AcquireTokenForManagedIdentity(TokenExchangeScopeMsi)
                    .WithMtlsProofOfPossession()
                    .WithTenantId(tenantId)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);

                return result;
            }
            else
            {
                var app = (IConfidentialClientApplication)_app;

                var result = await app
                    .AcquireTokenForClient(new[] { TokenExchangeScopeConfidential })
                    .WithMtlsProofOfPossession()
                    .WithTenantId(tenantId)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);

                return result;
            }
        }

        /// <summary>
        /// Disposes the underlying application if it's disposable.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            // Note: IManagedIdentityApplication and IConfidentialClientApplication
            // don't implement IDisposable in MSAL.NET 4.x, so no cleanup needed.
            _disposed = true;
        }
    }
}
