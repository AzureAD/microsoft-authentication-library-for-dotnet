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

using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Cache
{
    internal class TokenCacheJsonSerializer : ITokenCacheSerializer
    {
        private readonly ITokenCacheAccessor _accessor;

        public TokenCacheJsonSerializer(ITokenCacheAccessor accessor)
        {
            _accessor = accessor;
        }

        public byte[] Serialize()
        {
            var cache = new CacheSerializationContract();
            foreach (var t in _accessor.GetAllAccessTokens())
            {
                cache.AccessTokens[t.GetKey()
                                    .ToString()] = t;
            }

            foreach (var t in _accessor.GetAllRefreshTokens())
            {
                cache.RefreshTokens[t.GetKey()
                                     .ToString()] = t;
            }

            foreach (var t in _accessor.GetAllIdTokens())
            {
                cache.IdTokens[t.GetKey()
                                .ToString()] = t;
            }

            foreach (var t in _accessor.GetAllAccounts())
            {
                cache.Accounts[t.GetKey()
                                .ToString()] = t;
            }

            return cache.ToJsonString()
                        .ToByteArray();
        }

        public void Deserialize(byte[] bytes)
        {
            _accessor.Clear();

            var cache = CacheSerializationContract.FromJsonString(CoreHelpers.ByteArrayToString(bytes));

            if (cache.AccessTokens != null)
            {
                foreach (var atItem in cache.AccessTokens.Values)
                {
                    _accessor.SaveAccessToken(atItem);
                }
            }

            if (cache.RefreshTokens != null)
            {
                foreach (var rtItem in cache.RefreshTokens.Values)
                {
                    _accessor.SaveRefreshToken(rtItem);
                }
            }

            if (cache.IdTokens != null)
            {
                foreach (var idItem in cache.IdTokens.Values)
                {
                    _accessor.SaveIdToken(idItem);
                }
            }

            if (cache.Accounts != null)
            {
                foreach (var account in cache.Accounts.Values)
                {
                    _accessor.SaveAccount(account);
                }
            }
        }
    }
}