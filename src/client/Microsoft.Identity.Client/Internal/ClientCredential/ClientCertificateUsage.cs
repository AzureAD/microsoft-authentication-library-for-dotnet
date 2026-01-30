// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Describes how a client certificate is used in the authentication flow.
    /// </summary>
    internal enum ClientCertificateUsage
    {
        /// <summary>
        /// No certificate or not applicable.
        /// </summary>
        None,

        /// <summary>
        /// Certificate is used to sign client assertions (JWT).
        /// This is the default usage for certificate-based authentication.
        /// </summary>
        Assertion,

        /// <summary>
        /// Certificate is used for MTLS Proof-of-Possession binding.
        /// The certificate is used for the TLS handshake and binds the token to the TLS channel.
        /// </summary>
        MtlsBinding
    }
}
