// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Persistence interface for IMDSv2 mTLS binding certificates.
    /// Implementations must be best-effort and non-throwing.
    /// </summary>
    internal interface IPersistentCertificateCache
    {
        /// <summary>
        /// Reads the newest valid (≥24h remaining, has private key) entry for the alias.
        /// </summary>
        bool Read(string alias, out CertificateCacheValue value, ILoggerAdapter logger = null);

        /// <summary>
        /// Persists the certificate for the alias (best-effort). Implementations should
        /// tag entries to allow alias scoping and prune expired duplicates conservatively.
        /// </summary>
        void Write(string alias, X509Certificate2 cert, string endpointBase, ILoggerAdapter logger = null);

        /// <summary>
        /// Prunes expired entries for the alias (best-effort).
        /// </summary>
        void Delete(string alias, ILoggerAdapter logger = null);
    }
}
