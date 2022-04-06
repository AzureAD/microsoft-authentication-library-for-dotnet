// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Json;

namespace Microsoft.Identity.Client.Cache
{
    /// <summary>
    /// Contains the results of an ADAL token acquisition. Access Tokens from ADAL are not compatible 
    /// with MSAL, only Refresh Tokens are.
    /// </summary>
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal sealed class AdalResult
    {
        internal AdalResult() 
        { 
            // for serialization
        } 

        /// <summary>
        /// Gets user information including user Id. Some elements in UserInfo might be null if not returned by the service.
        /// </summary>
        [JsonProperty]
        public AdalUserInfo UserInfo { get; internal set; }
    }
}
