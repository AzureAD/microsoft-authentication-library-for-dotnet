// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// In-memory entry owned by the cache. Disposing the entry disposes the certificate it owns.
    /// </summary>
    /// <remarks>
    /// certificate+endpoint+clientId cache entry.
    /// </remarks>
    /// <param name="certificate"></param>
    /// <param name="notAfterUtc"></param>
    /// <param name="endpoint"></param>
    /// <param name="clientId"></param>
    /// <exception cref="ArgumentNullException"></exception>
    internal sealed class CertificateCacheEntry(X509Certificate2 certificate, DateTimeOffset notAfterUtc, string endpoint, string clientId) : IDisposable
    {
        private int _disposed;

        /// <summary>
        /// Represents the minimum remaining lifetime for an operation or resource.
        /// </summary>
        public static readonly TimeSpan MinRemainingLifetime = TimeSpan.FromHours(24);

        /// <summary>
        /// certificate owned by this entry.
        /// </summary>
        public X509Certificate2 Certificate { get; } = certificate ?? throw new ArgumentNullException(nameof(certificate));
        /// <summary>
        /// notAfterUtc of the certificate.
        /// </summary>
        public DateTimeOffset NotAfterUtc { get; } = notAfterUtc;
        /// <summary>
        /// endpoint associated with this certificate.
        /// </summary>
        public string Endpoint { get; } = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        /// <summary>
        /// clientId associated with this certificate.
        /// </summary>
        public string ClientId { get; } = clientId ?? throw new ArgumentNullException(nameof(clientId));

        /// <summary>Whether this entry has been disposed.</summary>
        public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

        /// <summary>
        /// is expired at the specified time.
        /// </summary>
        /// <param name="nowUtc"></param>
        /// <returns></returns>
        public bool IsExpiredUtc(DateTimeOffset nowUtc) => nowUtc >= (NotAfterUtc - MinRemainingLifetime);

        /// <summary>
        /// dispose the entry and its certificate.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return; // already disposed
            }

            // Idempotent due to the atomic guard
            Certificate.Dispose();
        }
    }
}
