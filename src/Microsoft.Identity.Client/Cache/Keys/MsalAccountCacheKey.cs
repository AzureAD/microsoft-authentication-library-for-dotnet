//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using System;
using System.Text;

namespace Microsoft.Identity.Client.Cache.Keys
{
    /// <summary>
    /// An object representing the key of the token cache Account dictionary. The 
    /// format of the key is not important for this library, as long as it is unique.
    /// </summary>
    internal class MsalAccountCacheKey 
    {
        private readonly string _environment;
        private readonly string _homeAccountId;
        private readonly string _tenantId;
        private readonly string _username;

        public MsalAccountCacheKey(string environment, string tenantId, string userIdentifier, string username)
        {
            if (string.IsNullOrEmpty(environment))
            {
                throw new ArgumentNullException(nameof(environment));
            }

            _tenantId = tenantId;
            _environment = environment;
            _homeAccountId = userIdentifier;
            _username = username;
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(_homeAccountId + MsalCacheKeys.CacheKeyDelimiter);
            stringBuilder.Append(_environment + MsalCacheKeys.CacheKeyDelimiter);
            stringBuilder.Append(_tenantId);

            return stringBuilder.ToString();
        }

        #region iOS

        public string GetiOSAccountKey()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(_homeAccountId ?? "");
            stringBuilder.Append(MsalCacheKeys.CacheKeyDelimiter);

            stringBuilder.Append(_environment);

            return stringBuilder.ToString().ToLowerInvariant();
        }

        public string GetiOSServiceKey()
        {
            return (_tenantId ?? "").ToLowerInvariant();
        }

        public string GetiOSGenericKey()
        {
            return _username.ToLowerInvariant();
        }


        #endregion
    }
}
