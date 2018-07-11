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
using Security;
using Foundation;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using System.Collections.ObjectModel;
using Microsoft.Identity.Core.Helpers;

namespace Microsoft.Identity.Core
{
    internal class TokenCacheAccessor : ITokenCacheAccessor
    {
        public const string CacheKeyDelimiter = "-";

        static Dictionary<string, int> AuthorityTypeToAttrType = new Dictionary<string, int>()
        {
            {AuthorityType.AAD.ToString(), 1001},
            {AuthorityType.MSA.ToString(), 1002},
            {AuthorityType.MSSTS.ToString(), 1003},
            {AuthorityType.OTHER.ToString(), 1004},
        };

        enum CredentialAttrType
        {
            AccessToken = 2001,
            RefreshToken = 2002,
            IdToken = 2003,
            Password = 2004
        }

        private const bool _defaultSyncSetting = false;
        private const SecAccessible _defaultAccessiblityPolicy = SecAccessible.AfterFirstUnlockThisDeviceOnly;

        private readonly string DefaultKeychainGroup = "com.microsoft.adalcache";
        private readonly string TeamIdKey = "teamIDHint";

        private string keychainGroup;

        private string GetBundleId()
        {
            return NSBundle.MainBundle.BundleIdentifier;
        }
   
        public void SetSecurityGroup(string securityGroup)
        {
            if (securityGroup == null)
            {
                keychainGroup = GetBundleId();
            }
            else
            {
                keychainGroup = securityGroup;
            }
        }

        private string GetTeamId()
        {
            var queryRecord = new SecRecord(SecKind.GenericPassword)
            {
                Service = "",
                Account = TeamIdKey
            };

            SecRecord match = SecKeyChain.QueryAsRecord(queryRecord, out SecStatusCode resultCode);

            if (resultCode == SecStatusCode.ItemNotFound)
            {
                SecKeyChain.Add(queryRecord);
                match = SecKeyChain.QueryAsRecord(queryRecord, out resultCode);
            }

            return match.AccessGroup.Split('.')[0];
        }

        private RequestContext _requestContext;

        public TokenCacheAccessor()
        {
            keychainGroup = GetTeamId() + '.' + DefaultKeychainGroup;
        }

        public TokenCacheAccessor(RequestContext requestContext) : this()
        {
            _requestContext = requestContext;
        }

        public void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            var key = item.GetKey();
            var account = key.HomeAccountId + CacheKeyDelimiter +
                          key.Environment;

            var service = key.CredentialType + CacheKeyDelimiter +
                          key.ClientId + CacheKeyDelimiter +
                          key.TenantId + CacheKeyDelimiter +
                          key.Scopes;

            var generic = key.CredentialType + CacheKeyDelimiter +
                          key.ClientId + CacheKeyDelimiter +
                          key.TenantId;

            var type = (int)CredentialAttrType.AccessToken; 

            var value = JsonHelper.SerializeToJson(item);

            Save(account, service, generic, type, value);
        }

        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            var key = item.GetKey();
            var account = key.HomeAccountId + CacheKeyDelimiter +
                          key.Environment;

            var service = key.CredentialType + CacheKeyDelimiter +
                          key.ClientId + CacheKeyDelimiter +
                          "" + CacheKeyDelimiter;

            var generic = key.CredentialType + CacheKeyDelimiter +
                          key.ClientId + CacheKeyDelimiter;

            var type = (int)CredentialAttrType.RefreshToken;

            var value = JsonHelper.SerializeToJson(item);

