// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Android.App;
using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Platforms.Android
{
    internal class AndroidTokenCacheAccessor : ITokenCacheAccessor
    {
        private const string AccessTokenSharedPreferenceName = "com.microsoft.identity.client.accessToken";
        private const string RefreshTokenSharedPreferenceName = "com.microsoft.identity.client.refreshToken";
        private const string IdTokenSharedPreferenceName = "com.microsoft.identity.client.idToken";
        private const string AccountSharedPreferenceName = "com.microsoft.identity.client.Account";

        private readonly ISharedPreferences _accessTokenSharedPreference;
        private readonly ISharedPreferences _refreshTokenSharedPreference;
        private readonly ISharedPreferences _idTokenSharedPreference;
        private readonly ISharedPreferences _accountSharedPreference;

        private readonly RequestContext _requestContext;

        public AndroidTokenCacheAccessor()
        {
            _accessTokenSharedPreference = Application.Context.GetSharedPreferences(AccessTokenSharedPreferenceName,
                    FileCreationMode.Private);
            _refreshTokenSharedPreference = Application.Context.GetSharedPreferences(RefreshTokenSharedPreferenceName,
                    FileCreationMode.Private);
            _idTokenSharedPreference = Application.Context.GetSharedPreferences(IdTokenSharedPreferenceName,
                    FileCreationMode.Private);
            _accountSharedPreference = Application.Context.GetSharedPreferences(AccountSharedPreferenceName,
                FileCreationMode.Private);

            if (_accessTokenSharedPreference == null ||
                _refreshTokenSharedPreference == null ||
                _idTokenSharedPreference == null ||
                _accountSharedPreference == null)
            {
                throw new MsalClientException(
                    MsalError.FailedToCreateSharedPreference,
                    "Fail to create SharedPreference");
            }
        }

        public AndroidTokenCacheAccessor(RequestContext requestContext) : this()
        {
            _requestContext = requestContext;
        }

        #region SaveItem
        public void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            ISharedPreferencesEditor editor = _accessTokenSharedPreference.Edit();
            editor.PutString(item.GetKey().ToString(), item.ToJsonString());
            editor.Apply();
        }

        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            ISharedPreferencesEditor editor = _refreshTokenSharedPreference.Edit();
            editor.PutString(item.GetKey().ToString(), item.ToJsonString());
            editor.Apply();
        }

        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            ISharedPreferencesEditor editor = _idTokenSharedPreference.Edit();
            editor.PutString(item.GetKey().ToString(), item.ToJsonString());
            editor.Apply();
        }

        public void SaveAccount(MsalAccountCacheItem item)
        {
            ISharedPreferencesEditor editor = _accountSharedPreference.Edit();
            editor.PutString(item.GetKey().ToString(), item.ToJsonString());
            editor.Apply();
        }
        #endregion

        #region DeleteItem
        public void DeleteAccessToken(MsalAccessTokenCacheItem item)
        {
            Delete(item.GetKey().ToString(), _accessTokenSharedPreference.Edit());
        }

        public void DeleteRefreshToken(MsalRefreshTokenCacheItem item)
        {
            Delete(item.GetKey().ToString(), _refreshTokenSharedPreference.Edit());
        }

        public void DeleteIdToken(MsalIdTokenCacheItem item)
        {
            Delete(item.GetKey().ToString(), _idTokenSharedPreference.Edit());
        }

        public void DeleteAccount(MsalAccountCacheItem item)
        {
            Delete(item.GetKey().ToString(), _accountSharedPreference.Edit());

        }

        private void Delete(string key, ISharedPreferencesEditor editor)
        {
            editor.Remove(key);
            editor.Apply();
        }

        private void DeleteAll(ISharedPreferences sharedPreferences)
        {
            var editor = sharedPreferences.Edit();

            editor.Clear();
            editor.Apply();
        }

        public void Clear()
        {
            DeleteAll(_accessTokenSharedPreference);
            DeleteAll(_refreshTokenSharedPreference);
            DeleteAll(_idTokenSharedPreference);
            DeleteAll(_accountSharedPreference);
        }

        #endregion

        #region GetItem
        public MsalIdTokenCacheItem GetIdToken(MsalAccessTokenCacheItem accessTokenCacheItem)
        {
            return MsalIdTokenCacheItem.FromJsonString(
                _idTokenSharedPreference.GetString(accessTokenCacheItem.GetIdTokenItemKey().ToString(), null));
        }
        #endregion

        #region GetAll
        public List<MsalAccessTokenCacheItem> GetAllAccessTokens(string optionalPartitionKey = null)
        {
            return _accessTokenSharedPreference.All.Values.Cast<string>().Select(x => MsalAccessTokenCacheItem.FromJsonString(x)).ToList();
        }

        public List<MsalRefreshTokenCacheItem> GetAllRefreshTokens(string optionalPartitionKey = null)
        {
            return _refreshTokenSharedPreference.All.Values.Cast<string>().Select(x => MsalRefreshTokenCacheItem.FromJsonString(x)).ToList();
        }

        public List<MsalIdTokenCacheItem> GetAllIdTokens(string optionalPartitionKey = null)
        {
            return _idTokenSharedPreference.All.Values.Cast<string>().Select(x => MsalIdTokenCacheItem.FromJsonString(x)).ToList();
        }

        public List<MsalAccountCacheItem> GetAllAccounts(string optionalPartitionKey = null)
        {
            return _accountSharedPreference.All.Values.Cast<string>().Select(x => MsalAccountCacheItem.FromJsonString(x)).ToList();
        }
        #endregion

        public MsalAccountCacheItem GetAccount(MsalAccountCacheKey accountKey)
        {
            return MsalAccountCacheItem.FromJsonString(_accountSharedPreference.GetString(accountKey.ToString(), null));
        }

        /// <summary>
        /// This method is used during token cache serialization which is not supported for Android.
        /// </summary>
        public bool HasAccessOrRefreshTokens()
        {
            throw new NotSupportedException();
        }

        #region App Metadata - not used on Android
        public MsalAppMetadataCacheItem ReadAppMetadata(MsalAppMetadataCacheKey appMetadataKey)
        {
            throw new NotImplementedException();
        }

        public void WriteAppMetadata(MsalAppMetadataCacheItem appMetadata)
        {
            throw new NotImplementedException();
        }

        public void SaveAppMetadata(MsalAppMetadataCacheItem item)
        {
            throw new NotImplementedException();
        }

        public List<MsalAppMetadataCacheItem> GetAllAppMetadata()
        {
            throw new NotImplementedException();
        }

        public MsalAppMetadataCacheItem GetAppMetadata(MsalAppMetadataCacheKey appMetadataKey)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
