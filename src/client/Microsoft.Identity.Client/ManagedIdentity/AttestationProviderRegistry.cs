// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Static registry for attestation providers.
    /// The KeyAttestation package registers itself here via InternalsVisibleTo.
    /// </summary>
    internal static class AttestationProviderRegistry
    {
        private static IAttestationProvider s_provider;

        /// <summary>
        /// Gets the current attestation provider, if one has been registered.
        /// </summary>
        internal static IAttestationProvider Provider => s_provider;

        /// <summary>
        /// Registers an attestation provider. Called by the KeyAttestation package.
        /// </summary>
        /// <param name="provider">The attestation provider to register.</param>
        internal static void RegisterProvider(IAttestationProvider provider)
        {
            s_provider = provider;
        }

        /// <summary>
        /// Clears the registered provider. Used for testing.
        /// </summary>
        internal static void ClearProvider()
        {
            s_provider = null;
        }
    }
}
