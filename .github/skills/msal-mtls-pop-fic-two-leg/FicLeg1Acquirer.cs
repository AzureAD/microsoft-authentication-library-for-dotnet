// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPop.Fic
{
    /// <summary>
    /// Helper for acquiring Leg 1 tokens in FIC two-leg flow.
    /// Supports both Managed Identity and Confidential Client authentication.
    /// Always targets api://AzureADTokenExchange with mTLS PoP and attestation support.
    /// </summary>
    /// <remarks>
    /// Production-ready implementation following MSAL.NET conventions:
    /// - ConfigureAwait(false) on all awaits
    /// - CancellationToken support with defaults
    /// - Proper IDisposable implementation
    /// - Input validation and disposal checks
    /// </remarks>
    public sealed class FicLeg1Acquirer : IDisposable
    {
        private const string TokenExchangeResource = "api://AzureADTokenExchange";
        
        private readonly IManagedIdentityApplication _msiApp;
        private readonly IConfidentialClientApplication _confApp;
        private readonly bool _isManagedIdentity;
        private bool _disposed;

        /// <summary>
        /// Creates a Leg 1 acquirer for Managed Identity scenarios.
        /// </summary>
        /// <param name="msiApp">Configured Managed Identity application.</param>
        /// <exception cref="ArgumentNullException">Thrown when msiApp is null.</exception>
        public FicLeg1Acquirer(IManagedIdentityApplication msiApp)
        {
            ArgumentNullException.ThrowIfNull(msiApp);
            
            _msiApp = msiApp;
            _isManagedIdentity = true;
        }

        /// <summary>
        /// Creates a Leg 1 acquirer for Confidential Client scenarios.
        /// </summary>
        /// <param name="confApp">Configured Confidential Client application.</param>
        /// <exception cref="ArgumentNullException">Thrown when confApp is null.</exception>
        public FicLeg1Acquirer(IConfidentialClientApplication confApp)
        {
            ArgumentNullException.ThrowIfNull(confApp);
            
            _confApp = confApp;
            _isManagedIdentity = false;
        }

        /// <summary>
        /// Acquires a Leg 1 token for api://AzureADTokenExchange with mTLS PoP and attestation.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>
        /// Authentication result containing mTLS PoP token for api://AzureADTokenExchange.
        /// This token will be used as the assertion in Leg 2.
        /// </returns>
        /// <exception cref="ObjectDisposedException">Thrown when the acquirer has been disposed.</exception>
        /// <exception cref="MsalException">Thrown when token acquisition fails.</exception>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            AuthenticationResult result;

            if (_isManagedIdentity)
            {
                // MSI path with attestation support
                result = await _msiApp
                    .AcquireTokenForManagedIdentity(TokenExchangeResource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()  // Credential Guard attestation
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                // Confidential Client path
                result = await _confApp
                    .AcquireTokenForClient(new[] { $"{TokenExchangeResource}/.default" })
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            // Validate result
            if (result.BindingCertificate == null)
            {
                throw new InvalidOperationException(
                    "BindingCertificate is null after Leg 1 token acquisition. " +
                    "This should not happen if .WithMtlsProofOfPossession() was called correctly.");
            }

            if (string.IsNullOrEmpty(result.AccessToken))
            {
                throw new InvalidOperationException(
                    "AccessToken is null or empty after Leg 1 token acquisition.");
            }

            return result;
        }

        /// <summary>
        /// Disposes the acquirer. Note that the underlying MSAL application
        /// instances are NOT disposed, as they may be reused elsewhere.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            // Note: We don't dispose _msiApp or _confApp as they may be reused
            // Caller is responsible for disposing those when appropriate
            _disposed = true;
        }
    }
}
