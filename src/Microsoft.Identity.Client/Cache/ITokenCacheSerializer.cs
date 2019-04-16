// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Cache
{
    internal interface ITokenCacheSerializer
    {
        IDictionary<string, JToken> Deserialize(byte[] bytes);
        byte[] Serialize(IDictionary<string, JToken> additionalNodes);
    }
}
