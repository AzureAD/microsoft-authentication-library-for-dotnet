// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Determines how the client authenticates when acquiring tokens.
    /// Replaces the confusing pair of boolean flags previously used to signal mTLS vs. regular flows.
    /// </summary>
    internal enum CredentialTransportProtocol
    {
        /// <summary>
        /// Standard OAuth client authentication: client secret, JWT bearer assertion, or JWT-PoP assertion.
        /// </summary>
        OAuth,

        /// <summary>
        /// mTLS authentication: the credential must supply a certificate for binding to the
        /// TLS transport layer. No client_secret is valid here; JWT-PoP assertions are issued when
        /// a certificate-bound delegate credential is used.
        /// </summary>
        Mtls
    }
}
