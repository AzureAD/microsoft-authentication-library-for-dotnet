// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Immutable snapshot of a cached certificate and its associated metadata.
    /// </summary>
    internal readonly struct CertificateCacheValue(X509Certificate2 certificate, string endpoint, string clientId)
    {

        /// <summary>The certificate (clone owned by the caller).</summary>
        public X509Certificate2 Certificate { get; } = certificate ?? throw new ArgumentNullException(nameof(certificate));

        /// <summary>The base endpoint to use with this certificate.</summary>
        public string Endpoint { get; } = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

        /// <summary>The canonical client id to be posted to the mTLS token endpoint.</summary>
        public string ClientId { get; } = clientId ?? throw new ArgumentNullException(nameof(clientId));
    }
}
