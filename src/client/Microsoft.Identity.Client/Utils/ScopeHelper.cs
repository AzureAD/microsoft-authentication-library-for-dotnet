// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Utils
{
    internal static class ScopeHelper
    {
        public static bool ScopeContains(ISet<string> outerSet, IEnumerable<string> possibleContainedSet)
        {
            foreach (string key in possibleContainedSet)
            {
                if (!string.IsNullOrEmpty(key) && !outerSet.Contains(key))
                {
                    return false;
                }
            }

            return true;
        }

        public static HashSet<string> GetMsalScopes(HashSet<string> userScopes)
        {
            return new HashSet<string>(userScopes.Concat(OAuth2Value.ReservedScopes));
        }

        private static readonly Char[] SingleSpace = new Char[] { ' ' };

        public static HashSet<string> ConvertStringToScopeSet(string singleString)
        {
            String[] parts = singleString?.Split(SingleSpace, StringSplitOptions.RemoveEmptyEntries);

            return CreateScopeSet(parts);
        }

        public static HashSet<string> CreateScopeSet(IEnumerable<string> input)
        {
            if (input == null)
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            return new HashSet<string>(input.Select(i => i.ToLower()), StringComparer.Ordinal);
        }
    }
}
