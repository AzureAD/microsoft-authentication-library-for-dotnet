// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client
{
    public sealed partial class TokenCache : ITokenCacheInternal
    {
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME

        // Unkown token cache data support for forwards compatibility.
        private IDictionary<string, JToken> _unknownNodes;

        /// <summary>
        /// Serializes the token cache to the ADAL.NET 3.x cache format.
        /// If you need to maintain SSO between an application using ADAL 3.x or MSAL 2.x and this application using MSAL 3.x,
        /// you might also want to serialize and deserialize with <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> or <see cref="SerializeMsalV2"/>/<see cref="DeserializeMsalV2"/>,
        /// otherwise just use <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        /// </summary>
        /// <returns>array of bytes containing the serialized ADAL.NET V3 cache data</returns>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        public byte[] SerializeAdalV3()
        {
            GuardOnMobilePlatforms();

            _semaphoreSlim.Wait();
            try
            {
                return SerializeAdalV3NoLocks();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        internal byte[] SerializeAdalV3NoLocks()
        {
            return LegacyCachePersistence.LoadCache();
        }

        /// <summary>
        /// Deserializes the token cache to the ADAL.NET 3.x cache format.
        /// If you need to maintain SSO between an application using ADAL 3.x or MSAL 2.x and this application using MSAL 3.x,
        /// you might also want to serialize and deserialize with <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> or <see cref="SerializeMsalV2"/>/<see cref="DeserializeMsalV2"/>,
        /// otherwise just use <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        /// </summary>
        /// <param name="adalV3State">Array of bytes containing serialized Adal.NET V3 cache data</param>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        public void DeserializeAdalV3(byte[] adalV3State)
        {
            GuardOnMobilePlatforms();

            _semaphoreSlim.Wait();
            try
            {
                DeserializeAdalV3NoLocks(adalV3State);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        internal void DeserializeAdalV3NoLocks(byte[] adalV3State)
        {
            LegacyCachePersistence.WriteCache(adalV3State);
        }

        /// <summary>
        /// Serializes the token cache to the MSAL.NET 2.x unified cache format, which is compatible with ADAL.NET v4 and other MSAL.NET v2 applications.
        /// If you need to maintain SSO between an application using ADAL 3.x or MSAL 2.x and this application using MSAL 3.x,
        /// you might also want to serialize and deserialize with <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> or <see cref="SerializeMsalV2"/>/<see cref="DeserializeMsalV2"/>,
        /// otherwise just use <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        /// </summary>
        /// <returns>array of bytes containing the serialized MsalV2 cache</returns>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        public byte[] SerializeMsalV2()
        {
            GuardOnMobilePlatforms();
            // reads the underlying in-memory dictionary and dumps out the content as a JSON
            _semaphoreSlim.Wait();
            try
            {
                return SerializeMsalV2NoLocks();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        internal byte[] SerializeMsalV2NoLocks()
        {
            return new TokenCacheDictionarySerializer(_accessor).Serialize(_unknownNodes);
        }

        /// <summary>
        /// Deserializes the token cache to the MSAL.NET 2.x unified cache format, which is compatible with ADAL.NET v4 and other MSAL.NET v2 applications.
        /// If you need to maintain SSO between an application using ADAL 3.x or MSAL 2.x and this application using MSAL 3.x,
        /// you might also want to serialize and deserialize with <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> or <see cref="SerializeMsalV2"/>/<see cref="DeserializeMsalV2"/>,
        /// otherwise just use <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        /// </summary>
        /// <param name="msalV2State">Array of bytes containing serialized MsalV2 cache data</param>
        /// <remarks>
        /// <paramref name="msalV2State"/>Is a Json blob containing access tokens, refresh tokens, id tokens and accounts information.
        /// </remarks>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        public void DeserializeMsalV2(byte[] msalV2State)
        {
            GuardOnMobilePlatforms();

            if (msalV2State == null || msalV2State.Length == 0)
            {
                return;
            }

            _semaphoreSlim.Wait();
            try
            {
                DeserializeMsalV2NoLocks(msalV2State);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        internal void DeserializeMsalV2NoLocks(byte[] msalV2State)
        {
            _unknownNodes = new TokenCacheDictionarySerializer(_accessor).Deserialize(msalV2State, false);
        }

        /// <summary>
        /// Serializes the token cache, in the MSAL.NET V3 cache format.
        /// If you need to maintain SSO between an application using ADAL 3.x or MSAL 2.x and this application using MSAL 3.x,
        /// you might also want to serialize and deserialize with <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> or <see cref="SerializeMsalV2"/>/<see cref="DeserializeMsalV2"/>,
        /// otherwise just use <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        /// </summary>
        /// <returns>Byte stream representation of the cache</returns>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        public byte[] SerializeMsalV3()
        {
            GuardOnMobilePlatforms();

            _semaphoreSlimTC.Wait();
            try
            {
                return SerializeMsalV3NoLocks();
            }
            finally
            {
                _semaphoreSlimTC.Release();
            }
        }

        internal byte[] SerializeMsalV3NoLocks()
        {
            return new TokenCacheJsonSerializer(_accessor).Serialize(_unknownNodes);
        }

        /// <summary>
        /// De-serializes from the MSAL.NET V3 cache format.
        /// If you need to maintain SSO between an application using ADAL 3.x or MSAL 2.x and this application using MSAL 3.x,
        /// you might also want to serialize and deserialize with <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> or <see cref="SerializeMsalV2"/>/<see cref="DeserializeMsalV2"/>,
        /// otherwise just use <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        /// </summary>
        /// <param name="msalV3State">Byte stream representation of the cache</param>
        /// <param name="shouldClearExistingCache">Set to true to clear MSAL cache contents.  Defaults to false.</param>
        /// <remarks>
        /// This format is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        public void DeserializeMsalV3(byte[] msalV3State, bool shouldClearExistingCache = false)
        {
            GuardOnMobilePlatforms();

            _semaphoreSlimTC.Wait();
            try
            {
                DeserializeMsalV3NoLocks(msalV3State, shouldClearExistingCache);
            }
            finally
            {
                _semaphoreSlimTC.Release();
            }
        }

        internal void DeserializeMsalV3NoLocks(byte[] msalV3State, bool shouldClearExistingCache)
        {
            if (msalV3State == null || msalV3State.Length == 0)
            {
                return;
            }
            _unknownNodes = new TokenCacheJsonSerializer(_accessor).Deserialize(msalV3State, shouldClearExistingCache);
        }

        private static void GuardOnMobilePlatforms()
        {
#if ANDROID || iOS
        throw new PlatformNotSupportedException("You should not use these TokenCache methods object on mobile platforms. " +
            "They meant to allow applications to define their own storage strategy on .net desktop and non-mobile platforms such as .net core. " +
            "On mobile platforms, a secure and performant storage mechanism is implemeted by MSAL. " +
            "For more details about custom token cache serialization, visit https://aka.ms/msal-net-serialization");
#endif
        }

#endif // !ANDROID_BUILDTIME && !iOS_BUILDTIME
    }
}
