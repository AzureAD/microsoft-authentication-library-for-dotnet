// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
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
            builder.CommonParameters.IsManagedIdentityPopEnabled = true;
            AddRuntimeSupport(builder);
            return builder;
        }

        private static void AddRuntimeSupport(
            AcquireTokenForManagedIdentityParameterBuilder builder)
        {
            // Register the "runtime" function that PoP operation will invoke.
            builder.CommonParameters.MtlsPopProvider =
                async (req, ct) =>
                {
                    // 1) Get the caller-provided KeyGuard/CNG handle
                    var keyHandle = req.KeyHandle;

                    // 2) Call the native interop via PopKeyAttestor
                    var att = await PopKeyAttestor.AttestKeyGuardAsync(
                                  req.AttestationEndpoint.AbsoluteUri, // expects string
                                  keyHandle,
                                  req.ClientId ?? string.Empty,
                                  ct).ConfigureAwait(false);

                    // 3) Map to MSAL's internal response
                    if (att != null &&
                        att.Status == Attestation.AttestationStatus.Success &&
                        !string.IsNullOrWhiteSpace(att.Jwt))
                    {
                        return new MtlsPopResponse { AttestationToken = att.Jwt };
                    }

                    throw new InvalidOperationException(
                        $"Attestation failed (status={att?.Status}, code={att?.NativeErrorCode}). {att?.ErrorMessage}");
                };
        }
    }
}
