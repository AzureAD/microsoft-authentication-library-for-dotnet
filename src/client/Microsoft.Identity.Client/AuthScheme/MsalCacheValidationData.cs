// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.AuthScheme
{
    /// <summary>
    /// Data used to validate cache items for different authentication schemes.
    /// </summary>
    public class MsalCacheValidationData
    {
        /// <summary>
        /// Gets the persisted parameters addded to the cache items.
        /// </summary>
        public IDictionary<string, string> PersistedCacheParameters { get; internal set; }
    }
}
