// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Microsoft.Identity.Client.Cache;

namespace Microsoft.Identity.Client
{
    public partial interface ITokenCache
    {
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME
        /// <summary>
        /// Functionality replaced by <see cref="ITokenCacheSerializer.SerializeMsalV3"/> and is accessible in TokenCacheNotificationArgs.
        /// </summary>
        /// <returns>Byte stream representation of the cache</returns>
        /// <remarks>
        /// This is the recommended format for maintaining SSO state between applications.
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use ITokenCacheSerializer.SerializeMsalV3 on the TokenCacheNotificationArgs in the cache callback. Read more: https://aka.ms/msal-net-4x-cache-breaking-change", false)]
        byte[] SerializeMsalV3();

        /// <summary>
        /// Functionality replaced by <see cref="ITokenCacheSerializer.DeserializeMsalV3"/> and is accessible in TokenCacheNotificationArgs.
        /// </summary>
        /// <param name="msalV3State">Byte stream representation of the cache</param>
        /// <param name="shouldClearExistingCache">
        /// Set to true to clear MSAL cache contents.  Defaults to false.
        /// You would want to set this to true if you want the cache contents in memory to be exactly what's on disk.
        /// You would want to set this to false if you want to merge the contents of what's on disk with your current in memory state.
        /// </param>
        /// <remarks>
        /// This is the recommended format for maintaining SSO state between applications.
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use ITokenCacheSerializer.DeserializeMsalV3 on the TokenCacheNotificationArgs in the cache callback. Read more: https://aka.ms/msal-net-4x-cache-breaking-change", false)]
        void DeserializeMsalV3(byte[] msalV3State, bool shouldClearExistingCache = false);

        /// <summary>
        /// Functionality replaced by <see cref="ITokenCacheSerializer.SerializeMsalV2"/> and is accessible in TokenCacheNotificationArgs.
        /// </summary>
        /// <returns>Byte stream representation of the cache</returns>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use ITokenCacheSerializer.SerializeMsalV2 on the TokenCacheNotificationArgs in the cache callback. Read more: https://aka.ms/msal-net-4x-cache-breaking-change", false)]
        byte[] SerializeMsalV2();

        /// <summary>
        /// Functionality replaced by <see cref="ITokenCacheSerializer.DeserializeMsalV2"/> and is accessible in TokenCacheNotificationArgs.
        /// </summary>
        /// <param name="msalV2State">Byte stream representation of the cache</param>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use ITokenCacheSerializer.DeserializeMsalV2 on the TokenCacheNotificationArgs in the cache callback. Read more: https://aka.ms/msal-net-4x-cache-breaking-change", false)]
        void DeserializeMsalV2(byte[] msalV2State);

        /// <summary>
        /// Functionality replaced by <see cref="ITokenCacheSerializer.SerializeAdalV3"/> and is accessible in TokenCacheNotificationArgs.
        /// </summary>
        /// <returns>Byte stream representation of the cache</returns>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use ITokenCacheSerializer.SerializeAdalV3 on the TokenCacheNotificationArgs in the cache callback. Read more: https://aka.ms/msal-net-4x-cache-breaking-change", false)]
        byte[] SerializeAdalV3();

        /// <summary>
        /// Functionality replaced by <see cref="ITokenCacheSerializer.DeserializeAdalV3"/> and is accessible in TokenCacheNotificationArgs.
        /// See https://aka.ms/msal-net-4x-cache-breaking-change
        /// </summary>
        /// <param name="adalV3State">Byte stream representation of the cache</param>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use ITokenCacheSerializer.DeserializeAdalV3 on the TokenCacheNotificationArgs in the cache callback. Read more: https://aka.ms/msal-net-4x-cache-breaking-change", false)]
        void DeserializeAdalV3(byte[] adalV3State);

        /// <summary>
        /// Functionality replaced by <see cref="ITokenCacheSerializer.SerializeMsalV2"/>. See https://aka.ms/msal-net-4x-cache-breaking-change
        /// /// </summary>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is expected to be removed in MSAL.NET v5. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-4x-cache-breaking-change", false)]
        byte[] Serialize();

        /// <summary>
        /// Functionality replaced by <see cref="ITokenCacheSerializer.DeserializeMsalV2"/>.  See https://aka.ms/msal-net-4x-cache-breaking-change        /// </summary>
        /// <param name="msalV2State"></param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is expected to be removed in MSAL.NET v5. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-4x-cache-breaking-change", false)]
        void Deserialize(byte[] msalV2State);

        /// <summary>
        /// Functionality replaced by <see cref="ITokenCacheSerializer.SerializeMsalV2"/> and <see cref="ITokenCacheSerializer.SerializeAdalV3"/>
        /// See https://aka.ms/msal-net-4x-cache-breaking-change
        /// </summary>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is expected to be removed in MSAL.NET v5. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-4x-cache-breaking-change", false)]
        CacheData SerializeUnifiedAndAdalCache();

