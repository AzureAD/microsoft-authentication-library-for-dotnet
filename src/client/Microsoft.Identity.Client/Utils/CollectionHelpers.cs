// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

#if HAVE_METHOD_IMPL_ATTRIBUTE
using System.Runtime.CompilerServices;
#endif

namespace Microsoft.Identity.Client.Utils
{
    internal static class CollectionHelpers
    {
#if HAVE_METHOD_IMPL_ATTRIBUTE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static IReadOnlyList<T> GetEmptyReadOnlyList<T>()
        {
            return Array.Empty<T>();
        }

#if HAVE_METHOD_IMPL_ATTRIBUTE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static List<T> GetEmptyList<T>()
        {
            return new List<T>();
        }

        public static IReadOnlyDictionary<TKey, TValue> GetEmptyDictionary<TKey, TValue>()
        {
#if NETCOREAPP
            return System.Collections.Immutable.ImmutableDictionary<TKey, TValue>.Empty;
#else
            return new Dictionary<TKey, TValue>();
#endif
        }

        public static bool AreDictionariesEqual(IDictionary<string, string> dict1, IDictionary<string, string> dict2)
        {
            if (dict1 == null || dict2 == null)
            {
                return dict1 == dict2;
            }

            if (dict1.Count != dict2.Count)
            {
                return false;
            }

            foreach (var kvp in dict1)
            {
                if (!dict2.TryGetValue(kvp.Key, out var value) || !string.Equals(kvp.Value, value))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
