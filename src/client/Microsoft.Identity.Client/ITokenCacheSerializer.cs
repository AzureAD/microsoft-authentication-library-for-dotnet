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
    /// The methods in this class are not thread safe. It is expected that they will be called from the token cache callbacks, 
    /// registered via SetBeforeAccess, SetAfterAccess. These callbacks thread safe because they are triggered sequentially.
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

        /// <summary>
        /// Serializes the token cache to the MSAL.NET 2.x unified cache format, which is compatible with ADAL.NET v4 and other MSAL.NET v2 applications.
        /// If you need to maintain SSO between an application using ADAL 3.x or MSAL 2.x and this application using MSAL 3.x,
        /// you might also want to serialize and deserialize with <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> or <see cref="SerializeMsalV2"/>/<see cref="DeserializeMsalV2"/>,
        /// otherwise just use <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        /// </summary>
        /// <returns>Byte stream representation of the cache</returns>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [Obsolete("Support for the MSAL v2 token cache format will be dropped in the next major version", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        byte[] SerializeMsalV2();

        /// <summary>
        /// Deserializes the token cache to the MSAL.NET 2.x cache format, which is compatible with ADAL.NET v4 and other MSAL.NET v2 applications.
        /// If you need to maintain SSO between an application using ADAL 3.x or MSAL 2.x and this application using MSAL 3.x,
        /// you might also want to serialize and deserialize with <see cref="SerializeAdalV3"/>/<see cref="DeserializeAdalV3"/> or <see cref="SerializeMsalV2"/>/<see cref="DeserializeMsalV2"/>,
        /// otherwise just use <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/>.
        /// </summary>
        /// <param name="msalV2State">Byte stream representation of the cache</param>
        /// <remarks>
        /// <see cref="SerializeMsalV3"/>/<see cref="DeserializeMsalV3"/> is compatible with other MSAL libraries such as MSAL for Python and MSAL for Java.
        /// </remarks>
        [Obsolete("Support for the MSAL v2 token cache format will be dropped in the next major version", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        void DeserializeMsalV2(byte[] msalV2State);
    }
}
