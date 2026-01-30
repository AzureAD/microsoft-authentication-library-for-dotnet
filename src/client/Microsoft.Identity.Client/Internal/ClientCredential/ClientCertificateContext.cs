// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Represents a resolved client certificate with metadata about its intended usage.
    /// </summary>
    /// <remarks>
    /// This class encapsulates both the certificate and how it will be used in the authentication flow.
    /// The same certificate may be used for different purposes (JWT signing vs MTLS binding) depending
    /// on the request configuration (IsMtlsPopRequested flag).
    /// </remarks>
    internal class ClientCertificateContext
    {
        /// <summary>
        /// Gets or sets the X509 certificate to be used for authentication.
        /// </summary>
        public X509Certificate2 Certificate { get; set; }

        /// <summary>
        /// Gets or sets how this certificate will be used in the authentication flow.
        /// </summary>
        public ClientCertificateUsage Usage { get; set; }
    }
}
