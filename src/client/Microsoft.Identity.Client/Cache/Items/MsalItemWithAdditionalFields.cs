// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Utils;
using System.Text.Json.Nodes;
using JObject = System.Text.Json.Nodes.JsonObject;
using JToken = System.Text.Json.Nodes.JsonNode;

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

            var asObj = value as JsonValue;

            if (asObj == null)
            {
                shouldSetValue = false;
            }
            else
            {
                string asString = asObj.GetValue<string>();
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
