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
using System.Collections.Generic;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.Common
{
    class ShortLivedTokenCache : TestTokenCacheStoreBase
    {
        public const int TokenLifetimeInSeconds = 10;

        private readonly Dictionary<TokenCacheKey, string> cache;

        public ShortLivedTokenCache() : base()
        {
            this.cache = new Dictionary<TokenCacheKey, string>();
        }

        public override ICollection<TokenCacheKey> Keys 
        {
            get { return this.cache.Keys; }
        }

        public override ICollection<string> Values
        {
            get { return this.cache.Values; }
        }

        public override IEnumerator<KeyValuePair<TokenCacheKey, string>> GetEnumerator()
        {
            return this.cache.GetEnumerator();
        }

        public override void Add(TokenCacheKey key, string value)
        {
            key.ExpiresOn = DateTime.UtcNow + TimeSpan.FromSeconds(TokenLifetimeInSeconds);
            this.cache.Add(key, value);
        }

        public override bool TryGetValue(TokenCacheKey key, out string value)
        {
            this.cache.TryGetValue(key, out value);

            return (value != null);
        }

        public override bool Remove(TokenCacheKey key)
        {
            return this.cache.Remove(key);
        }
    }
}
