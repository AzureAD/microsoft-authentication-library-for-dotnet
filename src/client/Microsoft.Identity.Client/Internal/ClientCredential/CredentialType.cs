// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Represents the type of credential used for authentication.
    /// </summary>
    internal enum CredentialType
    {
        /// <summary>
        /// Client secret (shared secret string).
        /// </summary>
        ClientSecret,

        /// <summary>
        /// Client certificate (X.509 certificate with private key).
        /// </summary>
        ClientCertificate,

        /// <summary>
        /// Client assertion (user-provided JWT or delegate).
        /// </summary>
        ClientAssertion,

        /// <summary>
        /// Federated identity credential (OIDC token exchange).
        /// </summary>
        FederatedIdentityCredential,

        /// <summary>
        /// Managed identity (Azure system-assigned or user-assigned identity).
        /// </summary>
        ManagedIdentity
    }
}
