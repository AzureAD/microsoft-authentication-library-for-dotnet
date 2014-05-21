//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class CompositeCacheElement
    {
        public const string Authority = "Authority";
        public const string ClientId = "ClientId";
        public const string ExpiresOn = "ExpiresOn";
        public const string FamilyName = "FamilyName";
        public const string GivenName = "GivenName";
        public const string IdentityProviderName = "IdentityProviderName";
        public const string IsMultipleResourceRefreshToken = "IsMultipleResourceRefreshToken";
        public const string Resource = "Resource";
        public const string TenantId = "TenantId";
        public const string UniqueId = "UniqueId";
        public const string DisplayableId = "DisplayableId";
        public const string CacheValue = "CacheValue";
        public const string CacheValueSegmentCount = "CacheValueSegmentCount";
    }

    /// <summary>
    /// This class implements a persistent cache based on application local settings. It uses composite local settings which hold
    /// both TokenCacheKey properties and the cache value containing serialized version of AuthenticationResult as setting values.
    /// The key to local setting is simply a Guid for uniqueness. This may change in the future to improve performance if needed.
    /// </summary>
    internal class DefaultTokenCacheStore : IDictionary<TokenCacheKey, string>
    {
        private const string LocalSettingsContainerName = "ActiveDirectoryAuthenticationLibrary";

        // The prefix contains cache version which should change if the format of the data store in the cache changes.
        private const string LocalSettingsPrefix = "ADAL-1-";

        private readonly IPropertySet settingValues;

        public DefaultTokenCacheStore()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.CreateContainer(LocalSettingsContainerName, ApplicationDataCreateDisposition.Always);
            settingValues = localSettings.Containers[LocalSettingsContainerName].Values;            
        }

        public int Count
        {
            get { return this.Keys.Count; }
        }

        public ICollection<TokenCacheKey> Keys
        {
            get
            {
                IEnumerable<ApplicationDataCompositeValue> cacheItemValues = this.GetAllLocalSettingValues();
                try
                {
                    return cacheItemValues.Select(ConvertToTokenCacheKey).ToList();
                }
                catch (InvalidCastException)
                {
                    this.RemoveCorruptLocalSettings();
                    return new List<TokenCacheKey>();
                }
            }
        }

        public ICollection<string> Values
        {
            get
            {
                IEnumerable<ApplicationDataCompositeValue> cacheItemValues = this.GetAllLocalSettingValues();
                try
                { 
                    return cacheItemValues.Select(LocalSettingsHelper.GetCacheValue).ToList();
                }
                catch (FormatException)
                {
                    // If the cache content is corrupted and cache decryption fails, FormatException is thrown.
                    // In this case, we go over all the elements and remove the corrupted ones.
                    this.RemoveCorruptLocalSettings();
                    return new List<string>();
                }
            }
        }

        public string this[TokenCacheKey key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException("key");
                }

                string value;
                if (!TryGetValue(key, out value))
                {
                    throw new KeyNotFoundException(string.Format("Key '{0}' is not found in the dictionary", key));
                }

                return value;
            }
            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException("key");
                }

                IEnumerable<KeyValuePair<string, object>> cacheItems = this.GetAllLocalSettings();
                foreach (KeyValuePair<string, object> cacheItem in cacheItems)
                {
                    var existingCompositeValue = (ApplicationDataCompositeValue)cacheItem.Value;
                    if (this.AreKeyValuesEqual(cacheItem, key))
                    {
                        LocalSettingsHelper.RemoveCacheValue(existingCompositeValue);
                        LocalSettingsHelper.SetCacheValue(existingCompositeValue, value);
                        settingValues[cacheItem.Key] = existingCompositeValue;
                        return;
                    }
                }

                this.AddAsLocalSetting(key, value);
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Add(TokenCacheKey key, string value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            string valueInStore;
            // Method Add does not replace the value if the key exists
            if (this.TryGetValue(key, out valueInStore))
            {
                throw new ArgumentException("An element with the same key already exists in the cache", "key");
            }

            this.AddAsLocalSetting(key, value);
        }

        public void Add(KeyValuePair<TokenCacheKey, string> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            IEnumerable<KeyValuePair<string, object>> cacheItems = this.GetAllLocalSettings();
            foreach (KeyValuePair<string, object> cacheItem in cacheItems)
            {
                settingValues.Remove(cacheItem.Key);
            }
        }

        public bool Contains(KeyValuePair<TokenCacheKey, string> item)
        {
            string value;

            bool found = this.TryGetValue(item.Key, out value);

            return found && (value.Equals(item.Value));
        }

        public bool ContainsKey(TokenCacheKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            string value;
            return TryGetValue(key, out value);
        }

        public void CopyTo(KeyValuePair<TokenCacheKey, string>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", "arrayIndex cannot be negative");
            }

            List<KeyValuePair<string, object>> cacheItems = this.GetAllLocalSettings().ToList();

            if (cacheItems.Count > array.Length - arrayIndex)
            {
                throw new ArgumentException("array is not large enough to copy all cache items");
            }

            for (int i = 0; i < cacheItems.Count; i++)
            {
                var cacheItemValue = (ApplicationDataCompositeValue)cacheItems[i].Value;
                try
                {
                    TokenCacheKey cacheKey = ConvertToTokenCacheKey(cacheItemValue);
                    array[arrayIndex + i] = new KeyValuePair<TokenCacheKey, string>(cacheKey, LocalSettingsHelper.GetCacheValue(cacheItemValue));
                }
                catch (InvalidCastException)
                {
                    // This happens when cache key does not have correct type.
                    this.RemoveCorruptLocalSettings();

                    // In case there is a corrupt item in the cache, the place of that item will be empty in the result.
                    array[arrayIndex + i] = default(KeyValuePair<TokenCacheKey, string>);
                }
                catch (FormatException)
                {
                    // This happens when encrypted cache value does not have correct format.
                    this.RemoveCorruptLocalSettings();

                    // In case there is a corrupt item in the cache, the place of that item will be empty in the result.
                    array[arrayIndex + i] = default(KeyValuePair<TokenCacheKey, string>);
                }
            }
        }

        public bool Remove(TokenCacheKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            IEnumerable<KeyValuePair<string, object>> cacheItems = GetAllLocalSettings();
            foreach (KeyValuePair<string, object> cacheItem in cacheItems)
            {
                if (this.AreKeyValuesEqual(cacheItem, key))
                {
                    return settingValues.Remove(cacheItem.Key);
                }
            }

            return false;
        }

        public bool Remove(KeyValuePair<TokenCacheKey, string> item)
        {
            if (this.Contains(item))
            {
                return this.Remove(item.Key);
            }

            return false;
        }

        public bool TryGetValue(TokenCacheKey key, out string value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            value = null;
            IEnumerable<KeyValuePair<string, object>> cacheItems = GetAllLocalSettings();
            foreach (KeyValuePair<string, object> cacheItem in cacheItems)
            {
                if (this.AreKeyValuesEqual(cacheItem, key))
                {
                    try 
                    { 
                        value = LocalSettingsHelper.GetCacheValue((ApplicationDataCompositeValue)cacheItem.Value);
                        return true;
                    }
                    catch (FormatException)
                    {
                        this.RemoveCorruptLocalSettings();
                        return false;
                    }
                }
            }

            return false;
        }

        public IEnumerator<KeyValuePair<TokenCacheKey, string>> GetEnumerator()
        {
            return this.Keys.Select(key => new KeyValuePair<TokenCacheKey, string>(key, this[key])).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private static ApplicationDataCompositeValue ConvertToCompositeValue(TokenCacheKey cacheKey)
        {
            var compositeValue = new ApplicationDataCompositeValue();
            compositeValue[CompositeCacheElement.Authority] = cacheKey.Authority;
            compositeValue[CompositeCacheElement.ClientId] = cacheKey.ClientId;
            compositeValue[CompositeCacheElement.ExpiresOn] = cacheKey.ExpiresOn.UtcTicks;
            compositeValue[CompositeCacheElement.FamilyName] = cacheKey.FamilyName;
            compositeValue[CompositeCacheElement.GivenName] = cacheKey.GivenName;
            compositeValue[CompositeCacheElement.IdentityProviderName] = cacheKey.IdentityProviderName;
            compositeValue[CompositeCacheElement.IsMultipleResourceRefreshToken] = cacheKey.IsMultipleResourceRefreshToken;
            compositeValue[CompositeCacheElement.Resource] = cacheKey.Resource;
            compositeValue[CompositeCacheElement.TenantId] = cacheKey.TenantId;
            compositeValue[CompositeCacheElement.UniqueId] = cacheKey.UniqueId;
            compositeValue[CompositeCacheElement.DisplayableId] = cacheKey.DisplayableId;

            return compositeValue;
        }

        private static TokenCacheKey ConvertToTokenCacheKey(ApplicationDataCompositeValue compositeValue)
        {
            var cacheKey = new TokenCacheKey
                {
                    Authority = (string)compositeValue[CompositeCacheElement.Authority],
                    ClientId = (string)compositeValue[CompositeCacheElement.ClientId],
                    ExpiresOn = new DateTimeOffset((long)compositeValue[CompositeCacheElement.ExpiresOn], TimeSpan.Zero),
                    FamilyName = (string)compositeValue[CompositeCacheElement.FamilyName],
                    GivenName = (string)compositeValue[CompositeCacheElement.GivenName],
                    IdentityProviderName = (string)compositeValue[CompositeCacheElement.IdentityProviderName],
                    IsMultipleResourceRefreshToken = (bool)compositeValue[CompositeCacheElement.IsMultipleResourceRefreshToken],
                    Resource = (string)compositeValue[CompositeCacheElement.Resource],
                    TenantId = (string)compositeValue[CompositeCacheElement.TenantId],
                    UniqueId = (string)compositeValue[CompositeCacheElement.UniqueId],
                    DisplayableId = (string)compositeValue[CompositeCacheElement.DisplayableId]
                };

            return cacheKey;
        }

        private void AddAsLocalSetting(TokenCacheKey cacheKey, string value)
        {
            ApplicationDataCompositeValue compositeValue = ConvertToCompositeValue(cacheKey);
            LocalSettingsHelper.SetCacheValue(compositeValue, value);
            settingValues[LocalSettingsPrefix + Guid.NewGuid()] = compositeValue;
        }

        private IEnumerable<KeyValuePair<string, object>> GetAllLocalSettings()
        {
            return settingValues.Where(v => v.Key.StartsWith(LocalSettingsPrefix));
        }

        private IEnumerable<ApplicationDataCompositeValue> GetAllLocalSettingValues()
        {
            IEnumerable<object> cacheobjects = this.GetAllLocalSettings().Select(item => item.Value);

            try
            {
                return cacheobjects.Cast<ApplicationDataCompositeValue>();
            }
            catch (InvalidCastException)
            {
                this.RemoveCorruptLocalSettings();
                return new List<ApplicationDataCompositeValue>();
            }
        }

        private bool AreKeyValuesEqual(KeyValuePair<string, object> cacheItem, TokenCacheKey key)
        {
            try
            {
                var existingCompositeValue = (ApplicationDataCompositeValue)cacheItem.Value;
                return ConvertToTokenCacheKey(existingCompositeValue).Equals(key);
            }
            catch (InvalidCastException)
            {
                this.RemoveCorruptLocalSettings();
                return false;
            }
        }

        private void RemoveCorruptLocalSettings()
        {
            Logger.Information(null, "Some token cache items were corrupted, so cleaning up those items");

            // Cache contains elements of bad format, so clear cache to start clean.
            foreach (KeyValuePair<string, object> kvp in this.GetAllLocalSettings())
            {
                try
                {
                    ApplicationDataCompositeValue compositeValue = (ApplicationDataCompositeValue)kvp.Value;
                    ConvertToTokenCacheKey(compositeValue);
                    LocalSettingsHelper.GetCacheValue(compositeValue);
                }
                catch (InvalidCastException)
                {
                    settingValues.Remove(kvp.Key);
                }
                catch (FormatException)
                {
                    settingValues.Remove(kvp.Key);
                }
            }

            Logger.Information(null, "Token cache cleanup for corrupted items completed");
        }
    }
}
