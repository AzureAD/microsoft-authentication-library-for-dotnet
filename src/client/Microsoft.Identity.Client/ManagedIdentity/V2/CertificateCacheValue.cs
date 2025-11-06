// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Immutable snapshot of a cached certificate and its associated metadata.
    /// </summary>
    internal readonly struct CertificateCacheValue
    {
        public CertificateCacheValue(X509Certificate2 certificate, string endpoint, string clientId)
        {
            if (certificate == null) throw new ArgumentNullException(nameof(certificate));
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));

            Certificate = certificate;
            Endpoint = endpoint;
            ClientId = clientId;
        }

        /// <summary>The certificate (clone owned by the caller).</summary>
        public X509Certificate2 Certificate { get; }

        /// <summary>The base endpoint to use with this certificate.</summary>
        public string Endpoint { get; }

        /// <summary>The canonical client id to be posted to the mTLS token endpoint.</summary>
        public string ClientId { get; }
    }
}
