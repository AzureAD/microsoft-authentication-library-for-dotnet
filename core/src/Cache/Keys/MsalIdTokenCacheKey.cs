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

namespace Microsoft.Identity.Core.Cache
{
    /// <summary>
    /// An object representing the key of the token cache Id Token dictionary. The 
    /// format of the key is not important for this library, as long as it is unique.
    /// </summary>
    internal class MsalIdTokenCacheKey
    {
        private string _environment;
        private string _homeAccountId;
        private string _clientId;
        private string _tenantId;

        public MsalIdTokenCacheKey(
            string environment,
            string tenantId,
            string userIdentifier,
            string clientId)

        {
            if (string.IsNullOrEmpty(environment))
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            _environment = environment;
            _homeAccountId = userIdentifier;
            _clientId = clientId;
            _tenantId = tenantId;
        }


        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(_homeAccountId ?? "");
            stringBuilder.Append(MsalCacheConstants.CacheKeyDelimiter);

            stringBuilder.Append(_environment);
            stringBuilder.Append(MsalCacheConstants.CacheKeyDelimiter);

            stringBuilder.Append(MsalCacheConstants.IdToken);
            stringBuilder.Append(MsalCacheConstants.CacheKeyDelimiter);

            stringBuilder.Append(_clientId);
            stringBuilder.Append(MsalCacheConstants.CacheKeyDelimiter);

            stringBuilder.Append(_tenantId ?? "");
            stringBuilder.Append(MsalCacheConstants.CacheKeyDelimiter);

            return stringBuilder.ToString();
        }

        #region iOS

        public string GetiOSAccountKey()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(_homeAccountId ?? "");
            stringBuilder.Append(MsalCacheConstants.CacheKeyDelimiter);

            stringBuilder.Append(_environment);

            return stringBuilder.ToString();
        }

        public string GetiOSServiceKey()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(MsalCacheConstants.IdToken);
            stringBuilder.Append(MsalCacheConstants.CacheKeyDelimiter);

            stringBuilder.Append(_clientId);
            stringBuilder.Append(MsalCacheConstants.CacheKeyDelimiter);

            stringBuilder.Append(_tenantId ?? "");
            stringBuilder.Append(MsalCacheConstants.CacheKeyDelimiter);

            return stringBuilder.ToString();
        }

        public string GetiOSGenericKey()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(MsalCacheConstants.IdToken);
            stringBuilder.Append(MsalCacheConstants.CacheKeyDelimiter);

            stringBuilder.Append(_clientId);
            stringBuilder.Append(MsalCacheConstants.CacheKeyDelimiter);

            stringBuilder.Append(_tenantId ?? "");

            return stringBuilder.ToString();
        }

        #endregion

    
    }
}

