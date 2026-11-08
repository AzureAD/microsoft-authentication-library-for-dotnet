// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// This interface will be available in TokenCacheNotificationArgs callback to enable serialization/deserialization of the cache.
    /// </summary>
    /// <remarks>
    /// These methods are not thread safe. Call them from token cache notifications using
    /// <see cref="TokenCacheNotificationArgs.TokenCache"/>, rather than a captured cache instance.
    /// <para>
    /// Eligible synchronized confidential-client user-cache reads use operation-owned caches when legacy ADAL
    /// compatibility is disabled, an in-memory user accessor is used, serialization callbacks are registered,
    /// and the request has a known partition key before its first hydration. Different read operations may
    /// invoke async-only read callbacks concurrently. If either BeforeAccess or AfterAccess has a synchronous
    /// callback, MSAL keeps the entire read notification lifecycle sequential. Async providers must synchronize
    /// their own shared state and await all cache work, including publication, before returning.
    /// Writes and removals remain exclusive on the original cache instance.
    /// </para>
    /// <para>
    /// Every eligible BeforeAccess notification must completely hydrate its supplied cache using
    /// <see cref="DeserializeMsalV3"/>, or a structured snapshot restore. Each private hydration attempt starts
    /// with empty cache state, so a complete MSALv3 payload can use the default clear-existing flag.
    /// A null payload can represent an empty partition. Incremental, load-once, and no-op hydration are not
    /// supported for these isolated reads.
    /// </para>
    /// <para>
    /// Successful keyed reads publish independently owned decoded state to the original cache after both
    /// notifications succeed, preserving subsequent unkeyed account discovery. Clear-existing restores replace
    /// the original state; other complete MSALv3 restores merge it. Unkeyed reads keep their original notification
    /// target and pin independently owned state before releasing the original cache.
    /// </para>
    /// <para>
    /// To restore the previous read behavior, set the AppContext switch
    /// <c>Microsoft.Identity.Client.DisableCacheReadIsolation</c> to <c>true</c> before constructing the application.
    /// Existing applications must be recreated or the process restarted. The opt-out restores the previous
    /// coherence limitations. Other ineligible paths retain their existing behavior; disabling cache synchronization
    /// does not make callbacks thread safe.
    /// </para>
    /// </remarks>
    public interface ITokenCacheSerializer
    {
        /// <summary>
        /// Serializes the token cache to the MSAL.NET 3.x cache format, which is compatible with other MSAL desktop libraries, including MSAL.NET 4.x, MSAL for Python and MSAL for Java.
        /// If you need to maintain SSO between an application using ADAL 3.x and this application using MSAL 3.x or later,
        /// you might also want to serialize and deserialize with <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/>,
        /// otherwise just use <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        /// </summary>
        /// <returns>Byte stream representation of the cache</returns>
        /// <remarks>
        /// This is the recommended format for maintaining SSO state between applications.
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        byte[] SerializeMsalV3();

        /// <summary>
        /// Deserializes the token cache to the MSAL.NET 3.x cache format, which is compatible with other MSAL desktop libraries, including MSAL.NET 4.x, MSAL for Python and MSAL for Java.
        /// If you need to maintain SSO between an application using ADAL 3.x and this application using MSAL 3.x or later,
        /// you might also want to serialize and deserialize with <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/>,
        /// otherwise just use <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
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
        void DeserializeMsalV3(byte[] msalV3State, bool shouldClearExistingCache = false);

        /// <summary>
        /// Serializes a part of the token cache - the refresh tokens - to the ADAL.NET 3.x cache format. 
        /// If you need to maintain SSO between an application using ADAL 3.x and this application using MSAL 3.x or later,
        /// use <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> in addition to <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        ///
        /// See https://aka.ms/adal-to-msal-net/cache for details on how to use this advanced API correctly.
        /// </summary>
        /// <returns>Byte stream representation of the cache</returns>
        /// <remarks>
        /// Do not use <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> without also using <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>, 
        /// because only refresh tokens are exported in this format. Your applications will not cache access token and id tokens, 
        /// and will instead need to get them from the identity provider (AAD), which will eventually throttle you.
        /// Later versions of ADAL (4.x and 5.x) use the same cache format as MSAL.
        /// Only <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// ADAL to MSAL cache interop is only available for public client scenarios and for web site scenario. 
        /// </remarks>
        byte[] SerializeAdalV3();

        /// <summary>
        /// Deserializes a part of the token cache - the refresh tokens - to the ADAL.NET 3.x cache format.         
        /// This API should only be used to maintain SSO between an application using ADAL 3.x and this application using MSAL 3.x or later.
        /// Use <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> in addition to <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        /// 
        /// See https://aka.ms/adal-to-msal-net/cache for details on how to use this advanced API correctly.
        /// </summary>
        /// <param name="adalV3State">Byte stream representation of the cache</param>
        /// <remarks>
        /// Do not use <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> without also using <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>, 
        /// because only refresh tokens are exported in this format. Your applications will not cache access token and id tokens, 
        /// and will instead need to get them from the identity provider (AAD), which will eventually throttle you.
        /// Later versions of ADAL (4.x and 5.x) use the same cache format as MSAL.
        /// Only <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// ADAL to MSAL cache interop is only available for public client scenarios and for web site scenario. 
        /// </remarks>
        void DeserializeAdalV3(byte[] adalV3State);

    }
}
