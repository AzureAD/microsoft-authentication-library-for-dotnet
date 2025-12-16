// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET5_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
using Microsoft.Identity.Client.KeyAttestation.Attestation;

namespace Microsoft.Identity.Client.KeyAttestation
{
#if NET5_0_OR_GREATER
    /// <summary>
    /// Module initializer that runs when the KeyAttestation assembly is loaded.
    /// Automatically registers the KeyGuard attestation provider with MSAL.
    /// </summary>
    internal static class ModuleInitializer
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries", Justification = "Required for auto-registration of attestation provider")]
        [ModuleInitializer]
        internal static void Initialize()
        {
            // Force the static constructor of AttestationProviderInitializer to run,
            // which registers the KeyGuard attestation provider
            AttestationProviderInitializer.Initialize();
        }
    }
#else
    // For .NET Standard 2.0 and .NET Framework, we rely on the static constructor
    // being triggered when the extension method is first called.
    // This ensures the provider is registered before it's needed.
#endif
}
