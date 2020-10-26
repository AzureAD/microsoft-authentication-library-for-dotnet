// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Cache
{
    /// <summary>
    /// Data class, common to ADAL.NET and MSAL.NET V2 used for the token cache serialization
    /// in a dual format: the ADAL V3 cache format, and the new unified cache format, common
    /// to ADAL.NET 4.x, MSAL.NET 2.x and other libraries in the same Operating System
    /// (for instance ADAL and MSAL for objective C in iOS)
    /// </summary>
    [Obsolete("This is expected to be removed in MSAL.NET v3 and ADAL.NET v5. We recommend using SerializeMsalV3/DeserializeMsalV3. Read more: https://aka.ms/msal-net-3x-cache-breaking-change", false)]
    public class CacheData
    {
        /// <summary>
        /// Array of bytes containing the serialized cache in ADAL.NET V3 format
        /// </summary>
        public byte[] AdalV3State { get; set; }

        /// <summary>
        /// Array of bytes containing the serialized MSAL.NET V2 cache
        /// </summary>
        public byte[] UnifiedState { get; set; }
    }
}
