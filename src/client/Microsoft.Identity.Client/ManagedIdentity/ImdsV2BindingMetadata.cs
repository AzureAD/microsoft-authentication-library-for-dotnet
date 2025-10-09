// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using Microsoft.Identity.Client.ManagedIdentity.V2;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Stores and manages certificate binding metadata for Azure managed identities using IMDSv2.
    /// This class caches certificate information and STS endpoints per identity (MSI client ID),
    /// maintaining separate mappings for different token types to ensure proper security isolation.
    /// </summary>
    /// <remarks>
    /// Each managed identity can have separate certificate bindings for different authentication methods:
    /// - Bearer tokens: Standard OAuth2 bearer tokens
    /// - PoP (Proof of Possession) tokens: Enhanced security tokens bound to a specific certificate
    /// 
    /// The Subject is set once (first-wins pattern) while thumbprints can rotate during certificate renewal.
    /// This design allows proper certificate rotation while maintaining stable subject identities.
    /// </remarks>
    internal class ImdsV2BindingMetadata
    {
        /// <summary>
        /// The X.509 certificate subject distinguished name used for this identity.
        /// This value is set once (first-wins) and persists across certificate rotations.
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Response data for Bearer token certificate authentication, including
        /// certificate data and STS endpoint information.
        /// </summary>
        public CertificateRequestResponse BearerResponse { get; set; }

        /// <summary>
        /// Thumbprint of the certificate used for Bearer token authentication.
        /// Updated during certificate rotation.
        /// </summary>
        public string BearerThumbprint { get; set; }

        /// <summary>
        /// Response data for PoP (Proof of Possession) token certificate authentication,
        /// including certificate data and STS endpoint information.
        /// </summary>
        public CertificateRequestResponse PopResponse { get; set; }

        /// <summary>
        /// Thumbprint of the certificate used for PoP token authentication.
        /// Updated during certificate rotation.
        /// </summary>
        public string PopThumbprint { get; set; }
    }
}
