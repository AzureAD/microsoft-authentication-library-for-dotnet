// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Cache.Items
{
    internal class MsalRefreshTokenCacheItem : MsalCredentialCacheItemBase
    {
        internal MsalRefreshTokenCacheItem()
        {
            CredentialType = StorageJsonValues.CredentialTypeRefreshToken;
        }

        internal MsalRefreshTokenCacheItem(
            string environment,
            string clientId,
            MsalTokenResponse response)
            : this(environment, clientId, response.RefreshToken, response.ClientInfo, response.FamilyId)
        {
        }

        internal MsalRefreshTokenCacheItem(
            string environment,
            string clientId,
            string secret,
            string rawClientInfo,
            string familyId = null)
            : this()
        {
            ClientId = clientId;
            Environment = environment;
            Secret = secret;
            RawClientInfo = rawClientInfo;
            FamilyId = familyId;

            InitUserIdentifier();
        }

        /// <summary>
        /// Optional. A value here means the token in an FRT.
        /// </summary>
        public string FamilyId { get; set; }

        internal MsalRefreshTokenCacheKey GetKey()
        {
            return new MsalRefreshTokenCacheKey(Environment, ClientId, HomeAccountId, FamilyId);
        }

        internal static MsalRefreshTokenCacheItem FromJsonString(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return FromJObject(JObject.Parse(json));
        }

        internal static MsalRefreshTokenCacheItem FromJObject(JObject j)
        {
            var item = new MsalRefreshTokenCacheItem();
            item.FamilyId = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.FamilyId);

            item.PopulateFieldsFromJObject(j);

            return item;
        }

        internal override JObject ToJObject()
        {
            var json = base.ToJObject();

            json[StorageJsonKeys.FamilyId] = FamilyId;

            return json;
        }

        internal string ToJsonString()
        {
            return ToJObject().ToString();
        }
    }
}
