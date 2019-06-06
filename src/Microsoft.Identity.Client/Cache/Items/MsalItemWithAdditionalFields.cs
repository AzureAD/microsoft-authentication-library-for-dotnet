// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Cache.Items
{
    internal abstract class MsalItemWithAdditionalFields
    {
        internal string AdditionalFieldsJson { get; set; } = "{}";

        /// <remarks>
        ///Important: order matters.  This MUST be the last one called since it will extract the
        /// remaining fields out.
        /// </remarks>
        internal virtual void PopulateFieldsFromJObject(JObject j)
        {
            AdditionalFieldsJson = j.ToString();
        }

        
        internal virtual JObject ToJObject()
        {
            var json = string.IsNullOrWhiteSpace(AdditionalFieldsJson) ? new JObject() : JObject.Parse(AdditionalFieldsJson);

            return json;
        }

        internal void SetItemIfValueNotNull(JObject json, string key, JToken value)
        {
            if (!value.IsNullOrEmpty())
            {
                json[key] = value;
            }
        }
    }
}
