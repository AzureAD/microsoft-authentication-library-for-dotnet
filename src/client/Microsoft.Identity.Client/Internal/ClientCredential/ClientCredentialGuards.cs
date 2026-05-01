// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Shared guard methods for credential implementations.
    /// </summary>
    internal static class ClientCredentialGuards
    {
        /// <summary>
        /// Throws <see cref="MsalClientException"/> when a credential that cannot supply
        /// a token-binding certificate is used in mTLS mode.
        /// </summary>
        /// <param name="context">The current credential context.</param>
        /// <param name="credentialDescription">
        /// Human-readable description of the credential type (e.g., "A client secret").
        /// </param>
        internal static void ThrowIfMtlsNotSupported(CredentialContext context, string credentialDescription)
        {
            if (context.Mode == CredentialTransportProtocol.Mtls)
            {
                throw new MsalClientException(
                    MsalError.InvalidCredentialMaterial,
                    $"{credentialDescription} cannot be used over mTLS. " +
                    "Use a certificate credential or a ClientSignedAssertion callback " +
                    "that can return a token-binding certificate.");
            }
        }
    }
}
