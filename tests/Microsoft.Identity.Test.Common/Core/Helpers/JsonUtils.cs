// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    public static class JsonTestUtils
    {
        public static string AddKeyValue(string json, string key, string value)
        {
            JObject jobj = JObject.Parse(json);
            jobj[key] = value;

            return jobj.ToString();
        }
    }
}
