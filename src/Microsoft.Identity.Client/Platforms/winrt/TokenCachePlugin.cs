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
        private const string CacheValue = "CacheValue";
        private const string CacheValueSegmentCount = "SegmentCount";
        private const string CacheValueLength = "Length";
        private const int MaxCompositeValueLength = 1024;
        private const string LocalSettingsTokenContainerName = "MicrosoftAuthenticationLibrary.Tokens";
        private const string LocalSettingsRefreshTokenContainerName = "MicrosoftAuthenticationLibrary.RefreshTokens";
        private ApplicationDataContainer _refreshTokenContainer = null;
        private ApplicationDataContainer _tokenContainer = null;

        private readonly RequestContext _requestContext;

        public TokenCachePlugin(RequestContext requestContext)
        {
            _requestContext = requestContext;
        }

        public TokenCachePlugin()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            _tokenContainer =
                localSettings.CreateContainer(LocalSettingsTokenContainerName, ApplicationDataCreateDisposition.Always);
            _refreshTokenContainer =
                localSettings.CreateContainer(LocalSettingsRefreshTokenContainerName,
                    ApplicationDataCreateDisposition.Always);
        }

        public ICollection<string> GetAllAccessTokens()
        {
            IList<string> list = new List<string>();
            foreach (ApplicationDataCompositeValue item in _tokenContainer.Values.Values)
            {
                list.Add(MsalHelpers.CreateString(GetCacheValue(item)));
            }

            return list;
        }

        public ICollection<string> AllRefreshTokens()
        {
            IList<string> list = new List<string>();
            foreach (ApplicationDataCompositeValue item in _refreshTokenContainer.Values.Values)
            {
                list.Add(MsalHelpers.CreateString(GetCacheValue(item)));
            }

            return list;
        }

        public void SaveAccessToken(string cacheKey, string accessTokenItem)
        {
            CryptographyHelper helper = new CryptographyHelper();
            string hashed = helper.CreateSha256Hash(cacheKey);
            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
            SetCacheValue(composite, accessTokenItem);
            _tokenContainer.Values[hashed] = composite;
        }

        public void SaveRefreshToken(string cacheKey, string refreshTokenItem)
        {
            CryptographyHelper helper = new CryptographyHelper();
            string hashed = helper.CreateSha256Hash(cacheKey);
            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
            SetCacheValue(composite, refreshTokenItem);
            _refreshTokenContainer.Values[hashed] = composite;
        }

        public void DeleteAccessToken(string cacheKey)
        {
            CryptographyHelper helper = new CryptographyHelper();
            string hashed = helper.CreateSha256Hash(cacheKey);
            _tokenContainer.Values.Remove(hashed);
        }

        public void DeleteRefreshToken(string cacheKey)
        {
            CryptographyHelper helper = new CryptographyHelper();
            string hashed = helper.CreateSha256Hash(cacheKey);
            _refreshTokenContainer.Values.Remove(hashed);
        }

        internal static void SetCacheValue(ApplicationDataCompositeValue composite, string stringValue)
        {
            byte[] encryptedValue = CryptographyHelper.Encrypt(stringValue.ToByteArray());
            composite[CacheValueLength] = encryptedValue.Length;

            int segmentCount = (encryptedValue.Length/MaxCompositeValueLength) +
                               ((encryptedValue.Length%MaxCompositeValueLength == 0) ? 0 : 1);
            byte[] subValue = new byte[MaxCompositeValueLength];
            for (int i = 0; i < segmentCount - 1; i++)
            {
                Array.Copy(encryptedValue, i*MaxCompositeValueLength, subValue, 0, MaxCompositeValueLength);
                composite[CacheValue + i] = subValue;
            }

            int copiedLength = (segmentCount - 1)*MaxCompositeValueLength;
            Array.Copy(encryptedValue, copiedLength, subValue, 0, encryptedValue.Length - copiedLength);
            composite[CacheValue + (segmentCount - 1)] = subValue;
            composite[CacheValueSegmentCount] = segmentCount;
        }

        internal static byte[] GetCacheValue(ApplicationDataCompositeValue composite)
        {
            if (!composite.ContainsKey(CacheValueLength))
            {
                return null;
            }

            int encyptedValueLength = (int) composite[CacheValueLength];
            int segmentCount = (int) composite[CacheValueSegmentCount];

            byte[] encryptedValue = new byte[encyptedValueLength];
            if (segmentCount == 1)
            {
                encryptedValue = (byte[]) composite[CacheValue + 0];
            }
            else
            {
                for (int i = 0; i < segmentCount - 1; i++)
                {
                    Array.Copy((byte[]) composite[CacheValue + i], 0, encryptedValue, i*MaxCompositeValueLength,
                        MaxCompositeValueLength);
                }
            }

            Array.Copy((byte[]) composite[CacheValue + (segmentCount - 1)], 0, encryptedValue,
                (segmentCount - 1)*MaxCompositeValueLength,
                encyptedValueLength - (segmentCount - 1)*MaxCompositeValueLength);

            return CryptographyHelper.Decrypt(encryptedValue);
        }
    }
}