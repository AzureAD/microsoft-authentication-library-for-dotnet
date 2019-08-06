// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Cache.Items
{
    internal class MsalWamAccountCacheItem : MsalItemWithAdditionalFields
    {
        internal MsalWamAccountCacheItem()
        {
        }

        internal MsalWamAccountCacheItem(IAccount account)
        {
            Username = account.Username;
            Environment = account.Environment;
            WamAccountId = account.HomeAccountId.Identifier;
        }

        internal string Username { get; set; }
        internal string Environment { get; set; }
        internal string WamAccountId { get; set; }

        internal MsalWamAccountCacheKey GetKey()
        {
            return new MsalWamAccountCacheKey(Environment, WamAccountId);
        }

        internal static MsalWamAccountCacheItem FromJObject(JObject j)
        {
            var item = new MsalWamAccountCacheItem
            {
                Username = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.Username),
                Environment = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.Environment),
                WamAccountId = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.WamAccountId),
            };

            item.PopulateFieldsFromJObject(j);

            return item;
        }

        internal override JObject ToJObject()
        {
            var json = base.ToJObject();

            SetItemIfValueNotNull(json, StorageJsonKeys.Username, Username);
            SetItemIfValueNotNull(json, StorageJsonKeys.Environment, Environment);
            SetItemIfValueNotNull(json, StorageJsonKeys.WamAccountId, WamAccountId);

            return json;
        }
    }
}
