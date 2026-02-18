// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Specifies the authentication mode for client credentials.
    /// </summary>
    internal enum ClientAuthMode
    {
        /// <summary>
        /// Regular authentication mode (non-mTLS).
        /// </summary>
        Regular,

        /// <summary>
        /// Mutual TLS (mTLS) authentication mode.
        /// Requires a certificate for the TLS handshake.
        /// </summary>
        MtlsMode
    }
}
