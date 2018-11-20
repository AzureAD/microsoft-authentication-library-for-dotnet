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
using System.Collections.Concurrent;

namespace Microsoft.Identity.Core.Instance
{
    internal class ValidatedAuthoritiesCache : IValidatedAuthoritiesCache
    {
        // TODO: The goal of creating this class was to remove statics, but for the time being
        // we don't have a good separation to cache these across ClientApplication instances
        // in the case where a ConfidentialClientApplication is created per-request, for example.
        // So moving this back to static to keep the existing behavior but the rest of the code
        // won't know this is static.
        private static readonly ConcurrentDictionary<string, Authority> _validatedAuthorities =
            new ConcurrentDictionary<string, Authority>();

        public ValidatedAuthoritiesCache(bool shouldClearCache = true)
        {
            if (shouldClearCache)
            {
                _validatedAuthorities.Clear();
            }
        }

        public int Count => _validatedAuthorities.Count;

        public bool ContainsKey(string key)
        {
            return _validatedAuthorities.ContainsKey(key);
        }

        public bool TryAddValue(string key, Authority authority)
        {
            return _validatedAuthorities.TryAdd(key, authority);
        }

        public bool TryGetValue(string key, out Authority authority)
        {
            return _validatedAuthorities.TryGetValue(key, out authority);
        }
    }
}