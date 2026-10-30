// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Android.App;
using Android.Content;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Platforms.Android
{
    internal class AndroidTokenCacheAccessor : ITokenCacheAccessor
    {
        private const string AccessTokenSharedPreferenceName = "com.microsoft.identity.client.accessToken";
        private const string RefreshTokenSharedPreferenceName = "com.microsoft.identity.client.refreshToken";
        private const string IdTokenSharedPreferenceName = "com.microsoft.identity.client.idToken";
        private const string AccountSharedPreferenceName = "com.microsoft.identity.client.Account";

        private readonly (ISharedPreferences Legacy, ISharedPreferences Subject) _accessTokenSharedPreference;
        private readonly (ISharedPreferences Legacy, ISharedPreferences Subject) _refreshTokenSharedPreference;
        private readonly (ISharedPreferences Legacy, ISharedPreferences Subject) _idTokenSharedPreference;
        private readonly (ISharedPreferences Legacy, ISharedPreferences Subject) _accountSharedPreference;

        private readonly RequestContext _requestContext;

        public AndroidTokenCacheAccessor() : this(requestContext: null)
        {
        }

        public AndroidTokenCacheAccessor(RequestContext requestContext)
            : this(name => Application.Context.GetSharedPreferences(name, FileCreationMode.Private), requestContext)
        {
        }

        internal AndroidTokenCacheAccessor(Func<string, ISharedPreferences> getPreferences, RequestContext requestContext)
        {
            if (getPreferences is null)
            {
                throw new ArgumentNullException(nameof(getPreferences));
            }

            _requestContext = requestContext;
            _accessTokenSharedPreference = OpenPreferences(AccessTokenSharedPreferenceName);
            _refreshTokenSharedPreference = OpenPreferences(RefreshTokenSharedPreferenceName);
            _idTokenSharedPreference = OpenPreferences(IdTokenSharedPreferenceName);
            _accountSharedPreference = OpenPreferences(AccountSharedPreferenceName);

            (ISharedPreferences Legacy, ISharedPreferences Subject) OpenPreferences(string name)
            {
                var legacy = getPreferences(name);
                // Separate storage makes legacy collisions ineligible without changing shared cache formats.
                var subject = getPreferences(name + ".subject.v1");
                if (legacy is null || subject is null)
                {
                    throw new MsalClientException(MsalError.FailedToCreateSharedPreference, "Fail to create SharedPreference");
                }

                return (legacy, subject);
            }
        }

        #region SaveItem
        public void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            Save(item, item.CacheKey, _accessTokenSharedPreference);
        }

        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            Save(item, item.CacheKey, _refreshTokenSharedPreference);
        }

        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            Save(item, item.CacheKey, _idTokenSharedPreference);
        }

        public void SaveAccount(MsalAccountCacheItem item)
        {
            Save(item, item.CacheKey, _accountSharedPreference);
        }

        private static (ISharedPreferences Preferences, string Key) GetStorage(
            MsalCacheItemBase item, string cacheKey, (ISharedPreferences Legacy, ISharedPreferences Subject) store)
        {
            item.IsAndroidCacheItem = true;
            // Length-prefix the exact subject so neither case folding nor delimiters can merge identities.
            return item.IsAndroidSubject
                ? (store.Subject, item.HomeAccountId.Length.ToString(CultureInfo.InvariantCulture) + ":" + item.HomeAccountId + ":" + cacheKey)
                : (store.Legacy, cacheKey);
        }

        private static void Save(MsalCacheItemBase item, string cacheKey, (ISharedPreferences Legacy, ISharedPreferences Subject) store)
        {
            var storage = GetStorage(item, cacheKey, store);
            var editor = storage.Preferences.Edit();
            editor.PutString(storage.Key, item.ToJObject().ToString());
            editor.Apply();
        }
        #endregion

        #region DeleteItem
        public void DeleteAccessToken(MsalAccessTokenCacheItem item)
        {
            Delete(item, item.CacheKey, _accessTokenSharedPreference);
        }

        public void DeleteRefreshToken(MsalRefreshTokenCacheItem item)
        {
            Delete(item, item.CacheKey, _refreshTokenSharedPreference);
        }

        public void DeleteIdToken(MsalIdTokenCacheItem item)
        {
            Delete(item, item.CacheKey, _idTokenSharedPreference);
        }

        public void DeleteAccount(MsalAccountCacheItem item)
        {
            Delete(item, item.CacheKey, _accountSharedPreference);

        }

        private static void Delete(MsalCacheItemBase item, string cacheKey, (ISharedPreferences Legacy, ISharedPreferences Subject) store)
        {
            var storage = GetStorage(item, cacheKey, store);
            var editor = storage.Preferences.Edit();
            editor.Remove(storage.Key);
            editor.Apply();
        }

        private static void DeleteAll((ISharedPreferences Legacy, ISharedPreferences Subject) store)
        {
            var editor = store.Legacy.Edit();
            editor.Clear();
            editor.Apply();
            editor = store.Subject.Edit();
            editor.Clear();
            editor.Apply();
        }

        public void Clear(ILoggerAdapter requestlogger)
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
            var item = accessTokenCacheItem.GetIdTokenItem();
            return GetItem(item, item.CacheKey, _idTokenSharedPreference, MsalIdTokenCacheItem.FromJsonString);
        }
        #endregion

        #region GetAll
        public List<MsalAccessTokenCacheItem> GetAllAccessTokens(string optionalPartitionKey = null, ILoggerAdapter requestlogger = null)
        {
            return GetAll(_accessTokenSharedPreference, MsalAccessTokenCacheItem.FromJsonString, requestlogger);
        }

        public List<MsalRefreshTokenCacheItem> GetAllRefreshTokens(string optionalPartitionKey = null, ILoggerAdapter requestlogger = null)
        {
            return GetAll(_refreshTokenSharedPreference, MsalRefreshTokenCacheItem.FromJsonString, requestlogger);
        }

        public List<MsalIdTokenCacheItem> GetAllIdTokens(string optionalPartitionKey = null, ILoggerAdapter requestlogger = null)
        {
            return GetAll(_idTokenSharedPreference, MsalIdTokenCacheItem.FromJsonString, requestlogger);
        }

        public List<MsalAccountCacheItem> GetAllAccounts(string optionalPartitionKey = null, ILoggerAdapter requestlogger = null)
        {
            return GetAll(_accountSharedPreference, MsalAccountCacheItem.FromJsonString, requestlogger);
        }

        private List<T> GetAll<T>((ISharedPreferences Legacy, ISharedPreferences Subject) store,
            Func<string, T> deserialize, ILoggerAdapter logger) where T : MsalCacheItemBase
        {
            var items = store.Legacy.All.Values.Cast<string>().Select(json => Read(json, deserialize)).ToList();
            if (items.RemoveAll(item => item.IsAndroidSubject) > 0)
            {
                (logger ?? _requestContext?.Logger)?.Warning("Ignoring legacy Android subject entries; sign-in is required.");
            }

            items.AddRange(store.Subject.All.Values.Cast<string>().Select(json => Read(json, deserialize)));
            return items;
        }

        private T GetItem<T>(MsalCacheItemBase query, string cacheKey,
            (ISharedPreferences Legacy, ISharedPreferences Subject) store, Func<string, T> deserialize) where T : MsalCacheItemBase
        {
            var storage = GetStorage(query, cacheKey, store);
            var item = Read(storage.Preferences.GetString(storage.Key, null), deserialize);
            if (item is not null && (item.IsAndroidSubject != query.IsAndroidSubject ||
                !item.MatchesHomeAccountId(query.HomeAccountId)))
            {
                _requestContext?.Logger.Warning("Ignoring an Android cache record with a different identity.");
                return null;
            }

            return item;
        }

        private static T Read<T>(string json, Func<string, T> deserialize) where T : MsalCacheItemBase
        {
            var item = deserialize(json);
            if (item is not null)
            {
                item.IsAndroidCacheItem = true;
            }

            return item;
        }
        #endregion

        public int EntryCount { get; } = 0; // not implemented for Android

        public MsalAccountCacheItem GetAccount(MsalAccountCacheItem accountCacheItem)
        {
            return GetItem(accountCacheItem, accountCacheItem.CacheKey, _accountSharedPreference, MsalAccountCacheItem.FromJsonString);
        }

        /// <summary>
        /// This method is used during token cache serialization which is not supported for Android.
        /// </summary>
        public bool HasAccessOrRefreshTokens()
        {
            throw new NotSupportedException();
        }

        #region App Metadata - not used on Android
        public MsalAppMetadataCacheItem ReadAppMetadata(MsalAppMetadataCacheItem appMetadataItem)
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

        public MsalAppMetadataCacheItem GetAppMetadata(MsalAppMetadataCacheItem appMetadataItem)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
