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

using Microsoft.Identity.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;

namespace Microsoft.Identity.Core
{
    internal class TokenCacheAccessor : ITokenCacheAccessor
    {
        private const string CacheValue = "CacheValue";
        private const string CacheValueSegmentCount = "SegmentCount";
        private const string CacheValueLength = "Length";
        private const int MaxCompositeValueLength = 1024;
        private const string LocalSettingsTokenContainerName = "MicrosoftAuthenticationLibrary.AccessTokens";
        private const string LocalSettingsRefreshTokenContainerName = "MicrosoftAuthenticationLibrary.RefreshTokens";
        private const string LocalSettingsIdTokenContainerName = "MicrosoftAuthenticationLibrary.IdTokens";
        private const string LocalSettingsAccountContainerName = "MicrosoftAuthenticationLibrary.Accounts";

        private ApplicationDataContainer _refreshTokenContainer = null;
        private ApplicationDataContainer _accessTokenContainer = null;
        private ApplicationDataContainer _idTokenContainer = null;
        private ApplicationDataContainer _accountContainer = null;

        private RequestContext _requestContext;

        public TokenCacheAccessor()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            _accessTokenContainer =
                localSettings.CreateContainer(LocalSettingsTokenContainerName, ApplicationDataCreateDisposition.Always);
            _refreshTokenContainer =
                localSettings.CreateContainer(LocalSettingsRefreshTokenContainerName,
                    ApplicationDataCreateDisposition.Always);
            _idTokenContainer = 
                localSettings.CreateContainer(LocalSettingsIdTokenContainerName, 
                    ApplicationDataCreateDisposition.Always);
            _accountContainer =
                localSettings.CreateContainer(LocalSettingsAccountContainerName,
                    ApplicationDataCreateDisposition.Always);
        }
        public TokenCacheAccessor(RequestContext requestContext) : this()
        {
            _requestContext = requestContext;
        }

        public void SaveAccessToken(string cacheKey, string item)
        {
            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
            SetCacheValue(composite, item);
            _accessTokenContainer.Values[/*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(cacheKey)*/cacheKey] = composite;
        }

        public void SaveRefreshToken(string cacheKey, string item)
        {
            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
            SetCacheValue(composite, item);
            _refreshTokenContainer.Values[/*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(cacheKey)*/cacheKey] = composite;
        }

        public string GetRefreshToken(string refreshTokenKey)
        {
            //var encodedKey = CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(refreshTokenKey);
            if (!_refreshTokenContainer.Values.ContainsKey(/*encodedKey*/refreshTokenKey))
            {
                return null;
            }
            return CoreHelpers.ByteArrayToString(
                GetCacheValue((ApplicationDataCompositeValue) _refreshTokenContainer.Values[/*encodedKey*/refreshTokenKey]));
        }
        
        public void DeleteAccessToken(string cacheKey)
        {
            _accessTokenContainer.Values.Remove(/*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(cacheKey)*/cacheKey);
        }

        public void DeleteRefreshToken(string cacheKey)
        {
            _refreshTokenContainer.Values.Remove(/*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(cacheKey)*/cacheKey);
        }

        public void DeleteIdToken(string cacheKey)
        {
            _idTokenContainer.Values.Remove(/*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(cacheKey)*/cacheKey);
        }

        public void DeleteAccount(string cacheKey)
        {
            _accountContainer.Values.Remove(/*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(cacheKey)*/cacheKey);
        }

        public ICollection<string> GetAllAccessTokensAsString()
        {
            ICollection<string> list = new List<string>();
            foreach (ApplicationDataCompositeValue item in _accessTokenContainer.Values.Values)
            {
                list.Add(CoreHelpers.CreateString(GetCacheValue(item)));
            }

            return list;
        }

        public ICollection<string> GetAllRefreshTokensAsString()
        {
            ICollection<string> list = new List<string>();
            foreach (ApplicationDataCompositeValue item in _refreshTokenContainer.Values.Values)
            {
                list.Add(CoreHelpers.CreateString(GetCacheValue(item)));
            }

            return list;
        }

        public ICollection<string> GetAllIdTokensAsString()
        {
            ICollection<string> list = new List<string>();
            foreach (ApplicationDataCompositeValue item in _idTokenContainer.Values.Values)
            {
                list.Add(CoreHelpers.CreateString(GetCacheValue(item)));
            }

            return list;
        }

