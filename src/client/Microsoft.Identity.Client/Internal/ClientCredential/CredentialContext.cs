// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Immutable context containing all information that credentials need to resolve themselves.
    /// This struct is passed to IClientCredential.GetCredentialMaterialAsync().
    /// </summary>
    internal readonly struct CredentialContext
    {
        public CredentialContext(
            string clientId,
            string tokenEndpoint,
            ClientAuthMode mode,
            string claims,
            string[] clientCapabilities,
            ICryptographyManager cryptographyManager,
            bool? sendX5C,
            bool useSha2,
            string extraClientAssertionClaims,
            string clientAssertionFmiPath,
            AuthorityType authorityType,
            string azureRegion)
        {
            ClientId = clientId;
            TokenEndpoint = tokenEndpoint;
            Mode = mode;
            Claims = claims;
            ClientCapabilities = clientCapabilities;
            CryptographyManager = cryptographyManager;
            SendX5C = sendX5C;
            UseSha2 = useSha2;
            ExtraClientAssertionClaims = extraClientAssertionClaims;
            ClientAssertionFmiPath = clientAssertionFmiPath;
            AuthorityType = authorityType;
            AzureRegion = azureRegion;
        }

        /// <summary>
        /// The client ID of the application.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// The token endpoint URL.
        /// </summary>
        public string TokenEndpoint { get; }

        /// <summary>
        /// The authentication mode (Regular or MtlsMode).
        /// </summary>
        public ClientAuthMode Mode { get; }

        /// <summary>
        /// Claims to be included in the request.
        /// </summary>
        public string Claims { get; }

        /// <summary>
        /// Client capabilities to be included in assertions.
        /// </summary>
        public string[] ClientCapabilities { get; }

        /// <summary>
        /// Cryptography manager for signing operations.
        /// </summary>
        public ICryptographyManager CryptographyManager { get; }

        /// <summary>
        /// Whether to send the X5C header (certificate chain).
        /// </summary>
        public bool? SendX5C { get; }

        /// <summary>
        /// Whether to use SHA-256 for signing (vs SHA-512).
        /// </summary>
        public bool UseSha2 { get; }

        /// <summary>
        /// Extra claims to include in client assertions.
        /// </summary>
        public string ExtraClientAssertionClaims { get; }

        /// <summary>
        /// FMI path suffix for client assertions.
        /// </summary>
        public string ClientAssertionFmiPath { get; }

        /// <summary>
        /// The type of authority (AAD, B2C, ADFS, etc.).
        /// </summary>
        public AuthorityType AuthorityType { get; }

        /// <summary>
        /// Azure region for regional endpoints.
        /// </summary>
        public string AzureRegion { get; }
    }
}
