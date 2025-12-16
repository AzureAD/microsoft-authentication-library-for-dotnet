// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ManagedIdentity;

namespace Microsoft.Identity.Client.KeyAttestation.Attestation
{
    /// <summary>
    /// Implementation of IAttestationProvider for KeyGuard attestation.
    /// This provider is automatically registered when the KeyAttestation package is loaded.
    /// </summary>
    internal class KeyGuardAttestationProvider : IAttestationProvider
    {
        public async Task<ManagedIdentity.AttestationResult> AttestKeyGuardAsync(
            string attestationEndpoint,
            SafeHandle keyHandle,
            string clientId,
            CancellationToken cancellationToken)
        {
            try
            {
                // Call the existing PopKeyAttestor implementation
                var result = await PopKeyAttestor.AttestKeyGuardAsync(
                    attestationEndpoint,
                    keyHandle,
                    clientId,
                    cancellationToken).ConfigureAwait(false);

                // Map the result to the MSAL interface types
                return new ManagedIdentity.AttestationResult
                {
                    Status = result.Status == AttestationStatus.Success 
                        ? ManagedIdentity.AttestationStatus.Success 
                        : ManagedIdentity.AttestationStatus.Failed,
                    Jwt = result.Jwt,
                    ErrorMessage = result.ErrorMessage,
                    NativeErrorCode = result.NativeErrorCode
                };
            }
            catch (Exception ex)
            {
                return new ManagedIdentity.AttestationResult
                {
                    Status = ManagedIdentity.AttestationStatus.Failed,
                    ErrorMessage = ex.Message,
                    NativeErrorCode = -1
                };
            }
        }
    }

    /// <summary>
    /// Static initializer that registers the KeyGuard attestation provider
    /// when the KeyAttestation assembly is loaded.
    /// </summary>
    internal static class AttestationProviderInitializer
    {
        static AttestationProviderInitializer()
        {
            // Register the provider when this type is first accessed
            AttestationProviderRegistry.RegisterProvider(new KeyGuardAttestationProvider());
        }

        /// <summary>
        /// Method to force static constructor execution.
        /// Called from module initializer.
        /// </summary>
        internal static void Initialize()
        {
            // This method body is intentionally empty.
            // Its purpose is to trigger the static constructor above.
        }
    }
}
