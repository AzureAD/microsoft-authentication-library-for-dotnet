// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Identity.Client.Utils
{
    internal static class EnumerableExtensions
    {
        internal static bool IsNullOrEmpty<T>(this IEnumerable<T> input)
        {
            return input == null || !input.Any();
        }

        internal static string AsSingleString(this IEnumerable<string> input)
        {
            if (input.IsNullOrEmpty())
            {
                return string.Empty;
            }

            return string.Join(" ", input);
        }

        internal static bool ContainsOrdinalIgnoreCase(this IEnumerable<string> set, string toLookFor)
        {
            return set.Any(el => el.Equals(toLookFor, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
