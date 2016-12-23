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
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.Storage;
using Microsoft.Identity.Client.Internal.Interfaces;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;

namespace Microsoft.Identity.Client
{
    internal class TokenCachePlugin : ITokenCachePlugin
    {
        private const string LocalSettingsTokenContainerName = "MicrosoftAuthenticationLibrary.Tokens";
        private const string LocalSettingsRefreshTokenContainerName = "MicrosoftAuthenticationLibrary.RefreshTokens";
        private ApplicationDataContainer _refreshTokenContainer = null;
        private ApplicationDataContainer _tokenContainer = null;

        public TokenCachePlugin()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            _tokenContainer =
                    localSettings.CreateContainer(LocalSettingsTokenContainerName, ApplicationDataCreateDisposition.Always);
            _refreshTokenContainer =
                    localSettings.CreateContainer(LocalSettingsRefreshTokenContainerName, ApplicationDataCreateDisposition.Always);
        }

        public ICollection<string> AllAccessAndIdTokens()
        {
            IList<string> list = new List<string>();
            foreach(var item in _tokenContainer.Values.Values)
            {
                byte[] decryptedEntry = CryptographyHelper.Decrypt((byte[]) item);
                list.Add(EncodingHelper.CreateString(decryptedEntry));
            }

            return list;
        }

        public ICollection<string> AllRefreshTokens()
        {
            IList<string> list = new List<string>();
            foreach (var item in _refreshTokenContainer.Values.Values)
            {
                byte[] decryptedEntry = CryptographyHelper.Decrypt((byte[])item);
                list.Add(EncodingHelper.CreateString(decryptedEntry));
            }

            return list;
        }

        public void SaveToken(TokenCacheItem tokenItem)
        {
            _tokenContainer.Values[tokenItem.GetTokenCacheKey().ToString()] =
                CryptographyHelper.Encrypt(JsonHelper.SerializeToJson(tokenItem));
        }

        public void SaveRefreshToken(RefreshTokenCacheItem refreshTokenItem)
        {
            _tokenContainer.Values[refreshTokenItem.GetTokenCacheKey().ToString()] =
                CryptographyHelper.Encrypt(JsonHelper.SerializeToJson(refreshTokenItem));
        }

        public void DeleteToken(TokenCacheKey key)
        {
            _tokenContainer.Values.Remove(key.ToString());
        }

        public void DeleteRefreshToken(TokenCacheKey key)
        {
            _refreshTokenContainer.Values.Remove(key.ToString());
        }
    }
}