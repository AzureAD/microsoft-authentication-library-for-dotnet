// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.CacheV2.Impl.Utils
{
    internal static class JObjectExtensions
    {
        public static bool IsEmpty(this JObject json)
        {
            return !json.HasValues;
        }
    }
}