        public ICollection<string> GetAllAccountsAsString()
        {
            ICollection<string> list = new List<string>();
            foreach (ApplicationDataCompositeValue item in _accountContainer.Values.Values)
            {
                list.Add(CoreHelpers.CreateString(GetCacheValue(item)));
            }

            return list;
        }

        internal static void SetCacheValue(ApplicationDataCompositeValue composite, string stringValue)
        {
            byte[] encryptedValue = CoreCryptographyHelpers.Encrypt(stringValue.ToByteArray());
            composite[CacheValueLength] = encryptedValue.Length;

            int segmentCount = (encryptedValue.Length / MaxCompositeValueLength) +
                               ((encryptedValue.Length % MaxCompositeValueLength == 0) ? 0 : 1);
            byte[] subValue = new byte[MaxCompositeValueLength];
            for (int i = 0; i < segmentCount - 1; i++)
            {
                Array.Copy(encryptedValue, i * MaxCompositeValueLength, subValue, 0, MaxCompositeValueLength);
                composite[CacheValue + i] = subValue;
            }

            int copiedLength = (segmentCount - 1) * MaxCompositeValueLength;
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

            int encyptedValueLength = (int)composite[CacheValueLength];
            int segmentCount = (int)composite[CacheValueSegmentCount];

            byte[] encryptedValue = new byte[encyptedValueLength];
            if (segmentCount == 1)
            {
                encryptedValue = (byte[])composite[CacheValue + 0];
            }
            else
            {
                for (int i = 0; i < segmentCount - 1; i++)
                {
                    Array.Copy((byte[])composite[CacheValue + i], 0, encryptedValue, i * MaxCompositeValueLength,
                        MaxCompositeValueLength);
                }
            }

            Array.Copy((byte[])composite[CacheValue + (segmentCount - 1)], 0, encryptedValue,
                (segmentCount - 1) * MaxCompositeValueLength,
                encyptedValueLength - (segmentCount - 1) * MaxCompositeValueLength);

            return CoreCryptographyHelpers.Decrypt(encryptedValue);
        }

        public ICollection<string> GetAllAccessTokenKeys()
        {
            return new ReadOnlyCollection<string>(_accessTokenContainer.Values.Keys.ToList());
        }

        public ICollection<string> GetAllRefreshTokenKeys()
        {
            return new ReadOnlyCollection<string>(_refreshTokenContainer.Values.Keys.ToList());
        }

        public ICollection<string> GetAllIdTokenKeys()
        {
            return new ReadOnlyCollection<string>(_idTokenContainer.Values.Keys.ToList());
        }

        public ICollection<string> GetAllAccountKeys()
        {
            return new ReadOnlyCollection<string>(_accountContainer.Values.Keys.ToList());
        }

        public void Clear()
        {
            foreach (string atKey in _accessTokenContainer.Values.Keys)
            {
                _accessTokenContainer.Values.Remove(atKey);
            }
            foreach (string rtKey in _refreshTokenContainer.Values.Keys)
            {
                _refreshTokenContainer.Values.Remove(rtKey);
            }
            foreach (string idKey in _idTokenContainer.Values.Keys)
            {
                _idTokenContainer.Values.Remove(idKey);
            }
            foreach (string accountKey in _accountContainer.Values.Keys)
            {
                _accountContainer.Values.Remove(accountKey);
            }
        }

        public void SaveIdToken(string cacheKey, string item)
        {
            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
            SetCacheValue(composite, item);
            _idTokenContainer.Values[/*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(cacheKey)*/cacheKey] = composite;
        }

        public void SaveAccount(string cacheKey, string item)
        {
            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
            SetCacheValue(composite, item);
            _accountContainer.Values[/*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(cacheKey)*/cacheKey] = composite;
        }

        public string GetIdToken(string idTokenKey)
        {
            return CoreHelpers.ByteArrayToString(
                GetCacheValue((ApplicationDataCompositeValue)_idTokenContainer.Values[
                    /*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(idTokenKey)*/idTokenKey]));
        }

        public string GetAccessToken(string accessTokenKey)
        {
            return CoreHelpers.ByteArrayToString(
                GetCacheValue((ApplicationDataCompositeValue)_accessTokenContainer.Values[
                    /*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(accessTokenKey)*/accessTokenKey]));
        }

        public string GetAccount(string accountKey)
        {
            return CoreHelpers.ByteArrayToString(
                GetCacheValue((ApplicationDataCompositeValue)_accountContainer.Values[
                    /*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(accountKey)*/accountKey]));
        }
    }
}
