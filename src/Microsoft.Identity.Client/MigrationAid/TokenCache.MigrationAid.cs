// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Cache;

namespace Microsoft.Identity.Client
{
    public sealed partial class TokenCache : ITokenCacheInternal
    {
        /// <summary>
        /// Gets or sets the flag indicating whether the state of the cache has changed.
        /// MSAL methods set this flag after any change.
        /// Caller applications should reset the flag after serializing and persisting the state of the cache.
        /// </summary>
        [Obsolete("Please use the equivalent flag TokenCacheNotificationArgs.HasStateChanged, " +
        "which indicates if the operation triggering the notification is modifying the cache or not." +
        " Setting the flag is not required.")]
        public bool HasStateChanged
        {
            get => _hasStateChanged;
            set => _hasStateChanged = value;
        }

#if !ANDROID_BUILDTIME && !iOS_BUILDTIME

        /// <summary>
        /// Serializes the entire token cache in both the ADAL V3 and unified cache formats.
        /// </summary>
        /// <returns>Serialized token cache <see cref="CacheData"/></returns>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [Obsolete("This is expected to be removed in MSAL.NET v4 and ADAL.NET v6. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-3x-cache-breaking-change", false)]
        public CacheData SerializeUnifiedAndAdalCache()
        {
            GuardOnMobilePlatforms();
            // reads the underlying in-memory dictionary and dumps out the content as a JSON
            _semaphoreSlim.Wait();
            try
            {
                var serializedUnifiedCache = Serialize();
                var serializeAdalCache = LegacyCachePersistence.LoadCache();

                return new CacheData()
                {
                    AdalV3State = serializeAdalCache,
                    UnifiedState = serializedUnifiedCache
                };
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Deserializes the token cache from a serialization blob in both format (ADAL V3 format, and unified cache format)
        /// </summary>
        /// <param name="cacheData">Array of bytes containing serialize cache data</param>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [Obsolete("This is expected to be removed in MSAL.NET v4 and ADAL.NET v6. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-3x-cache-breaking-change", false)]
        public void DeserializeUnifiedAndAdalCache(CacheData cacheData)
        {
            GuardOnMobilePlatforms();
            _semaphoreSlim.Wait();
            try
            {
                Deserialize(cacheData.UnifiedState);
                LegacyCachePersistence.WriteCache(cacheData.AdalV3State);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Serializes using the <see cref="SerializeMsalV2"/> serializer.
        /// Obsolete: Please use specialized Serialization methods.
        /// <see cref="SerializeMsalV2"/> replaces <see cref="Serialize"/>.
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> Is our recommended way of serializing/deserializing.
        /// <see cref="SerializeAdalV3"/> For interoperability with ADAL.NET v3.
        /// </summary>
        /// <returns>array of bytes, <see cref="SerializeMsalV2"/></returns>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [Obsolete("This is expected to be removed in MSAL.NET v4 and ADAL.NET v6. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-3x-cache-breaking-change", false)]
        public byte[] Serialize()
        {
            return SerializeMsalV2();
        }

        /// <summary>
        /// Deserializes the token cache from a serialization blob in the unified cache format
        /// Obsolete: Please use specialized Deserialization methods.
        /// <see cref="DeserializeMsalV2"/> replaces <see cref="Deserialize"/>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> Is our recommended way of serializing/deserializing.
        /// <see cref="DeserializeAdalV3"/> For interoperability with ADAL.NET v3
        /// </summary>
        /// <param name="msalV2State">Array of bytes containing serialized MSAL.NET V2 cache data</param>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// <paramref name="msalV2State"/>Is a Json blob containing access tokens, refresh tokens, id tokens and accounts information.
        /// </remarks>
        [Obsolete("This is expected to be removed in MSAL.NET v4 and ADAL.NET v6. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-3x-cache-breaking-change", false)]
        public void Deserialize(byte[] msalV2State)
        {
            DeserializeMsalV2(msalV2State);
        }

        /// <summary>
        /// Notification for certain token cache interactions during token acquisition. This delegate is
        /// used in particular to provide a custom token cache serialization
        /// </summary>
        /// <param name="args">Arguments related to the cache item impacted</param>
        [Obsolete("Use Microsoft.Identity.Client.TokenCacheCallback instead. See https://aka.msa/msal-net-3x-cache-breaking-change", true)]
        public delegate void TokenCacheNotification(TokenCacheNotificationArgs args);
#endif // !ANDROID_BUILDTIME && !iOS_BUILDTIME
    }
}
