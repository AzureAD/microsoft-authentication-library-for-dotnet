// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace Microsoft.Identity.ServiceEssentials
{
    /// <summary>
    /// Represents the cache options applied to an <see cref="ICacheEntry{T}"/>.
    /// </summary>
    public class CacheEntryOptions
    {
        private TimeSpan _timeToExpire = TimeSpan.FromHours(1);
        private TimeSpan _timeToRefresh = TimeSpan.MaxValue;
        private TimeSpan _timeToRemove = TimeSpan.FromHours(1);

        /// <summary>
        /// Logical time-to-live for the <see cref="ICacheEntry{T}"/>, relative to time now.
        /// </summary>
        public TimeSpan TimeToExpire
        {
            get => _timeToExpire;
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _timeToExpire = value;
            }
        }

        /// <summary>
        /// Emergency time-to-live for the cache item, relative to time now.
        /// Represents the actual expiration time for the <see cref="ICacheEntry{T}"/> in a cache storage and can be used as a 'last-known-good'
        /// value in scenarios when primary data source is not available.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="TimeToExpire"/>.
        /// </remarks>
        public TimeSpan TimeToRemove
        {
            get => _timeToRemove;
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _timeToRemove = value;
            }
        }

        /// <summary>
        /// Refresh time-to-live for the <see cref="ICacheEntry{T}"/>, relative to time now.
        /// This value can be used in proactive <see cref="ICacheEntry{T}"/> refresh scenarios.
        /// </summary>
        /// <remarks>
        /// <see cref="TimeSpan.MaxValue"/> for no refresh (default).
        /// </remarks>
        public TimeSpan TimeToRefresh
        {
            get => _timeToRefresh;
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _timeToRefresh = value;
            }
        }

        /// <summary>
        /// Flag that indicates whether the <see cref="ICacheEntry{T}"/> should be stored only in a local cache.
        /// </summary>
        public bool StoreToLocalCacheOnly { get; set; }

        /// <summary>
        /// Value that can be used to randomize spans in <see cref="CacheEntryOptions"/>.
        /// </summary>
        /// <remarks>
        /// Negative value will be used to randomize spans to a maximum of -<see cref="JitterInSeconds"/>,
        /// while positive value will be used to randomize spans to a maximum of +-<see cref="JitterInSeconds"/>.
        /// </remarks>
        public int JitterInSeconds { get; set; }

        /// <summary>
        /// Should be used for deserialization only.
        /// </summary>
        /// <remarks>
        /// <see cref=" MissingMethodException"/> will be thrown if System.Text.Json is used to
        /// serialize this class on targets below .NET Core 3.0, if public parameterless constructor is not defined.
        /// https://docs.microsoft.com/en-us/dotnet/core/compatibility/serialization/5.0/non-public-parameterless-constructors-not-used-for-deserialization
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This constructor is for serialization")]
        public CacheEntryOptions()
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CacheEntryOptions"/>.
        /// </summary>
        /// <param name="timeToExpire">Logical time-to-live of the cache item, relative to now.</param>
        /// <remarks>
        /// <see cref="TimeToRemove"/> is set to <paramref name="timeToExpire"/> by default.
        /// <see cref="TimeToRefresh"/> is set to <paramref name="timeToExpire"/> by default.
        /// <see cref="JitterInSeconds"/> is set to 0 by default.
        /// </remarks> 
        public CacheEntryOptions(TimeSpan timeToExpire)
           : this (timeToExpire, 0)
        { }

        /// <summary>
        /// Creates a new instance of the <see cref="CacheEntryOptions"/>.
        /// </summary>
        /// <param name="timeToExpire">Logical time-to-live of the cache item, relative to now.</param>
        /// <param name="jitterInSeconds">
        /// Negative value will be used to randomize spans to a maximum of -<paramref name="jitterInSeconds"/>,
        /// while positive value will be used to randomize spans to a maximum of +-<paramref name="jitterInSeconds"/>.
        /// </param>
        /// <remarks>
        /// <see cref="TimeToRemove"/> is set to <paramref name="timeToExpire"/> by default.
        /// <see cref="TimeToRefresh"/> is set to <paramref name="timeToExpire"/> by default.
        /// </remarks> 
        public CacheEntryOptions(TimeSpan timeToExpire, int jitterInSeconds)
        {
            TimeToExpire = timeToExpire;
            TimeToRemove = timeToExpire;
            TimeToRefresh = TimeSpan.MaxValue;
            StoreToLocalCacheOnly = false;
            JitterInSeconds = jitterInSeconds;
        }
    }
}
