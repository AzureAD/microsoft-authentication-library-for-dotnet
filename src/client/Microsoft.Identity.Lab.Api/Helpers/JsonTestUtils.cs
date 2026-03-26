// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// JsonTestUtils provides utility methods for working with JSON in test scenarios, such as adding key-value pairs to JSON strings and asserting deep equality of JSON structures. These utilities help simplify common JSON manipulation and comparison tasks in tests, improving readability and maintainability of test code that involves JSON data.
    /// </summary>
    public static class JsonTestUtils
    {
        /// <summary>
        /// Adds a key-value pair to the specified JSON string.
        /// </summary>
        /// <param name="json">The JSON string to modify.</param>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to add.</param>
        /// <returns>The modified JSON string.</returns>    
        public static string AddKeyValue(string json, string key, string value)
        {
            JObject jobj = JObject.Parse(json);
            jobj[key] = value;

            return jobj.ToString();
        }

        /// <summary>
        /// Asserts that two JSON strings are deeply equal.
        /// </summary>
        /// <param name="expectedJson">The expected JSON string.</param>
        /// <param name="actualJson">The actual JSON string.</param>
        public static void AssertJsonDeepEquals(string expectedJson, string actualJson)
        {
            JToken expected = JToken.Parse(expectedJson);
            JToken actual = JToken.Parse(actualJson);

            if (!JToken.DeepEquals(expected, actual))
            {
                ValidationHelpers.AssertFail($"The 2 JSON strings are not the same. Expected {expectedJson} Actual {actualJson}");
            }
        }
    }
}
