// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Identity.Client.Cache
{
    /// <summary>
    /// Contains the results of an ADAL token acquisition. Access Tokens from ADAL are not compatible 
    /// with MSAL, only Refresh Tokens are.
    /// </summary>
    [Preserve(AllMembers = true)]
    internal sealed class AdalResult
    {
        public AdalResult()
        {
            // for serialization
        }

        /// <summary>
        /// Gets user information including user Id. Some elements in UserInfo might be null if not returned by the service.
        /// </summary>
        [JsonInclude]
        public AdalUserInfo UserInfo { get; internal set; }
    }
}
