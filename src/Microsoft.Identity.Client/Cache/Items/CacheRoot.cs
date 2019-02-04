// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Identity.Json;

namespace Microsoft.Identity.Client.Cache.Items
{
    [DataContract]
    internal class CacheRoot
    {
        [DataMember(Name = "access_tokens")]
        public Dictionary<string, MsalAccessTokenCacheItem> AccessTokens { get; set; } =
            new Dictionary<string, MsalAccessTokenCacheItem>();

        [DataMember(Name = "refresh_tokens")]
        public Dictionary<string, MsalRefreshTokenCacheItem> RefreshTokens { get; set; } =
            new Dictionary<string, MsalRefreshTokenCacheItem>();

        [DataMember(Name = "id_tokens")]
        public Dictionary<string, MsalIdTokenCacheItem> IdTokens { get; set; } = new Dictionary<string, MsalIdTokenCacheItem>();

        [DataMember(Name = "accounts")]
        public Dictionary<string, MsalAccountCacheItem> Accounts { get; set; } = new Dictionary<string, MsalAccountCacheItem>();

        internal static CacheRoot FromJsonString(string json)
        {
            // TODO: need to load this as a JObject and walk to each value to call AccessToken.FromJObject() so
            // we can properly handle the additional_fields for forward/backward compat.
            return JsonConvert.DeserializeObject<CacheRoot>(json);
        }

        internal string ToJsonString()
        {
            // TODO: need to handle "additional fields json" for forward/back compat.
            return JsonConvert.SerializeObject(this);
        }
    }
}