            Save(account, service, generic, type, value);
        }

        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            var key = item.GetKey();
            var account = key.HomeAccountId + CacheKeyDelimiter +
                          key.Environment;

            var service = key.CredentialType + CacheKeyDelimiter +
                          key.ClientId + CacheKeyDelimiter +
                          key.TenantId + CacheKeyDelimiter;

            var generic = key.CredentialType + CacheKeyDelimiter +
                          key.ClientId + CacheKeyDelimiter +
                          key.TenantId;

            var type = (int)CredentialAttrType.IdToken;

            var value = JsonHelper.SerializeToJson(item);

            Save(account, service, generic, type, value);
        }

        public void SaveAccount(MsalAccountCacheItem item)
        {
            var key = item.GetKey();
            var account = key.HomeAccountId + CacheKeyDelimiter +
                          key.Environment;

            var service = key.TenantId;

            var generic = item.LocalAccountId;

            var type = AuthorityTypeToAttrType[item.AuthorityType];

            var value = JsonHelper.SerializeToJson(item);

            Save(account, service, generic, type, value);
        }

        
        public void DeleteAccessToken(MsalAccessTokenCacheKey cacheKey)
        {
            var account = cacheKey.HomeAccountId + CacheKeyDelimiter + 
                cacheKey.Environment;

            var service = cacheKey.CredentialType + CacheKeyDelimiter +
                          cacheKey.ClientId + CacheKeyDelimiter +
                          cacheKey.TenantId + CacheKeyDelimiter +
                          cacheKey.Scopes;

            var type = (int)CredentialAttrType.AccessToken;

            Remove(account, service, type);
        }

        public void DeleteRefreshToken(MsalRefreshTokenCacheKey cacheKey)
        {
            var account = cacheKey.HomeAccountId + CacheKeyDelimiter +
                          cacheKey.Environment;

            var service = cacheKey.CredentialType + CacheKeyDelimiter +
                          cacheKey.ClientId + CacheKeyDelimiter +
                          "" + CacheKeyDelimiter;

            var type = (int)CredentialAttrType.RefreshToken;

            Remove(account, service, type);
        }

        public void DeleteIdToken(MsalIdTokenCacheKey cacheKey)
        {
            var account = cacheKey.HomeAccountId + CacheKeyDelimiter +
                          cacheKey.Environment;

            var service = cacheKey.CredentialType + CacheKeyDelimiter +
                          cacheKey.ClientId + CacheKeyDelimiter +
                          cacheKey.TenantId + CacheKeyDelimiter;

            var type = (int)CredentialAttrType.IdToken;

            Remove(account, service, type);
        }

        public void DeleteAccount(MsalAccountCacheKey cacheKey)
        {
            var account = cacheKey.HomeAccountId + CacheKeyDelimiter +
                          cacheKey.Environment;

            var service = cacheKey.TenantId;

            var type = AuthorityTypeToAttrType[AuthorityType.MSSTS.ToString()];

            Remove(account, service, type);
        }
        

        public ICollection<string> GetAllAccessTokensAsString()
        {
            return GetValues((int)CredentialAttrType.AccessToken);
        }

        public ICollection<string> GetAllRefreshTokensAsString()
        {
            return GetValues((int)CredentialAttrType.RefreshToken);
        }

        public ICollection<string> GetAllIdTokensAsString()
        {
            return GetValues((int)CredentialAttrType.IdToken);
        }

        public ICollection<string> GetAllAccountsAsString()
        {
            return GetValues(AuthorityTypeToAttrType[AuthorityType.MSSTS.ToString()]);
        }
        /*
        public ICollection<string> GetAllAccessTokenKeys()
        {
            return GetKeys(AccessTokenServiceId);
        }
        
        public ICollection<string> GetAllRefreshTokenKeys()
        {
            return GetKeys(RefreshTokenServiceId);
        }

        public ICollection<string> GetAllIdTokenKeys()
        {
            return GetKeys(IdTokenServiceId);
        }

        public ICollection<string> GetAllAccountKeys()
        {
            return GetKeys(AccountServiceId);
        }
        */
        private string GetValue(string account, string service, int type)
        {
            var queryRecord = new SecRecord(SecKind.GenericPassword)
            {
                Account = account,
                Service = service,
                CreatorType = type,
                AccessGroup = keychainGroup
            };

            var match = SecKeyChain.QueryAsRecord(queryRecord, out SecStatusCode resultCode);

            return (resultCode == SecStatusCode.Success)
                ? match.ValueData.ToString(NSStringEncoding.UTF8)
                : string.Empty;
        }

        private ICollection<string> GetValues(int type)
        {
            var queryRecord = new SecRecord(SecKind.GenericPassword)
            {
                CreatorType = type,
                AccessGroup = keychainGroup
            };

            SecRecord[] records = SecKeyChain.QueryAsRecord(queryRecord, Int32.MaxValue, out SecStatusCode resultCode);

            ICollection<string> res = new List<string>();

            if (resultCode == SecStatusCode.Success)
            {
                foreach (var record in records)
                {
                    string str = record.ValueData.ToString(NSStringEncoding.UTF8);
                    res.Add(str);
                }
            }

            return res;
        }
        /*
        private ICollection<string> GetKeys(string service)
        {
            var queryRecord = new SecRecord(SecKind.GenericPassword)
            {
                Service = service,
                AccessGroup = keychainGroup,
            };

            SecRecord[] records = SecKeyChain.QueryAsRecord(queryRecord, Int32.MaxValue, out SecStatusCode resultCode);

            ICollection<string> res = new List<string>();

            if (resultCode == SecStatusCode.Success)
            {
                foreach (var record in records)
                {
                    res.Add(record.Account);
                }
            }

            return res;
        }
        */
        private SecStatusCode Save(string account, string service, string generic, int type, string value)
        {
            SecRecord recordToSave = CreateRecord(account, service, generic, type, value);

            var secStatusCode = Update(recordToSave);

            if (secStatusCode == SecStatusCode.ItemNotFound)
            {
                secStatusCode = SecKeyChain.Add(recordToSave);
            }

            return secStatusCode;
        }
        /*
        private SecStatusCode SetValueForKey(string key, string value, string service)
        {
            Remove(key, service);

            var result = SecKeyChain.Add(CreateRecord(key, value, service));

            return result;
        }
        */
        private SecRecord CreateRecord(string account, string service, string generic, int type, string value)
        {
            return new SecRecord(SecKind.GenericPassword)
            {
                Account = account,
                Service = service,
                Generic = generic,
                CreatorType = type,
                ValueData = NSData.FromString(value, NSStringEncoding.UTF8),
                AccessGroup = keychainGroup,
                Accessible = _defaultAccessiblityPolicy,
                Synchronizable = _defaultSyncSetting,
            };
        }
        
        private SecStatusCode Remove(string account, string service, int type)
        {
            var record = new SecRecord(SecKind.GenericPassword)
            {
                Account = account,
                Service = service,
                CreatorType = type,
                AccessGroup = keychainGroup
            };

            return SecKeyChain.Remove(record);
        }

        private SecStatusCode Update(SecRecord updatedRecord)
        {
            var currentRecord = new SecRecord(SecKind.GenericPassword)
            {
                Account = updatedRecord.Account,
                Service = updatedRecord.Service,
                CreatorType = updatedRecord.CreatorType,
                AccessGroup = keychainGroup
            };
            var attributesToUpdate = new SecRecord()
            {
                ValueData = updatedRecord.ValueData
            };

            return SecKeyChain.Update(currentRecord, attributesToUpdate);
        }

        private void RemoveAll(int type)
        {
            var queryRecord = new SecRecord(SecKind.GenericPassword)
            {
                CreatorType = type,
                AccessGroup = keychainGroup
            };
            SecKeyChain.Remove(queryRecord);
        }

        public void Clear()
        {
            RemoveAll((int)CredentialAttrType.AccessToken);
            RemoveAll((int)CredentialAttrType.RefreshToken);
            RemoveAll((int)CredentialAttrType.IdToken);

            RemoveAll(AuthorityTypeToAttrType[AuthorityType.MSSTS.ToString()]);
        }

        public string GetAccessToken(MsalAccessTokenCacheKey accessTokenKey)
        {
            var account = accessTokenKey.HomeAccountId + CacheKeyDelimiter +
                          accessTokenKey.Environment;

            var service = accessTokenKey.CredentialType + CacheKeyDelimiter +
                          accessTokenKey.ClientId + CacheKeyDelimiter +
                          accessTokenKey.TenantId + CacheKeyDelimiter +
                          accessTokenKey.Scopes;

            var type = (int)CredentialAttrType.AccessToken;

            return GetValue(account, service, type);
        }

        public string GetRefreshToken(MsalRefreshTokenCacheKey refreshTokenKey)
        {
            var account = refreshTokenKey.HomeAccountId + CacheKeyDelimiter +
                          refreshTokenKey.Environment;

            var service = refreshTokenKey.CredentialType + CacheKeyDelimiter +
                          refreshTokenKey.ClientId + CacheKeyDelimiter +
                          "" + CacheKeyDelimiter;

            var type = (int)CredentialAttrType.RefreshToken;

            return GetValue(account, service, type);
        }

        public string GetIdToken(MsalIdTokenCacheKey idTokenKey)
        {
            var account = idTokenKey.HomeAccountId + CacheKeyDelimiter +
                          idTokenKey.Environment;

            var service = idTokenKey.CredentialType + CacheKeyDelimiter +
                          idTokenKey.ClientId + CacheKeyDelimiter +
                          idTokenKey.TenantId + CacheKeyDelimiter;

            var type = (int)CredentialAttrType.IdToken;

            return GetValue(account, service, type);
        }

        public string GetAccount(MsalAccountCacheKey accountKey)
        {
            var account = accountKey.HomeAccountId + CacheKeyDelimiter +
                          accountKey.Environment;

            var service = accountKey.TenantId;

            var type = AuthorityTypeToAttrType[AuthorityType.MSSTS.ToString()];

            return GetValue(account, service, type);
        }
    }
}
