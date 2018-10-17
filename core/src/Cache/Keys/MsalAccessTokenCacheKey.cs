//------------------------------------------------------------------------------
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
    /// An object representing the key of the token cache AT dictionary. The 
    /// format of the key is not important for this library, as long as it is unique.
    /// </summary>
    /// <remarks>The format of the key is platform dependent</remarks>
    internal class MsalAccessTokenCacheKey
    {
        private readonly string _environment;
        private readonly string _homeAccountId;
        private readonly string _clientId;
        private readonly string _normalizedScopes; // space separated, lowercase and ordered alphabetically
        private readonly string _tenantId;

        internal MsalAccessTokenCacheKey(
            string environment,
            string tenantId,
            string userIdentifier,
            string clientId,
            string scopes)
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
           _normalizedScopes = scopes;
           _tenantId = tenantId;
        }


        public override string ToString()
        {
            return MsalCacheCommon.GetCredentialKey(
                _homeAccountId,
                _environment,
                MsalCacheCommon.AccessToken,
                _clientId,
                _tenantId,
                _normalizedScopes);          
        }

        #region UWP

        /// <summary>
        /// Gets a key that is smaller than 255 characters, which is a limitation for 
        /// UWP storage. This is done by hashing the scopes and env.
        /// </summary>
        /// <remarks>
        /// accountId - two guids plus separator - 73 chars        
        /// "accesstoken" string - 11 chars
        /// env - ussually loging.microsoft.net - 20 chars
        /// clientid - a guid - 36 chars
        /// tenantid - a guid - 36 chars
        /// scopes - a sha256 string - 44 chars
        /// delimiters - 4 chars
        /// total: 224 chars
        /// </remarks>
        public string GetUWPFixedSizeKey()
        {
            var crypto = PlatformProxyFactory.GetPlatformProxy().CryptographyManager;
            return MsalCacheCommon.GetCredentialKey(
              _homeAccountId,
              _environment,
              MsalCacheCommon.AccessToken,
              _clientId,
              _tenantId,
              crypto.CreateSha256Hash(_normalizedScopes)); // can't use scopes and env because they are of variable length
        }
        #endregion


        #region iOS

        public string GetiOSAccountKey()
        {
            return MsalCacheCommon.GetiOSAccountKey(_homeAccountId, _environment);
        }

        public string GetiOSServiceKey()
        {
            return MsalCacheCommon.GetiOSServiceKey(MsalCacheCommon.AccessToken, _clientId, _tenantId, _normalizedScopes);
        }

        public string GetiOSGenericKey()
        {
            return MsalCacheCommon.GetiOSGenericKey(MsalCacheCommon.AccessToken, _clientId, _tenantId);           
        }

        #endregion
    }
}