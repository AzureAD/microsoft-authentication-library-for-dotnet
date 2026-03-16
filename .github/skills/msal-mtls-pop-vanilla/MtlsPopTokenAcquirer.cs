// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace MsalMtlsPop.Vanilla
{
    /// <summary>
    /// Unified helper for acquiring mTLS PoP tokens with Credential Guard attestation support.
    /// Supports both Managed Identity and Confidential Client authentication methods.
    /// </summary>
    /// <remarks>
    /// Production-ready implementation following MSAL.NET conventions:
    /// - ConfigureAwait(false) on all awaits
    /// - CancellationToken support with defaults
    /// - Proper IDisposable implementation
    /// - Input validation and disposal checks
    /// </remarks>
    public sealed class MtlsPopTokenAcquirer : IDisposable
    {
        private readonly IManagedIdentityApplication _msiApp;
        private readonly IConfidentialClientApplication _confApp;
        private readonly bool _isManagedIdentity;
        private bool _disposed;

        /// <summary>
        /// Creates an acquirer for Managed Identity scenarios.
        /// </summary>
        /// <param name="msiApp">Configured Managed Identity application.</param>
        /// <exception cref="ArgumentNullException">Thrown when msiApp is null.</exception>
        public MtlsPopTokenAcquirer(IManagedIdentityApplication msiApp)
        {
            ArgumentNullException.ThrowIfNull(msiApp);
            
            _msiApp = msiApp;
            _isManagedIdentity = true;
        }

        /// <summary>
        /// Creates an acquirer for Confidential Client scenarios.
        /// </summary>
        /// <param name="confApp">Configured Confidential Client application.</param>
        /// <exception cref="ArgumentNullException">Thrown when confApp is null.</exception>
        public MtlsPopTokenAcquirer(IConfidentialClientApplication confApp)
        {
            ArgumentNullException.ThrowIfNull(confApp);
            
            _confApp = confApp;
            _isManagedIdentity = false;
        }

        /// <summary>
        /// Acquires an mTLS PoP token for the specified resource with Credential Guard attestation.
        /// </summary>
        /// <param name="resource">Target resource URI (e.g., "https://graph.microsoft.com").</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Authentication result containing mTLS PoP token and binding certificate.</returns>
        /// <exception cref="ArgumentNullException">Thrown when resource is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the acquirer has been disposed.</exception>
        /// <exception cref="MsalException">Thrown when token acquisition fails.</exception>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            string resource,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(resource);
            ObjectDisposedException.ThrowIf(_disposed, this);

            AuthenticationResult result;

            if (_isManagedIdentity)
            {
                // MSI path with attestation support
                result = await _msiApp
                    .AcquireTokenForManagedIdentity(resource)
                    .WithMtlsProofOfPossession()
                    .WithAttestationSupport()  // Credential Guard attestation
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                // Confidential Client path
                string[] scopes = resource.EndsWith("/.default", StringComparison.OrdinalIgnoreCase)
                    ? new[] { resource }
                    : new[] { $"{resource}/.default" };

                result = await _confApp
                    .AcquireTokenForClient(scopes)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            // Validate result
            if (result.BindingCertificate == null)
            {
                throw new InvalidOperationException(
                    "BindingCertificate is null after token acquisition. " +
                    "This should not happen if .WithMtlsProofOfPossession() was called correctly.");
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
