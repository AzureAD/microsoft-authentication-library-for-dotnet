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

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Cache
{
    internal class TokenCacheDictionarySerializer : ITokenCacheSerializer
    {
        private const string AccessTokenKey = "access_tokens";
        private const string RefreshTokenKey = "refresh_tokens";
        private const string IdTokenKey = "id_tokens";
        private const string AccountKey = "accounts";
        private readonly ITokenCacheAccessor _accessor;

        public TokenCacheDictionarySerializer(ITokenCacheAccessor accessor)
        {
            _accessor = accessor;
        }

        public byte[] Serialize()
        {
            var accessTokensAsString = new List<string>();
            var refreshTokensAsString = new List<string>();
            var idTokensAsString = new List<string>();
            var accountsAsString = new List<string>();

            foreach (var accessToken in _accessor.GetAllAccessTokens())
            {
                accessTokensAsString.Add(accessToken.ToJsonString());
            }

            foreach (var refreshToken in _accessor.GetAllRefreshTokens())
            {
                refreshTokensAsString.Add(refreshToken.ToJsonString());
            }

            foreach (var idToken in _accessor.GetAllIdTokens())
            {
                idTokensAsString.Add(idToken.ToJsonString());
            }

            foreach (var account in _accessor.GetAllAccounts())
            {
                accountsAsString.Add(account.ToJsonString());
            }

            // reads the underlying in-memory dictionary and dumps out the content as a JSON
            var cacheDict = new Dictionary<string, IEnumerable<string>>
            {
                [AccessTokenKey] = accessTokensAsString,
                [RefreshTokenKey] = refreshTokensAsString,
                [IdTokenKey] = idTokensAsString,
                [AccountKey] = accountsAsString
            };

            return JsonHelper.SerializeToJson(cacheDict)
                             .ToByteArray();
        }

        public void Deserialize(byte[] bytes)
        {
            Dictionary<string, IEnumerable<string>> cacheDict;

            try
            {
                cacheDict = JsonHelper.DeserializeFromJson<Dictionary<string, IEnumerable<string>>>(bytes);
            }
            catch (Exception ex)
            {
                throw new MsalClientException(MsalError.JsonParseError, MsalErrorMessage.TokenCacheDictionarySerializerFailedParse, ex);
            }

            if (cacheDict == null || cacheDict.Count == 0)
            {
                return;
            }

            if (cacheDict.ContainsKey(AccessTokenKey))
            {
                foreach (var atItem in cacheDict[AccessTokenKey])
                {
                    _accessor.SaveAccessToken(MsalAccessTokenCacheItem.FromJsonString(atItem));
                }
            }

            if (cacheDict.ContainsKey(RefreshTokenKey))
            {
                foreach (var rtItem in cacheDict[RefreshTokenKey])
                {
                    _accessor.SaveRefreshToken(MsalRefreshTokenCacheItem.FromJsonString(rtItem));
                }
            }

            if (cacheDict.ContainsKey(IdTokenKey))
            {
                foreach (var idItem in cacheDict[IdTokenKey])
                {
                    _accessor.SaveIdToken(MsalIdTokenCacheItem.FromJsonString(idItem));
                }
            }

            if (cacheDict.ContainsKey(AccountKey))
            {
                foreach (var account in cacheDict[AccountKey])
                {
                    _accessor.SaveAccount(MsalAccountCacheItem.FromJsonString(account));
                }
            }
        }
    }
}
