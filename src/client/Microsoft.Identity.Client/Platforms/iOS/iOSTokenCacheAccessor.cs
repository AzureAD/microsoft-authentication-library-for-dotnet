﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Foundation;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Security;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    internal class iOSTokenCacheAccessor : ITokenCacheAccessor
    {
        public const string CacheKeyDelimiter = "-";

        private const bool _defaultSyncSetting = false;
        private const SecAccessible _defaultAccessiblityPolicy = SecAccessible.AfterFirstUnlockThisDeviceOnly;

        // Identifier for the keychain item used to retrieve current team ID
        private const string TeamIdKey = "DotNetTeamIDHint";
        private const string DefaultKeychainAccessGroup = "com.microsoft.adalcache";

        private string _keychainGroup;
        private readonly RequestContext _requestContext;

        public void SetiOSKeychainSecurityGroup(string keychainSecurityGroup)
        {
            if (string.IsNullOrEmpty(keychainSecurityGroup))
            {
                _keychainGroup = GetTeamId() + '.' + DefaultKeychainAccessGroup;
            }
            else
            {
                _keychainGroup = GetTeamId() + '.' + keychainSecurityGroup;
            }
        }

        private string GetTeamId()
        {
#pragma warning disable CA1422 // Validate platform compatibility
            var queryRecord = new SecRecord(SecKind.GenericPassword)
            {
                Service = "",
                Account = TeamIdKey,
                Accessible = SecAccessible.Always
            };
#pragma warning restore CA1422 // Validate platform compatibility

            SecRecord match = SecKeyChain.QueryAsRecord(queryRecord, out SecStatusCode resultCode);

            if (resultCode == SecStatusCode.ItemNotFound)
            {
                SecKeyChain.Add(queryRecord);
                match = SecKeyChain.QueryAsRecord(queryRecord, out resultCode);
            }

            if (resultCode == SecStatusCode.Success)
            {
                return match.AccessGroup.Split('.')[0];
            }

            throw new MsalClientException(
                MsalError.CannotAccessPublisherKeyChain,
                MsalErrorMessage.CannotAccessPublisherKeyChain);
        }

        public iOSTokenCacheAccessor()
        {
            SetiOSKeychainSecurityGroup(null);
        }

        public iOSTokenCacheAccessor(RequestContext requestContext) : this()
        {
            _requestContext = requestContext;
        }

#region SaveItem
        public void SaveAccessToken(MsalAccessTokenCacheItem item)
        {
            IiOSKey key = item.iOSCacheKey;
            Save(key, item.ToJsonString());
        }

        public void SaveRefreshToken(MsalRefreshTokenCacheItem item)
        {
            Save(item.iOSCacheKey, item.ToJsonString());
        }

        public void SaveIdToken(MsalIdTokenCacheItem item)
        {
            Save(item.iOSCacheKey, item.ToJsonString());
        }

        public void SaveAccount(MsalAccountCacheItem item)
        {
            Save(item.iOSCacheKey, item.ToJsonString());
        }
#endregion

#region GetItem
        public MsalIdTokenCacheItem GetIdToken(MsalAccessTokenCacheItem accessTokenCacheItem)
        {
            var idTokenItem = accessTokenCacheItem.GetIdTokenItem();
            return MsalIdTokenCacheItem.FromJsonString(GetPayload(idTokenItem.iOSCacheKey));
        }

        public MsalAccountCacheItem GetAccount(MsalAccountCacheItem accountCacheItem)
        {
            return MsalAccountCacheItem.FromJsonString(GetPayload(accountCacheItem.iOSCacheKey));
        }
#endregion

#region DeleteItem
        public void DeleteAccessToken(MsalAccessTokenCacheItem item)
        {
            Remove(item.iOSCacheKey);
        }

        public void DeleteRefreshToken(MsalRefreshTokenCacheItem item)
        {
            Remove(item.iOSCacheKey);
        }

        public void DeleteIdToken(MsalIdTokenCacheItem item)
        {
            Remove(item.iOSCacheKey);
        }

        public void DeleteAccount(MsalAccountCacheItem item)
        {
            Remove(item.iOSCacheKey);
        }

#endregion

        #region GetAllItems
        public List<MsalAccessTokenCacheItem> GetAllAccessTokens(string optionalPartitionKey = null, ILoggerAdapter requestlogger = null)
        {
            return GetPayloadAsString((int)MsalCacheKeys.iOSCredentialAttrType.AccessToken)
                .Select(x => MsalAccessTokenCacheItem.FromJsonString(x))
                .ToList();
        }

        public List<MsalRefreshTokenCacheItem> GetAllRefreshTokens(string optionalPartitionKey = null, ILoggerAdapter requestlogger = null)
        {
            return GetPayloadAsString((int)MsalCacheKeys.iOSCredentialAttrType.RefreshToken)
                .Select(x => MsalRefreshTokenCacheItem.FromJsonString(x))
                .ToList();
        }

        public List<MsalIdTokenCacheItem> GetAllIdTokens(string optionalPartitionKey = null, ILoggerAdapter requestlogger = null)
        {
            return GetPayloadAsString((int)MsalCacheKeys.iOSCredentialAttrType.IdToken)
                .Select(x => MsalIdTokenCacheItem.FromJsonString(x))
                .ToList();
        }

        public List<MsalAccountCacheItem> GetAllAccounts(string optionalPartitionKey = null, ILoggerAdapter requestlogger = null)
        {
            return GetPayloadAsString(MsalCacheKeys.iOSAuthorityTypeToAttrType[CacheAuthorityType.MSSTS.ToString()])
                .Select(x => MsalAccountCacheItem.FromJsonString(x))
                .ToList();
        }
        #endregion

        public int EntryCount { get; } = 0; // not implemented for iOS
        

        internal SecStatusCode TryGetBrokerApplicationToken(string clientId, out string appToken)
        {
            var queryRecord = new SecRecord(SecKind.GenericPassword)
            {
                AccessGroup = _keychainGroup,
                Account = clientId,
                Service = iOSBrokerConstants.iOSBroker,
            };

            SecRecord record = SecKeyChain.QueryAsRecord(queryRecord, out SecStatusCode resultCode);

            appToken = null;

            if (resultCode == SecStatusCode.Success)
            {
                appToken = record.ValueData.ToString(NSStringEncoding.UTF8);
            }

            return resultCode;
        }

        private string GetPayload(IiOSKey key)
        {
            var queryRecord = new SecRecord(SecKind.GenericPassword)
            {
                Account = key.iOSAccount,
                Service = key.iOSService,
                CreatorType = key.iOSType,
                AccessGroup = _keychainGroup
            };

            var match = SecKeyChain.QueryAsRecord(queryRecord, out SecStatusCode resultCode);

            return (resultCode == SecStatusCode.Success)
                ? match.ValueData.ToString(NSStringEncoding.UTF8)
                : string.Empty;
        }

        private ICollection<string> GetPayloadAsString(int type)
        {
            var queryRecord = new SecRecord(SecKind.GenericPassword)
            {
                CreatorType = type,
                AccessGroup = _keychainGroup
            };

            SecRecord[] records = SecKeyChain.QueryAsRecord(queryRecord, int.MaxValue, out SecStatusCode resultCode);

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

        private SecStatusCode Save(IiOSKey key, string payload)
        {
            var recordToSave = new SecRecord(SecKind.GenericPassword)
            {
                Account = key.iOSAccount,
                Service = key.iOSService,
                Generic = key.iOSGeneric,
                CreatorType = key.iOSType,
                ValueData = NSData.FromString(payload, NSStringEncoding.UTF8),
                AccessGroup = _keychainGroup,
                Accessible = _defaultAccessiblityPolicy,
                Synchronizable = _defaultSyncSetting,
            };

            var secStatusCode = Update(recordToSave);

            if (secStatusCode == SecStatusCode.ItemNotFound)
            {
                secStatusCode = SecKeyChain.Add(recordToSave);
            }

            if (secStatusCode == SecStatusCode.MissingEntitlement)
            {
                throw new MsalClientException(
                MsalError.MissingEntitlements,
                string.Format(
                    CultureInfo.InvariantCulture,
                    MsalErrorMessage.MissingEntitlements,
                    recordToSave.AccessGroup));
            }

            return secStatusCode;
        }

        internal SecStatusCode SaveBrokerApplicationToken(string clientIdAsKey, string applicationToken)
        {
            // The broker application token is used starting w/iOS broker 6.3.19+ (v3)
            // If the application cannot be verified, an additional user consent dialog will be required the first time 
            // when the application is using the iOS broker. 
            // After initial user consent, the iOS broker will issue a token to the application that will 
            // grant application access to its own cache on subsequent broker invocations.
            // On subsequent calls, the application presents the application token to the iOS broker, this will prevent
            // the showing of the consent dialog.
            var recordToSave = new SecRecord(SecKind.GenericPassword)
            {
                Account = clientIdAsKey,
                Service = iOSBrokerConstants.iOSBroker,
                ValueData = NSData.FromString(applicationToken, NSStringEncoding.UTF8),
                AccessGroup = _keychainGroup,
                Accessible = _defaultAccessiblityPolicy,
                Synchronizable = _defaultSyncSetting
            };

            SecStatusCode secStatusCode = Update(recordToSave);

            if (secStatusCode == SecStatusCode.ItemNotFound)
            {
                secStatusCode = SecKeyChain.Add(recordToSave);
            }

            return secStatusCode;
        }

        private SecStatusCode Remove(IiOSKey key)
        {
            var record = new SecRecord(SecKind.GenericPassword)
            {
                Account = key.iOSAccount,
                Service = key.iOSService,
                CreatorType = key.iOSType,
                AccessGroup = _keychainGroup
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
                AccessGroup = _keychainGroup
            };
            var attributesToUpdate = new SecRecord()
            {
                ValueData = updatedRecord.ValueData
            };

            return SecKeyChain.Update(currentRecord, attributesToUpdate);
        }

        private void RemoveByType(int type)
        {
            var queryRecord = new SecRecord(SecKind.GenericPassword)
            {
                CreatorType = type,
                AccessGroup = _keychainGroup
            };
            SecKeyChain.Remove(queryRecord);
        }

        public void Clear(ILoggerAdapter requestlogger = null)
        {
            RemoveByType((int)MsalCacheKeys.iOSCredentialAttrType.AccessToken);
            RemoveByType((int)MsalCacheKeys.iOSCredentialAttrType.RefreshToken);
            RemoveByType((int)MsalCacheKeys.iOSCredentialAttrType.IdToken);

            RemoveByType(MsalCacheKeys.iOSAuthorityTypeToAttrType[CacheAuthorityType.MSSTS.ToString()]);
        }

        /// <summary>
        /// This method is used during token cache serialization which is not supported for iOS.
        /// </summary>
        public bool HasAccessOrRefreshTokens()
        {
            throw new NotSupportedException();
        }
   
        #region AppMetadata - not implemented on iOS
        public MsalAppMetadataCacheItem ReadAppMetadata(MsalAppMetadataCacheItem appMetadataItem)
        {
            //return MsalAppMetadataCacheItem.FromJsonString(GetPayload(appMetadataKey));
            throw new NotImplementedException();
        }

        public void WriteAppMetadata(MsalAppMetadataCacheItem appMetadata)
        {
            //Save(appMetadata.GetKey(), appMetadata.ToJsonString());
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
