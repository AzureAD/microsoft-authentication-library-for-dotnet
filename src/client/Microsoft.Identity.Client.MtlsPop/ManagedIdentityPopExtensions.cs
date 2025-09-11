// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.MtlsPop.Attestation;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.MtlsPop
{
    /// <summary>
    /// Registers the mTLS PoP attestation runtime (interop) by installing a provider
    /// function into MSAL's internal config.
    /// </summary>
    public static class ManagedIdentityPopExtensions
    {
        /// <summary>
        /// App-level registration: tells MSAL how to obtain a KeyGuard/CNG handle
        /// and perform attestation to get the JWT needed for mTLS PoP.
        /// </summary>
        public static AcquireTokenForManagedIdentityParameterBuilder WithMtlsProofOfPossession(
            this AcquireTokenForManagedIdentityParameterBuilder builder)
        {
            builder.CommonParameters.IsMtlsPopRequested = true;
            AddRuntimeSupport(builder);
            return builder;
        }

        /// <summary>
        /// Adds the runtime support by registering the attestation function.
        /// </summary>
        /// <param name="builder"></param>
        /// <exception cref="MsalClientException"></exception>
        private static void AddRuntimeSupport(
            AcquireTokenForManagedIdentityParameterBuilder builder)
        {
            // Register the "runtime" function that PoP operation will invoke.
            builder.CommonParameters.AttestationTokenProvider =
                async (req, ct) =>
                {
                    // 1) Get the caller-provided KeyGuard/CNG handle
                    SafeHandle keyHandle = req.KeyHandle;

                    // 2) Call the native interop via PopKeyAttestor
                    AttestationResult attestationResult = await PopKeyAttestor.AttestKeyGuardAsync(
                                  req.AttestationEndpoint.AbsoluteUri, // expects string
                                  keyHandle,
                                  req.ClientId ?? string.Empty,
                                  ct).ConfigureAwait(false);

                    // 3) Map to MSAL's internal response
                    if (attestationResult != null &&
                        attestationResult.Status == AttestationStatus.Success &&
                        !string.IsNullOrWhiteSpace(attestationResult.Jwt))
                    {
                        return new ManagedIdentity.AttestationTokenResponse { AttestationToken = attestationResult.Jwt };
                    }

                    throw new MsalClientException(
                        "attestation_failure",
                        $"Key Attestation failed " +
                        $"(status={attestationResult?.Status}, " +
                        $"code={attestationResult?.NativeErrorCode}). {attestationResult?.ErrorMessage}");
                };
        }
    }
}
