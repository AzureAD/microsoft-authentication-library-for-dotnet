// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Extensions.FileCache
{
    /// <summary>
    /// Generic secure cache for short‑lived payloads (tokens, proof material, etc.).
    /// Freshness decisions are time‑based via <c>expires_on</c> and (optionally) <c>refresh_on</c>;
    /// the cache never parses the payload.
    /// </summary>
    public interface ISecureTokenCache
    {
        /// <summary>
        /// Attempts to read a valid payload for the specified <paramref name="bucket"/> and <paramref name="keyId"/>.
        /// </summary>
        /// <param name="bucket">Logical grouping for related cache entries (e.g., "attestation", "pop").</param>
        /// <param name="keyId">Stable, file‑name‑safe identifier for the key/material being cached.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// The payload bytes if a non‑expired entry exists; otherwise <see cref="Array.Empty{T}"/>.
        /// </returns>
        /// <remarks>
        /// This method never returns <see langword="null"/>. An empty array indicates a cache miss or an expired entry.
        /// </remarks>
        Task<byte[]> TryReadAsync(string bucket, string keyId, CancellationToken ct);

        /// <summary>
        /// Writes (or overwrites) a payload and its timing metadata atomically.
        /// </summary>
        /// <param name="bucket">Logical grouping for related cache entries.</param>
        /// <param name="keyId">Stable, file‑name‑safe identifier for the key/material being cached.</param>
        /// <param name="value">
        /// The cache value containing the payload, required <c>expires_on</c>, and optional <c>refresh_on</c>.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        Task WriteAsync(string bucket, string keyId, CacheValue value, CancellationToken ct);

        /// <summary>
        /// Returns a valid payload or, if missing/expired (or past <c>refresh_on</c>), performs a cross‑process‑safe
        /// refresh via <paramref name="factory"/>.
        /// </summary>
        /// <param name="bucket">Logical grouping for related cache entries.</param>
        /// <param name="keyId">Stable, file‑name‑safe identifier for the key/material being cached.</param>
        /// <param name="factory">
        /// A function that produces a new payload together with required <c>expires_on</c> and optional <c>refresh_on</c>.
        /// The cache does not parse the payload; it uses only the supplied times for freshness decisions.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The valid payload (either cached or freshly minted).</returns>
        Task<byte[]> GetOrCreateAsync(
            string bucket,
            string keyId,
            Func<CancellationToken, Task<CacheValue>> factory,
            CancellationToken ct);
    }

    /// <summary>
    /// Value produced by the factory: raw payload plus timing hints. The cache never inspects the payload.
    /// </summary>
    public readonly struct CacheValue
    {
        /// <summary>
        /// Creates a <see cref="CacheValue"/> with a required expiration time and no refresh hint.
        /// </summary>
        /// <param name="payload">Payload bytes. If <see langword="null"/>, treated as an empty array.</param>
        /// <param name="expiresOnUtc">Absolute UTC time when the payload expires.</param>
        public CacheValue(byte[] payload, DateTimeOffset expiresOnUtc)
        {
            Payload = payload ?? Array.Empty<byte>();
            ExpiresOnUtc = expiresOnUtc;
            HasRefreshOnUtc = false;
            RefreshOnUtc = default;
        }

        /// <summary>
        /// Creates a <see cref="CacheValue"/> with both expiration and an earlier refresh hint.
        /// </summary>
        /// <param name="payload">Payload bytes. If <see langword="null"/>, treated as an empty array.</param>
        /// <param name="expiresOnUtc">Absolute UTC time when the payload expires.</param>
        /// <param name="refreshOnUtc">UTC time to proactively refresh before expiry.</param>
        public CacheValue(byte[] payload, DateTimeOffset expiresOnUtc, DateTimeOffset refreshOnUtc)
        {
            Payload = payload ?? Array.Empty<byte>();
            ExpiresOnUtc = expiresOnUtc;
            HasRefreshOnUtc = true;
            RefreshOnUtc = refreshOnUtc;
        }

        /// <summary>Payload bytes.</summary>
        public byte[] Payload { get; }

        /// <summary>Absolute UTC expiration time for the payload.</summary>
        public DateTimeOffset ExpiresOnUtc { get; }

        /// <summary>Indicates whether <see cref="RefreshOnUtc"/> is set.</summary>
        public bool HasRefreshOnUtc { get; }

        /// <summary>
        /// UTC time to proactively refresh the payload before it expires. Meaningful only when <see cref="HasRefreshOnUtc"/> is <see langword="true"/>.
        /// </summary>
        public DateTimeOffset RefreshOnUtc { get; }
    }
}
