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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.Common
{
    public abstract class TestTokenCacheStoreBase : IDictionary<TokenCacheKey, string>
    {
        public abstract ICollection<TokenCacheKey> Keys { get; }

        public abstract ICollection<string> Values { get; }

        public abstract void Add(TokenCacheKey key, string value);

        public abstract bool Remove(TokenCacheKey key);

        public abstract bool TryGetValue(TokenCacheKey key, out string cacheValue);

        public abstract IEnumerator<KeyValuePair<TokenCacheKey, string>> GetEnumerator();

        public virtual bool ContainsKey(TokenCacheKey key)
        {
            return (this.Keys.FirstOrDefault(k => k == key) != null);
        }

        public virtual string this[TokenCacheKey key]
        {
            get
            {
                string cacheValue;
                this.TryGetValue(key, out cacheValue);
                return cacheValue;
            }
            set
            {
                // Behavior of this[] is different from 'Add' as it should replace the existing value.
                this.Remove(key);
                this.Add(key, value);
            }
        }

        public virtual int Count
        {
            get { return this.Keys.Count; }
        }

        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        public virtual void Add(KeyValuePair<TokenCacheKey, string> item)
        {
            Add(item.Key, item.Value);
        }

        public virtual bool Contains(KeyValuePair<TokenCacheKey, string> item)
        {
            bool contains = ContainsKey(item.Key);
            if (contains)
            {
                contains = (string.Compare(item.Value, this[item.Key], StringComparison.OrdinalIgnoreCase) == 0);
            }

            return contains;
        }

        public virtual void CopyTo(KeyValuePair<TokenCacheKey, string>[] array, int arrayIndex)
        {
            ICollection<TokenCacheKey> keys = this.Keys;
            if (array.Length - arrayIndex < keys.Count)
                throw new ArgumentOutOfRangeException("array");

            int i = 0;
            foreach (TokenCacheKey key in keys)
            {
                array[arrayIndex + i] = new KeyValuePair<TokenCacheKey, string>(key, this[key]);
                i++;
            }
        }

        public virtual bool Remove(KeyValuePair<TokenCacheKey, string> item)
        {
            bool removed = false;
            if (Contains(item))
            {
                removed = Remove(item.Key);
            }

            return removed;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Keys.GetEnumerator();
        }

        public virtual void Clear()
        {
            foreach (var key in this.Keys)
            {
                this.Remove(key);
            }
        }
    }
}
