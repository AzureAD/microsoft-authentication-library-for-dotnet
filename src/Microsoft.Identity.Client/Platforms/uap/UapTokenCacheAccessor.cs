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

using System;
using System.Collections.Generic;
using Windows.Storage;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Platforms.uap
{
    /// <remarks>On UWP, the ApplicationDataCompositeValue storage has a size limitation for keys 
    /// of no more than 255 chars. As such, keys cannot contain arbitrately long strings, i.e. they cannot contain the scopes. 
    /// This means that on UWP the AT key is not guaranteed to be unique, although the chances of collisions 
    /// are astronomically small - <see cref="MsalAccessTokenCacheKey.GetUWPFixedSizeKey"/>
    /// </remarks>
    internal class UapTokenCacheAccessor : ITokenCacheAccessor
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

        private readonly ICryptographyManager _cryptographyManager;

        public UapTokenCacheAccessor(ICryptographyManager cryptographyManager)
        {
            _cryptographyManager = cryptographyManager;
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

        /// <inheritdoc />
        public int RefreshTokenCount => throw new NotImplementedException();

        /// <inheritdoc />
        public int AccessTokenCount => throw new NotImplementedException();

        /// <inheritdoc />
        public int AccountCount => throw new NotImplementedException();

        /// <inheritdoc />
        public int IdTokenCount => throw new NotImplementedException();

        /// <inheritdoc />
        public void ClearRefreshTokens()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void ClearAccessTokens()
        {
            throw new NotImplementedException();
        }

        public void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
            SetCacheValue(composite,  item.ToJsonString());
            var key = item.GetKey().GetUWPFixedSizeKey(_cryptographyManager);

            _accessTokenContainer.Values[/*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(cacheKey)*/key] = composite;
        }

        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
            SetCacheValue(composite, item.ToJsonString());
            _refreshTokenContainer.Values[/*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(cacheKey)*/item.GetKey().ToString()] = composite;
        }

        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
            SetCacheValue(composite,  item.ToJsonString());
            _idTokenContainer.Values[/*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(cacheKey)*/item.GetKey().ToString()] = composite;
        }

        public void SaveAccount(MsalAccountCacheItem item)
        {
            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
            SetCacheValue(composite,  item.ToJsonString());
            _accountContainer.Values[/*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(cacheKey)*/item.GetKey().ToString()] = composite;
        }

        public MsalAccessTokenCacheItem GetAccessToken(MsalAccessTokenCacheKey accessTokenKey)
        {
            var keyStr = accessTokenKey.GetUWPFixedSizeKey(_cryptographyManager);
            if (!_accessTokenContainer.Values.ContainsKey(/*encodedKey*/keyStr))
            {
                return null;
            }
            return MsalAccessTokenCacheItem.FromJsonString(CoreHelpers.ByteArrayToString(
                GetCacheValue((ApplicationDataCompositeValue)_accessTokenContainer.Values[
                    /*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(accessTokenKey)*/keyStr])));
        }

        public MsalRefreshTokenCacheItem GetRefreshToken(MsalRefreshTokenCacheKey refreshTokenKey)
        {
            var keyStr = refreshTokenKey.ToString();
            //var encodedKey = CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(refreshTokenKey);
            if (!_refreshTokenContainer.Values.ContainsKey(/*encodedKey*/keyStr))
            {
                return null;
            }
            return MsalRefreshTokenCacheItem.FromJsonString(CoreHelpers.ByteArrayToString(
                GetCacheValue((ApplicationDataCompositeValue)_refreshTokenContainer.Values[/*encodedKey*/keyStr])));
        }

        public MsalIdTokenCacheItem GetIdToken(MsalIdTokenCacheKey idTokenKey)
        {
            var keyStr = idTokenKey.ToString();
            if (!_idTokenContainer.Values.ContainsKey(/*encodedKey*/keyStr))
            {
                return null;
            }
            return MsalIdTokenCacheItem.FromJsonString(CoreHelpers.ByteArrayToString(
                GetCacheValue((ApplicationDataCompositeValue)_idTokenContainer.Values[
                    /*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(idTokenKey)*/keyStr])));
        }

        public MsalAccountCacheItem GetAccount(MsalAccountCacheKey accountKey)
        {
            var keyStr = accountKey.ToString();
            if (!_accountContainer.Values.ContainsKey(/*encodedKey*/keyStr))
            {
                return null;
            }

            return MsalAccountCacheItem.FromJsonString(CoreHelpers.ByteArrayToString(
                GetCacheValue((ApplicationDataCompositeValue)_accountContainer.Values[
                    /*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(accountKey)*/keyStr])));
        }

        public void DeleteAccessToken(MsalAccessTokenCacheKey cacheKey)
        {
            _accessTokenContainer.Values.Remove(/*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(cacheKey)*/cacheKey.GetUWPFixedSizeKey(_cryptographyManager));
        }

        public void DeleteRefreshToken(MsalRefreshTokenCacheKey cacheKey)
        {
            _refreshTokenContainer.Values.Remove(/*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(cacheKey)*/cacheKey.ToString());
        }

        public void DeleteIdToken(MsalIdTokenCacheKey cacheKey)
        {
            _idTokenContainer.Values.Remove(/*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(cacheKey)*/cacheKey.ToString());
        }

        public void DeleteAccount(MsalAccountCacheKey cacheKey)
        {
            _accountContainer.Values.Remove(/*CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(cacheKey)*/cacheKey.ToString());
        }

        public ICollection<MsalAccessTokenCacheItem> GetAllAccessTokens()
        {
            ICollection<MsalAccessTokenCacheItem> list = new List<MsalAccessTokenCacheItem>();
            foreach (ApplicationDataCompositeValue item in _accessTokenContainer.Values.Values)
            {
                list.Add(MsalAccessTokenCacheItem.FromJsonString(CoreHelpers.CreateString(GetCacheValue(item))));
            }

            return list;
        }

        public ICollection<MsalRefreshTokenCacheItem> GetAllRefreshTokens()
        {
            ICollection<MsalRefreshTokenCacheItem> list = new List<MsalRefreshTokenCacheItem>();
            foreach (ApplicationDataCompositeValue item in _refreshTokenContainer.Values.Values)
            {
                list.Add(MsalRefreshTokenCacheItem.FromJsonString(CoreHelpers.CreateString(GetCacheValue(item))));
            }

            return list;
        }

        public ICollection<MsalIdTokenCacheItem> GetAllIdTokens()
        {
            ICollection<MsalIdTokenCacheItem> list = new List<MsalIdTokenCacheItem>();
            foreach (ApplicationDataCompositeValue item in _idTokenContainer.Values.Values)
            {
                list.Add(MsalIdTokenCacheItem.FromJsonString(CoreHelpers.CreateString(GetCacheValue(item))));
            }

            return list;
        }

        public ICollection<MsalAccountCacheItem> GetAllAccounts()
        {
            ICollection<MsalAccountCacheItem> list = new List<MsalAccountCacheItem>();
            foreach (ApplicationDataCompositeValue item in _accountContainer.Values.Values)
            {
                list.Add(MsalAccountCacheItem.FromJsonString(CoreHelpers.CreateString(GetCacheValue(item))));
            }

            return list;
        }

        internal void SetCacheValue(ApplicationDataCompositeValue composite, string stringValue)
        {
            byte[] encryptedValue = _cryptographyManager.Encrypt(stringValue.ToByteArray());
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

        internal byte[] GetCacheValue(ApplicationDataCompositeValue composite)
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

            return _cryptographyManager.Decrypt(encryptedValue);
        }
     
        public void Clear()
        {
            _accessTokenContainer.Values.Clear();
            _refreshTokenContainer.Values.Clear();
            _idTokenContainer.Values.Clear();
            _accountContainer.Values.Clear();
        }
    }
}
