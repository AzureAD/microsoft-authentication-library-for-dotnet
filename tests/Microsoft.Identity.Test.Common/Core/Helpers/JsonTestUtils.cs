// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

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

        public static void AssertJsonDeepEquals(string expectedJson, string actualJson)
        {
            JToken expected = JToken.Parse(expectedJson);
            JToken actual = JToken.Parse(actualJson);

            if (!JToken.DeepEquals(expected, actual))
            {
                Assert.Fail($"The 2 JSON strings are not the same. Expected {expectedJson} Actual {actualJson}");
            }
        }
    }
}
