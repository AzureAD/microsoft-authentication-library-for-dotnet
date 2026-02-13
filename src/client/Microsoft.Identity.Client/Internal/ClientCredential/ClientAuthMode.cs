// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Client authentication mode for credential resolution.
    /// Determines what authentication material to produce.
    /// </summary>
    internal enum ClientAuthMode
    {
        /// <summary>
        /// Regular client authentication using JWT assertion or client secret.
        /// Produces OAuth2 token request parameters (client_assertion, client_secret).
        /// </summary>
        Regular,

        /// <summary>
        /// Mutual TLS (mTLS) bearer mode.
        /// Certificate used for TLS client authentication only (no JWT assertion).
        /// Produces certificate for HTTP client certificate collection.
        /// </summary>
        MtlsMode
    }
}
