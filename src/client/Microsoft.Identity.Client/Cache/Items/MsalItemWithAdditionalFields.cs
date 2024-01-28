// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Utils;
#if SUPPORTS_SYSTEM_TEXT_JSON
using System.Text.Json.Nodes;
using JObject = System.Text.Json.Nodes.JsonObject;
using JToken = System.Text.Json.Nodes.JsonNode;
#else
using Microsoft.Identity.Json.Linq;
#endif

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
            var json = string.IsNullOrWhiteSpace(AdditionalFieldsJson) ? new JObject() : JsonHelper.ParseIntoJsonObject(AdditionalFieldsJson);

            return json;
        }

        internal static void SetItemIfValueNotNull(JObject json, string key, JToken value)
        {
            SetValueIfFilterMatches(json, key, value, strVal => !string.IsNullOrEmpty(strVal));
        }

        internal static void SetItemIfValueNotNullOrDefault(JObject json, string key, JToken value, string defaultValue)
        {
            SetValueIfFilterMatches(json, key, value, strVal => !string.IsNullOrEmpty(strVal) &&
                            !strVal.Equals(defaultValue, StringComparison.OrdinalIgnoreCase));
        }

        private static void SetValueIfFilterMatches(JObject json, string key, JToken value, Func<string, bool> filter)
        {
            bool shouldSetValue = true;

#if SUPPORTS_SYSTEM_TEXT_JSON
            var asObj = value as JsonValue;
#else
            object asObj = value.ToObject<object>();
#endif
            if (asObj == null)
            {
                shouldSetValue = false;
            }
            else
            {
#if SUPPORTS_SYSTEM_TEXT_JSON
                string asString = asObj.GetValue<string>();
#else
                string asString = asObj as string;
#endif
                if (asString != null)
                {
                    shouldSetValue = filter(asString);
                }
            }

            if (shouldSetValue)
            {
                json[key] = value;
            }
        }
    }
}
