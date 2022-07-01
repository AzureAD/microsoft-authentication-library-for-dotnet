// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Identity.Client.Utils
{
    internal static class CollectionHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReadOnlyList<T> GetEmptyReadOnlyList<T>()
        {
            return Array.Empty<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> GetEmptyList<T>()
        {
            return new List<T>();
        }

        public static IDictionary<TKey, TValue> GetEmptyDictionary<TKey, TValue>()
        {
#if NET_CORE
            return System.Collections.Immutable.ImmutableDictionary<TKey, TValue>.Empty;
#else
            return new Dictionary<TKey, TValue>();
#endif
        }
    }
}