        /// <summary>
        /// Functionality replaced by <see cref="ITokenCacheSerializer.DeserializeMsalV2"/> and <see cref="ITokenCacheSerializer.DeserializeAdalV3"/>
        /// See https://aka.ms/msal-net-4x-cache-breaking-change
        /// </summary>
        /// <param name="cacheData"></param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is expected to be removed in MSAL.NET v5. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-4x-cache-breaking-change", false)]
        void DeserializeUnifiedAndAdalCache(CacheData cacheData);
#endif
    }

    public sealed partial class TokenCache : ITokenCacheInternal
    {
        /// <summary>
        /// Gets or sets the flag indicating whether the state of the cache has changed.
        /// MSAL methods set this flag after any change.
        /// Caller applications should reset the flag after serializing and persisting the state of the cache.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
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
        /// <see cref="ITokenCacheSerializer.SerializeMsalV3"/>/<see cref="ITokenCacheSerializer.DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is expected to be removed in MSAL.NET v5. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-3x-cache-breaking-change", false)]
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
        /// <see cref="ITokenCacheSerializer.SerializeMsalV3"/>/<see cref="ITokenCacheSerializer.DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is expected to be removed in MSAL.NET v5. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-3x-cache-breaking-change", false)]
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
        /// Serializes using the <see cref="ITokenCacheSerializer.SerializeMsalV2"/> serializer.
        /// Obsolete: Please use specialized Serialization methods.
        /// <see cref="ITokenCacheSerializer.SerializeMsalV2"/> replaces <see cref="Serialize"/>.
        /// <see cref="ITokenCacheSerializer.SerializeMsalV3"/>/<see cref="ITokenCacheSerializer.DeserializeMsalV3"/> Is our recommended way of serializing/deserializing.
        /// <see cref="ITokenCacheSerializer.SerializeAdalV3"/> For interoperability with ADAL.NET v3.
        /// </summary>
        /// <returns>array of bytes, <see cref="ITokenCacheSerializer.SerializeMsalV2"/></returns>
        /// <remarks>
        /// <see cref="ITokenCacheSerializer.SerializeMsalV3"/>/<see cref="ITokenCacheSerializer.DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is expected to be removed in MSAL.NET v5. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-3x-cache-breaking-change", false)]
        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deserializes the token cache from a serialization blob in the unified cache format
        /// Obsolete: Please use specialized Deserialization methods.
        /// <see cref="ITokenCacheSerializer.DeserializeMsalV2"/> replaces <see cref="Deserialize"/>
        /// <see cref="ITokenCacheSerializer.SerializeMsalV3"/>/<see cref="ITokenCacheSerializer.DeserializeMsalV3"/> Is our recommended way of serializing/deserializing.
        /// <see cref="ITokenCacheSerializer.DeserializeAdalV3"/> For interoperability with ADAL.NET v3
        /// </summary>
        /// <param name="msalV2State">Array of bytes containing serialized MSAL.NET V2 cache data</param>
        /// <remarks>
        /// <see cref="ITokenCacheSerializer.SerializeMsalV3"/>/<see cref="ITokenCacheSerializer.DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// <paramref name="msalV2State"/>Is a Json blob containing access tokens, refresh tokens, id tokens and accounts information.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is expected to be removed in MSAL.NET v5. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-3x-cache-breaking-change", false)]
        public void Deserialize(byte[] msalV2State)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Notification for certain token cache interactions during token acquisition. This delegate is
        /// used in particular to provide a custom token cache serialization
        /// </summary>
        /// <param name="args">Arguments related to the cache item impacted</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use Microsoft.Identity.Client.TokenCacheCallback instead. See https://aka.msa/msal-net-3x-cache-breaking-change", true)]
        public delegate void TokenCacheNotification(TokenCacheNotificationArgs args);

        /// <summary>
        /// This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change", false)]
        public byte[] SerializeAdalV3()
        {
            throw new NotImplementedException("This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change");
        }

        /// <summary>
        /// This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change", false)]
        public void DeserializeAdalV3(byte[] adalV3State)
        {
            throw new NotImplementedException("This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change");
        }

        /// <summary>
        /// This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change", false)]
        public byte[] SerializeMsalV2()
        {
            throw new NotImplementedException("This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change");
        }

        /// <summary>
        /// This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change", false)]
        public void DeserializeMsalV2(byte[] msalV2State)
        {
            throw new NotImplementedException("This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change");
        }

        /// <summary>
        /// This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change", false)]
        public byte[] SerializeMsalV3()
        {
            throw new NotImplementedException("This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change");
        }

        /// <summary>
        /// This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change", false)]
        public void DeserializeMsalV3(byte[] msalV3State, bool shouldClearExistingCache)
        {
            throw new NotImplementedException("This is removed in MSAL.NET v4. Read more: https://aka.ms/msal-net-4x-cache-breaking-change");
        }

#endif // !ANDROID_BUILDTIME && !iOS_BUILDTIME
    }
}
