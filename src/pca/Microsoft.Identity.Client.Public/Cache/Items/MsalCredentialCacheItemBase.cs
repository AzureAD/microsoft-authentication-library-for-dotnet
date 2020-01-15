// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Cache.Items
{
    internal class MsalCredentialCacheItemBase : MsalCacheItemBase
    {
        internal string CredentialType { get; set; }
        public string ClientId { get; set; }
        public string Secret { get; set; }

        internal override void PopulateFieldsFromJObject(JObject j)
        {
            CredentialType = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.CredentialType);
            ClientId = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.ClientId);
            Secret = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.Secret);

            // Important: this MUST be last, since it will extract the AdditionalFieldsJson
            // after all other fields are read.
            base.PopulateFieldsFromJObject(j);
        }

        internal override JObject ToJObject()
        {
            var json = base.ToJObject();

            SetItemIfValueNotNull(json, StorageJsonKeys.ClientId, ClientId);
            SetItemIfValueNotNull(json, StorageJsonKeys.Secret, Secret);
            SetItemIfValueNotNull(json, StorageJsonKeys.CredentialType, CredentialType);

            return json;
        }
    }
}
