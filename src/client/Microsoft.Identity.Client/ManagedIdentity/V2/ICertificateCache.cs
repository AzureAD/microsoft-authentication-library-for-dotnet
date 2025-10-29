// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Process-local cache for an mTLS certificate and its endpoint.
    /// Expiration is based solely on certificate.NotAfter.
    /// </summary>
    internal interface ICertificateCache
    {
        /// <summary>
        /// Try to get a cached certificate+endpoint+clientId for the specified cacheKey.
        /// Returns true and non-null outputs if found and not expired.
        /// </summary>
        bool TryGet(
            string cacheKey, 
            out CertificateCacheValue value, 
            ILoggerAdapter logger = null);

        /// <summary>
        /// Insert or replace the cached certificate+endpoint+clientId for cacheKey.
        /// </summary>
        void Set(
            string cacheKey,
            in CertificateCacheValue value,
            ILoggerAdapter logger = null);

        /// <summary>Remove an entry if present.</summary>
        bool Remove(string cacheKey, ILoggerAdapter logger = null);

        /// <summary>Clear all entries.</summary>
        void Clear(ILoggerAdapter logger = null);
    }
}
