// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Utils
{
    internal interface IJsonSerializable<T>
    {
        string SerializeToJson();
        JObject SerializeToJObject();
        T DeserializeFromJson(string json);
        T DeserializeFromJObject(JObject jObject);
    }
}
