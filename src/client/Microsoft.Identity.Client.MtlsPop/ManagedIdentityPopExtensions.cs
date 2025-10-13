// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.MtlsPop.Attestation;

namespace Microsoft.Identity.Client.MtlsPop
{
    /// <summary>
    /// Registers the mTLS PoP attestation runtime (interop) by installing a provider
    /// function into MSAL's internal config.
    /// </summary>
    public static class ManagedIdentityPopExtensions
    {
        // One cached instance per process; thread-safe lazy init.
        private static readonly System.Lazy<IAttestationProvider> s_attProvider =
            new System.Lazy<IAttestationProvider>(
                () => CachedAttestationProviderFactory.CreateDefault(),
                System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

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

        // Register the provider that uses the on‑disk cache.
        private static void AddRuntimeSupport(AcquireTokenForManagedIdentityParameterBuilder builder)
        {
            // MSAL Core will pass AttestationTokenInput (req) here.
            // We simply forward to the cached provider. No native call here.
            builder.CommonParameters.AttestationTokenProvider = (req, ct) =>
                s_attProvider.Value.GetAsync(req, ct);
        }
    }
}
