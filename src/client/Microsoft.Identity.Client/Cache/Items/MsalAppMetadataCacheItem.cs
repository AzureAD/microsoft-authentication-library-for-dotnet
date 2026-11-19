// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Utils;
using JObject = System.Text.Json.Nodes.JsonObject;

namespace Microsoft.Identity.Client.Cache.Items
{
    /// <summary>
    /// Apps shouldn't rely on its presence, unless the app itself wrote it. It means that SDK should translate absence of app metadata to the default values of its required fields.
    /// Other apps that don't support app metadata should never remove existing app metadata.
    /// App metadata is a non-removable entity.It means there's no need for a public API to remove app metadata, and it shouldn't be removed when removeAccount is called.
    /// App metadata is a non-secret entity. It means that it cannot store any secret information, like tokens, nor PII, like username etc.
    /// App metadata can be extended by adding additional fields when required.Absence of any non-required field should translate to default values for those field.
    /// </summary>
    internal class MsalAppMetadataCacheItem : MsalItemWithAdditionalFields, IEquatable<MsalAppMetadataCacheItem>
    {
        public MsalAppMetadataCacheItem(string clientId, string preferredCacheEnv, string familyId, bool isClientIdCaseSensitive = false)
        {
            ClientId = clientId;
            IsClientIdCaseSensitive = isClientIdCaseSensitive;
            Environment = preferredCacheEnv;
            FamilyId = familyId;

            InitCacheKey();
        }

        private void InitCacheKey()
        {
            CacheKey = $"{StorageJsonKeys.AppMetadata}{MsalCacheKeys.CacheKeyDelimiter}{Environment}{MsalCacheKeys.CacheKeyDelimiter}".ToLowerInvariant() +
                (IsClientIdCaseSensitive ? ClientId : ClientId?.ToLowerInvariant());

            iOSCacheKeyLazy = new Lazy<IiOSKey>(InitiOSKey);
        }

     #region iOS

        private IiOSKey InitiOSKey()
        {
            string iOSService = $"{StorageJsonValues.AppMetadata}{MsalCacheKeys.CacheKeyDelimiter}".ToLowerInvariant() +
                (IsClientIdCaseSensitive ? ClientId : ClientId?.ToLowerInvariant());

            string iOSGeneric = "1";

            string iOSAccount = $"{Environment}".ToLowerInvariant();

            int iOSType = (int)MsalCacheKeys.iOSCredentialAttrType.AppMetadata;

            return new IosKey(iOSAccount, iOSService, iOSGeneric, iOSType);
        }

    #endregion

    /// <remarks>mandatory</remarks>
    public string ClientId { get; }
        internal bool IsClientIdCaseSensitive { get; }

        /// <remarks>mandatory</remarks>

        public string Environment { get; }

        /// <summary>
        /// The family id of which this application is part of. This is an internal feature and there is currently a single app,
        /// with id 1. If familyId is empty, it means an app is not part of a family. A missing entry means unknown status.
        /// </summary>
        public string FamilyId { get; }
        public string CacheKey { get; private set; }

        private Lazy<IiOSKey> iOSCacheKeyLazy;
        public IiOSKey iOSCacheKey => iOSCacheKeyLazy.Value;

        internal MsalAppMetadataCacheItem CloneForSnapshot()
        {
            return new MsalAppMetadataCacheItem(
                ClientId ?? "", Environment ?? "", FamilyId ?? "", IsClientIdCaseSensitive)
            {
                AdditionalFieldsJson = AdditionalFieldsJson
            };
        }

        internal static MsalAppMetadataCacheItem FromJsonString(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return FromJObject(JsonHelper.ParseIntoJsonObject(json));
        }

        internal static MsalAppMetadataCacheItem FromJObject(JObject j, bool isClientIdCaseSensitive = false)
        {
            string clientId = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.ClientId);
            string environment = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.Environment);
            string familyId = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.FamilyId);
            bool storedCaseSensitivity = JsonHelper.ExtractExistingOrDefault<bool>(j, StorageJsonKeys.ClientIdCaseSensitive);

            var item = new MsalAppMetadataCacheItem(clientId, environment, familyId, isClientIdCaseSensitive || storedCaseSensitivity);

            item.PopulateFieldsFromJObject(j);

            item.InitCacheKey();

            return item;
        }

        internal string ToJsonString()
        {
            return ToJObject()
                .ToString();
        }

        internal override JObject ToJObject()
        {
            var json = base.ToJObject();

            SetItemIfValueNotNull(json, StorageJsonKeys.Environment, Environment);
            SetItemIfValueNotNull(json, StorageJsonKeys.ClientId, ClientId);
            if (IsClientIdCaseSensitive)
            {
                json[StorageJsonKeys.ClientIdCaseSensitive] = true;
            }
            SetItemIfValueNotNull(json, StorageJsonKeys.FamilyId, FamilyId);

            return json;
        }

        #region Equals and GetHashCode

        public override int GetHashCode()
        {
            var hashCode = -1793347351;
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(ClientId);
            hashCode = (hashCode * -1521134295) + IsClientIdCaseSensitive.GetHashCode();
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Environment);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(FamilyId);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(AdditionalFieldsJson);

            return hashCode;
        }

        public bool Equals(MsalAppMetadataCacheItem other)
        {
            return ClientId == other.ClientId &&
                   IsClientIdCaseSensitive == other.IsClientIdCaseSensitive &&
                   Environment == other.Environment &&
                   FamilyId == other.FamilyId &&
                   base.AdditionalFieldsJson == other.AdditionalFieldsJson;
        }
        public override bool Equals(object obj)
        {
            return obj is MsalAppMetadataCacheItem item &&
                Equals(item);

        }
        #endregion
    }
}
