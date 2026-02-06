// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Normalized output from credential resolution.
    /// Contains the authentication parameters, optional mTLS certificate, and telemetry metadata.
    /// </summary>
    internal sealed class CredentialMaterial
    {
        /// <summary>
        /// OAuth2 parameters to add to the token request body.
        /// Examples: client_secret, client_assertion, client_assertion_type
        /// </summary>
        public IReadOnlyDictionary<string, string> TokenRequestParameters { get; set; }

        /// <summary>
        /// Optional certificate for mTLS transport / token binding.
        /// </summary>
        public X509Certificate2 MtlsCertificate { get; set; }

        /// <summary>
        /// Optional telemetry-safe metadata about the credential resolution.
        /// </summary>
        public CredentialMaterialMetadata Metadata { get; set; }
    }
}
