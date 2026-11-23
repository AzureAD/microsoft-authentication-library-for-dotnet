// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.Identity.Client.Cache
{
    internal interface ITokenCacheSerializable
    {
        IDictionary<string, JsonElement> Deserialize(byte[] bytes, bool clearExistingCacheData);
        byte[] Serialize(IDictionary<string, JsonElement> additionalNodes);
    }
}
