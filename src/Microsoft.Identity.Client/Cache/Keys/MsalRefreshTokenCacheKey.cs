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

namespace Microsoft.Identity.Client.Cache.Keys
{
    /// <summary>
    /// An object representing the key of the token cache RT dictionary. The 
    /// format of the key is not important for this library, as long as it is unique.
    /// </summary>
    /// <remarks>
    /// Normal RTs are scoped by env, account_id and clientID
    /// FRTs are scoped by env, account_id and familyID (clientID exists, but is irrelevant)
    /// </remarks>
    internal class MsalRefreshTokenCacheKey : IiOSKey //TODO bogavril: add a base class with FRT key?
    {
        private readonly string _environment;
        private readonly string _homeAccountId;
        private readonly string _clientId;
        private readonly string _familyId;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="clientId"></param>
        /// <param name="userIdentifier"></param>
        /// <param name="familyId">Can be null or empty, denoting a normal RT. A value signifies an FRT.</param>
        internal MsalRefreshTokenCacheKey(string environment, string clientId, string userIdentifier, string familyId)
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
            _familyId = familyId;
        }

        public override string ToString()
        {
            // FRT
            if (!String.IsNullOrWhiteSpace(_familyId))
            {
                string d = MsalCacheKeys.CacheKeyDelimiter;
                return $"{_homeAccountId}{d}{_environment}{d}{StorageJsonValues.CredentialTypeRefreshToken}{d}{_familyId}{d}{d}".ToLowerInvariant();
               
            }

            return MsalCacheKeys.GetCredentialKey(
                   _homeAccountId,
                   _environment,
                   StorageJsonValues.CredentialTypeRefreshToken,
                   _clientId,
                   tenantId: null,
                   scopes: null);
        }

        #region iOS

        public string iOSAccount
        {
            get
            {
                return MsalCacheKeys.GetiOSAccountKey(_homeAccountId, _environment);
            }
        }

        public string iOSGeneric
        {
            get
            {
                // FRT
                if (!String.IsNullOrWhiteSpace(_familyId))
                {
                    return $"{StorageJsonValues.CredentialTypeRefreshToken}{MsalCacheKeys.CacheKeyDelimiter}{_familyId}{MsalCacheKeys.CacheKeyDelimiter}".ToLowerInvariant();
                }

                return MsalCacheKeys.GetiOSGenericKey(StorageJsonValues.CredentialTypeRefreshToken, _clientId, tenantId: null);

            }
        }

        public string iOSService
        {
            get
            {
                // FRT
                if (!String.IsNullOrWhiteSpace(_familyId))
                {
                    return $"{StorageJsonValues.CredentialTypeRefreshToken}{MsalCacheKeys.CacheKeyDelimiter}{_familyId}{MsalCacheKeys.CacheKeyDelimiter}{MsalCacheKeys.CacheKeyDelimiter}".ToLowerInvariant();
                }

                return MsalCacheKeys.GetiOSServiceKey(StorageJsonValues.CredentialTypeRefreshToken, _clientId, tenantId: null, scopes: null);
            }
        }

        public int iOSType => (int)MsalCacheKeys.iOSCredentialAttrType.RefreshToken;



        #endregion
    }
}

