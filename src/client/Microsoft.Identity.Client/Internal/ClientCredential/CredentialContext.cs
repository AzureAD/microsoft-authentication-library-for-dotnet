// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Context for credential resolution.
    /// Contains all information credentials need to produce material.
    /// Immutable by design.
    /// </summary>
    internal readonly record struct CredentialContext
    {
        /// <summary>
        /// Application's client ID.
        /// </summary>
        public string ClientId { get; init; }

        /// <summary>
        /// Token endpoint URL (audience for JWT-based credentials).
        /// </summary>
        public string TokenEndpoint { get; init; }

        /// <summary>
        /// Client authentication mode (Regular or MtlsMode).
        /// Determines what authentication material to produce.
        /// </summary>
        public ClientAuthMode Mode { get; init; }

        /// <summary>
        /// Additional claims to include in assertions (if supported by credential).
        /// </summary>
        public string Claims { get; init; }

        /// <summary>
        /// Client capabilities (for client assertion JWT generation).
        /// </summary>
        public IReadOnlyCollection<string> ClientCapabilities { get; init; }

        /// <summary>
        /// Cryptography manager for signing operations (JWT, etc.).
        /// Required for certificate-based credentials.
        /// </summary>
        public ICryptographyManager CryptographyManager { get; init; }

        /// <summary>
        /// Whether to send X5C header in JWT.
        /// </summary>
        public bool SendX5C { get; init; }

        /// <summary>
        /// Whether the authority supports SHA-2 credentials.
        /// </summary>
        public bool UseSha2 { get; init; }

        /// <summary>
        /// Extra claims to include in client assertion (takes precedence over regular Claims).
        /// Used for scenarios like cache key binding.
        /// </summary>
        public string ExtraClientAssertionClaims { get; init; }

        /// <summary>
        /// FMI path for client assertion (Federated Managed Identity).
        /// </summary>
        public string ClientAssertionFmiPath { get; init; }

        /// <summary>
        /// Authority type (AAD, B2C, ADFS, etc.).
        /// Used for mTLS validation - some authority types have mTLS restrictions.
        /// </summary>
        public AuthorityType AuthorityType { get; init; }

        /// <summary>
        /// Azure region configured for this client.
        /// Required when using mTLS mode with AAD.
        /// </summary>
        public string AzureRegion { get; init; }
    }
}
