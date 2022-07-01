// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.ServiceEssentials
{
    /// <inheritdoc/>
    internal readonly struct CacheEntry<T> : ICacheEntry<T>, IEquatable<CacheEntry<T>>
    {
        /// <summary>
        /// Gets the cache entry when this is non existing.
        /// </summary>
        public static CacheEntry<T> NonExisting => new CacheEntry<T>();

        /// <inheritdoc/>
        public T Value { get; }

        public DateTimeOffset ExpirationTime { get; }

        public DateTimeOffset LastKnowGoodTime { get; }

        public DateTimeOffset RefreshTime { get; }

        public CacheEntry(T value, DateTimeOffset expirationTime, DateTimeOffset lastKnowGoodTime, DateTimeOffset refreshTime)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            ExpirationTime = expirationTime;
            LastKnowGoodTime = lastKnowGoodTime;
            RefreshTime = refreshTime;
        }

        /// <inheritdoc/>
        public bool IsValid()
        {
            return DateTimeOffset.UtcNow < ExpirationTime;
        }

        /// <inheritdoc/>
        public bool IsValidAsLastKnownGood()
        {
            return DateTimeOffset.UtcNow < LastKnowGoodTime;
        }

        /// <inheritdoc/>
        public bool NeedsRefresh()
        {
            return DateTimeOffset.UtcNow >= RefreshTime;
        }

        /// <summary>
        /// Test for equality.
        /// </summary>
        /// <param name="lhs">Left hand side.</param>
        /// <param name="rhs">Right hand side.</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
        public static bool operator ==(CacheEntry<T> lhs, CacheEntry<T> rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Test for inequality.
        /// </summary>
        /// <param name="lhs">Left hand side.</param>
        /// <param name="rhs">Right hand side.</param>
        /// <returns>true if the current object is not equal to the other parameter; otherwise, false.</returns>
        public static bool operator !=(CacheEntry<T> lhs, CacheEntry<T> rhs)
        {
            return !lhs.Equals(rhs);
        }

        /// <summary>
        /// Generate hashcode.
        /// </summary>
        /// <returns>returns hashcode.</returns>
        public override int GetHashCode()
        {
            return (Value?.GetHashCode() ?? 0) ^
                   (ExpirationTime.GetHashCode()) ^
                   (RefreshTime.GetHashCode()) ^
                   (LastKnowGoodTime.GetHashCode());
        }

        /// <summary>
        /// Checks equality.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>if equal or not.</returns>
        public override bool Equals(object obj)
        {
            if (obj is CacheEntry<T> cacheEntry)
                return Equals(cacheEntry);

            return false;
        }

        /// <summary>
        /// Checks equality.
        /// </summary>
        /// <param name="other">Object to compare.</param>
        /// <returns>if equal or not.</returns>
        public bool Equals(CacheEntry<T> other)
        {
            return Equals(Value, other.Value) &&
                   ExpirationTime == other.ExpirationTime &&
                   RefreshTime == other.RefreshTime &&
                   LastKnowGoodTime == other.LastKnowGoodTime;
        }
    }
}
