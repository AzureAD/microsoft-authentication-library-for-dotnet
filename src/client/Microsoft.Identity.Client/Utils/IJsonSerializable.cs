// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Utils
{
    internal interface IJsonSerializable<T>
    {
        string SerializeToJson();
        T DeserializeFromJson(string json);
    }
}
