// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Cache
{
    internal interface ITokenCacheSerializable
    {
        IDictionary<string, JToken> Deserialize(byte[] bytes, bool clearExistingCacheData);
        byte[] Serialize(IDictionary<string, JToken> additionalNodes);
    }
}
