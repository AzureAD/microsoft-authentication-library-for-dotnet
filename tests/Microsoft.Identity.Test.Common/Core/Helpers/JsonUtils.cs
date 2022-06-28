// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    public static class JsonTestUtils
    {
        public static string AddKeyValue(string json, string key, string value)
        {
            var jobj = JsonNode.Parse(json).AsObject();
            jobj[key] = value;

            return jobj.ToString();
        }

        public static void AssertJsonDeepEquals(string expectedJson, string actualJson)
        {
            var expected = JsonNode.Parse(expectedJson);
            var actual = JsonNode.Parse(actualJson);

            if (!DeepEquals(expected, actual))
            {
                Assert.Fail($"The 2 JSON strings are not the same. Expected {expectedJson} Actual {actualJson}");
            }
        }

        public static bool DeepEquals(JsonNode a, JsonNode b)
        {
            if(a == null && b == null)
            {
                return true;
            }
            else if (a is JsonValue aVal && b is JsonValue bVal)
            {
                return aVal.ToJsonString() == bVal.ToJsonString();
            }
            else if (a is JsonObject aObj && b is JsonObject bObj)
            {
                return ObjectEquals(aObj, bObj);
            }
            else if (a is JsonArray aArray && b is JsonArray bArray)
            {
                return ArrayEquals(aArray, bArray);
            }

            return false;
        }

        private static bool ArrayEquals(JsonArray arr1, JsonArray arr2)
        {
            if (arr1.Count != arr2.Count)
                return false;

            for (int i = 0; i < arr1.Count; i++)
            {
                var item1 = arr1[i];
                var item2 = arr2[i];
                if (!DeepEquals(item1, item2))
                    return false;
            }
            return true;
        }

        private static bool ObjectEquals(JsonObject obj1, JsonObject obj2)
        {
            if (obj1.Count != obj2.Count)
                return false;

            for (int i = 0; i < obj1.Count; i++)
            {
                var keyPair = obj1.ElementAt(i);
                var item1 = obj1[keyPair.Key];
                if(!obj2.ContainsKey(keyPair.Key))
                {
                    return false;
                }

                var item2 = obj2[keyPair.Key];
                if (!DeepEquals(item1, item2))
                    return false;
            }
            return true;
        }
    }
}
