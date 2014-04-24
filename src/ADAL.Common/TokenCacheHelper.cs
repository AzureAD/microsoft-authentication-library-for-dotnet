//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// ID of the elements which can be used in creating token cache key. Depending on the AcquireToken method used, different elements might be contained in a token cache key
    /// </summary>
    internal enum TokenCacheKeyElement
    {
        /// <summary>
        /// Authority uri
        /// </summary>
        Authority = 0,

        /// <summary>
        /// Resource id
        /// </summary>
        Resource = 1,

        /// <summary>
        /// Client id
        /// </summary>
        ClientId = 2,

        /// <summary>
        /// Hash created from credential passed to acquire token
        /// </summary>
        CredentialHash = 3,

        /// <summary>
        /// Name of the identity provider
        /// </summary>
        IdentityProviderName = 4,

        /// <summary>
        /// User (in interactive flow, user can be passed as login_hint parameter)
        /// </summary>
        User = 5
    }

    /// <summary>
    /// Lists the elements which were used in creating token cache key. Depending on the AcquireToken method used, some properties may be null which means they were not included in the cache key.
    /// </summary>
    internal sealed class TokenCacheKeyElements
    {
        /// <summary>
        /// Gets or sets Authority
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Gets or sets Resource
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// Gets or sets Client Id
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets hash created from credential passed to acquire token
        /// </summary>
        public string CredentialHash { get; set; }

        /// <summary>
        /// Gets or sets name of the identity provider
        /// </summary>
        public string IdentityProviderName { get; set; }

        /// <summary>
        /// Gets or sets user (in interactive flow, user can be passed as login_hint parameter)
        /// </summary>
        public string User { get; set; }
    }

    /// <summary>
    /// Helper class to perform decoding and encoding operations on token cache keys and values
    /// </summary>
    internal static class TokenCacheEncoding
    {
        /// <summary>
        /// Decodes token cache value in form of authentication result which is the output of AcquireToken
        /// </summary>
        /// <param name="cacheValue">Token cache value</param>
        /// <returns>Authentication result decoded from token cache value. Null if input is null.</returns>
        public static AuthenticationResult DecodeCacheValue(string cacheValue)
        {
            AuthenticationResult result = null;

            if (!String.IsNullOrWhiteSpace(cacheValue))
            {
                result = AuthenticationResult.Deserialize(EncodingHelper.Base64Decode(cacheValue));
            }

            return result;
        }

        /// <summary>
        /// Encodes authentication result which is the output of AcquireToken in form of token cache value.
        /// </summary>
        /// <param name="result">Authentication result</param>
        /// <returns>Token cache value created by encoding authentication result</returns>
        public static string EncodeCacheValue(AuthenticationResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            return EncodingHelper.Base64Encode(result.Serialize());
        }
    }
}