// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.KeyAttestation.Attestation
{
    /// <summary>
    /// Represents a cached MAA (Microsoft Azure Attestation) token entry with expiry tracking.
    /// </summary>
    internal sealed class MaaTokenCacheEntry
    {
        /// <summary>
        /// The cached MAA attestation JWT token.
        /// </summary>
        public string Token { get; }

        /// <summary>
        /// The UTC time when the token was issued.
        /// </summary>
        public DateTimeOffset IssuedAt { get; }

        /// <summary>
        /// The UTC time when the token expires.
        /// </summary>
        public DateTimeOffset ExpiresAt { get; }

        /// <summary>
        /// The total lifetime duration of the token.
        /// </summary>
        public TimeSpan Lifetime => ExpiresAt - IssuedAt;

        /// <summary>
        /// Creates a new MAA token cache entry.
        /// </summary>
        /// <param name="token">The MAA JWT token.</param>
        /// <param name="issuedAt">The UTC time when the token was issued.</param>
        /// <param name="expiresAt">The UTC time when the token expires.</param>
        public MaaTokenCacheEntry(string token, DateTimeOffset issuedAt, DateTimeOffset expiresAt)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentNullException(nameof(token));

            if (expiresAt <= issuedAt)
                throw new ArgumentException("ExpiresAt must be after IssuedAt.", nameof(expiresAt));

            Token = token;
            IssuedAt = issuedAt;
            ExpiresAt = expiresAt;
        }

        /// <summary>
        /// Checks whether the token needs refresh based on the 50% lifetime threshold.
        /// Returns true if less than 50% of the token lifetime remains.
        /// </summary>
        public bool NeedsRefresh(DateTimeOffset now)
        {
            if (now >= ExpiresAt)
            {
                // Token is expired
                return true;
            }

            // Calculate remaining lifetime percentage
            var totalLifetime = Lifetime;
            var remainingLifetime = ExpiresAt - now;

            // Refresh if less than 50% lifetime remains
            return remainingLifetime < (totalLifetime / 2);
        }

        /// <summary>
        /// Checks whether the token is expired.
        /// </summary>
        public bool IsExpired(DateTimeOffset now)
        {
            return now >= ExpiresAt;
        }
    }
}
