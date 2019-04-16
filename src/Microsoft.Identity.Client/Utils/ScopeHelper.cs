// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Identity.Client.Utils
{
    internal static class ScopeHelper
    {
        public static bool ScopeContains(SortedSet<string> outerSet, SortedSet<string> possibleContainedSet)
        {
            foreach (string key in possibleContainedSet)
            {
                if (!outerSet.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        internal static SortedSet<string> ConvertStringToLowercaseSortedSet(string singleString)
        {
            if (string.IsNullOrEmpty(singleString))
            {
                return new SortedSet<string>();
            }

            return new SortedSet<string>(singleString.ToLowerInvariant().Split(new[] { " " }, StringSplitOptions.None));
        }

        internal static SortedSet<string> CreateSortedSetFromEnumerable(IEnumerable<string> input)
        {
            if (input == null || !input.Any())
            {
                return new SortedSet<string>();
            }
            return new SortedSet<string>(input);
        }
    }
}
