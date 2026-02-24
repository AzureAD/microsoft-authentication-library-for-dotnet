// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Persistence interface for IMDSv2 mTLS binding certificates.
    /// Implementations must be best-effort and non-throwing so that
    /// certificate persistence never blocks authentication.
    /// </summary>
    internal interface IPersistentCertificateCache
    {
        /// <summary>
        /// Reads the newest valid (≥24h remaining, has private key) entry for the alias.
        /// Returns <c>true</c> on cache hit, <c>false</c> otherwise.
        /// </summary>
        bool Read(string alias, out CertificateCacheValue value, ILoggerAdapter logger);

        /// <summary>
        /// Persists the certificate for the alias (best-effort).
        /// Implementations should log failures but must not throw; callers do not
        /// depend on persistence succeeding and fall back to in-memory cache only.
        /// </summary>
        void Write(string alias, X509Certificate2 cert, string endpointBase, ILoggerAdapter logger);

        /// <summary>
        /// Deletes expired certificate entries for the alias (best-effort),
        /// leaving the latest valid binding for the alias in place (if any).
        /// Write calls DeleteAllForAlias, so this method is only expected to be called 
        /// by implementations of Write. 
        /// </summary>
        void Delete(string alias, ILoggerAdapter logger);

        /// <summary>
        /// Deletes ALL certificate entries for the alias (best-effort), including non-expired ones.
        /// Intended for "reset/evict" scenarios (e.g., SCHANNEL rejects the cached cert) to force a 
        /// re-mint. When a machine restarts the key becomes inaccessible and the cached certs should 
        /// be cleared to allow a new cert to be minted.
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="logger"></param>
        void DeleteAllForAlias(string alias, ILoggerAdapter logger);
    }
}
