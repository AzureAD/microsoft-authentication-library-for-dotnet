// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.CacheV2.Impl.Utils
{
    internal static class ScopeUtils
    {
        public static HashSet<string> SplitScopes(string kvpKey)
        {
            return new HashSet<string>(kvpKey.Split(' '));
        }

        public static string JoinScopes(ISet<string> scopes)
        {
            return string.Join(" ", scopes);
        }
    }
}
