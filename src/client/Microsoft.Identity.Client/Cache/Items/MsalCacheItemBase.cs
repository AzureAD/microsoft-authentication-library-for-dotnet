// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Cache.Items
{
    internal abstract class MsalCacheItemBase : MsalItemWithAdditionalFields
    {
        internal string HomeAccountId { get; set; }
        internal string Environment { get; set; }
        internal string RawClientInfo { get; set; }

        internal override void PopulateFieldsFromJObject(JsonObject j)
        {
            HomeAccountId = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.HomeAccountId);
            Environment = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.Environment);
            RawClientInfo = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.ClientInfo);

            // Important: order matters.  This MUST be the last one called since it will extract the
            // remaining fields out.
            base.PopulateFieldsFromJObject(j);
        }

        internal override JsonObject ToJObject()
        {
            var json = base.ToJObject();
            SetItemIfValueNotNull(json, StorageJsonKeys.HomeAccountId, HomeAccountId);
            SetItemIfValueNotNull(json, StorageJsonKeys.Environment, Environment);
            SetItemIfValueNotNull(json, StorageJsonKeys.ClientInfo, RawClientInfo);

            return json;
        }
    }
}
