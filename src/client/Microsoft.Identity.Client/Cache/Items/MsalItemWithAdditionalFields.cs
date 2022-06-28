// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text.Json.Nodes;

namespace Microsoft.Identity.Client.Cache.Items
{
    internal abstract class MsalItemWithAdditionalFields
    {
        internal string AdditionalFieldsJson { get; set; } = "{}";

        /// <remarks>
        ///Important: order matters.  This MUST be the last one called since it will extract the
        /// remaining fields out.
        /// </remarks>
        internal virtual void PopulateFieldsFromJObject(JsonObject j)
        {
            AdditionalFieldsJson = j.ToString();
        }

        
        internal virtual JsonObject ToJObject()
        {
            var json = string.IsNullOrWhiteSpace(AdditionalFieldsJson) ? new JsonObject() : JsonNode.Parse(AdditionalFieldsJson).AsObject();

            return json;
        }

        internal void SetItemIfValueNotNull(JsonObject json, string key, JsonNode value)
        {
            SetValueIfFilterMatches(json, key, value, strVal => !string.IsNullOrEmpty(strVal));
        }

        internal void SetItemIfValueNotNullOrDefault(JsonObject json, string key, JsonNode value, string defaultValue)
        {
            SetValueIfFilterMatches(json, key, value, strVal => !string.IsNullOrEmpty(strVal) &&
                        !strVal.Equals(defaultValue, StringComparison.OrdinalIgnoreCase));
        }

        private static void SetValueIfFilterMatches(JsonObject json, string key, JsonNode value, Func<string, bool> filter)
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
