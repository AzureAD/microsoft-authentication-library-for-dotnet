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
    /// App metadata is an optional entity in cache and can be used by apps to store additional metadata applicable to a particular client.
    /// </summary>
    internal class MsalAppMetadataCacheKey : IiOSKey
    {        
        private readonly string _clientId; 
        private readonly string _environment; 

        public MsalAppMetadataCacheKey(string clientId, string environment)
        {
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Ex: appmetadata-login.microsoftonline.com-b6c69a37-df96-4db0-9088-2ab96e1d8215
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{StorageJsonValues.AppMetadata}{MsalCacheKeys.CacheKeyDelimiter}{_environment}{MsalCacheKeys.CacheKeyDelimiter}{_clientId}";
        }

        #region iOS

        public string iOSService => $"{StorageJsonValues.AppMetadata}{MsalCacheKeys.CacheKeyDelimiter}{_clientId}";

        public string iOSGeneric => "1";

        public string iOSAccount => $"{_environment}";

        public int iOSType => (int)MsalCacheKeys.iOSCredentialAttrType.AppMetadata;

        #endregion
    }
}
