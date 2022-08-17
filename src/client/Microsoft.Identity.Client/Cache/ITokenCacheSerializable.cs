// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
#if NET6_0_OR_GREATER
using JToken = System.Text.Json.Nodes.JsonNode;
#else
using Microsoft.Identity.Json.Linq;
#endif

namespace Microsoft.Identity.Client.Cache
{
    internal interface ITokenCacheSerializable
    {
        IDictionary<string, JToken> Deserialize(byte[] bytes, bool clearExistingCacheData);
        byte[] Serialize(IDictionary<string, JToken> additionalNodes);
    }
}
