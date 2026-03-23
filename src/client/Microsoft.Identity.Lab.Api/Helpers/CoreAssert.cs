// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// Provides custom assertion helpers for MSAL.NET test scenarios.
    /// </summary>
    public static class CoreAssert
    {
        /// <summary>
        /// Asserts that two scope strings represent the same set of scopes.
        /// </summary>
        /// <param name="scopesExpected">The expected scopes, formatted as a single string (e.g. space-delimited).</param>
        /// <param name="scopesActual">The actual scopes, formatted as a single string (e.g. space-delimited).</param>
        /// <remarks>
        /// Scope comparison is performed as a set comparison (order-insensitive).
        /// </remarks>
        public static void AreScopesEqual(string scopesExpected, string scopesActual)
        {
            var expectedScopes = ScopeHelper.ConvertStringToScopeSet(scopesExpected);
            var actualScopes = ScopeHelper.ConvertStringToScopeSet(scopesActual);

            // can't use Assert.AreEqual on HashSet, so we'll compare by hand.
            ValidationHelpers.AssertHasCount(expectedScopes.Count, actualScopes);
            foreach (string expectedScope in expectedScopes)
            {
                ValidationHelpers.AssertContains(expectedScope, actualScopes);
            }
        }

        internal static void AreAccountsEqual(
            string expectedUsername,
            string expectedEnv,
            string expectedId,
            string expectedTid,
            string expectedOid,
            params IAccount[] accounts)
        {
            foreach (var account in accounts)
            {
                ValidationHelpers.AssertAreEqual(expectedUsername, account.Username);
                ValidationHelpers.AssertAreEqual(expectedEnv, account.Environment);
                ValidationHelpers.AssertAreEqual(expectedId, account.HomeAccountId.Identifier);
                ValidationHelpers.AssertAreEqual(expectedTid, account.HomeAccountId.TenantId);
                ValidationHelpers.AssertAreEqual(expectedOid, account.HomeAccountId.ObjectId);
            }
        }

        /// <summary>
        /// Asserts that three values are all equal to each other.
        /// </summary>
        /// <typeparam name="T">The type of values to compare.</typeparam>
        /// <param name="val1">The first value (used as the reference).</param>
        /// <param name="val2">The second value.</param>
        /// <param name="val3">The third value.</param>
        public static void AreEqual<T>(T val1, T val2, T val3)
        {
            ValidationHelpers.AssertAreEqual(val1, val2, "First and second values differ");
            ValidationHelpers.AssertAreEqual(val1, val3, "First and third values differ");
        }

        /// <summary>
        /// Asserts that two <see cref="DateTimeOffset"/> values are within one second of each other.
        /// </summary>
        /// <param name="expected">The expected date/time value.</param>
        /// <param name="actual">The actual date/time value.</param>
        /// <param name="message">Optional prefix message to include in the assertion failure.</param>
        public static void AreWithinOneSecond(DateTimeOffset expected, DateTimeOffset actual, string message = "")
        {
            IsWithinRange(expected, actual, TimeSpan.FromSeconds(1), message);
        }

        /// <summary>
        /// Asserts that two <see cref="DateTimeOffset"/> values are within a specified range of each other.
        /// </summary>
        /// <param name="expected">The expected date/time value.</param>
        /// <param name="actual">The actual date/time value.</param>
        /// <param name="range">The allowable difference between <paramref name="expected"/> and <paramref name="actual"/>.</param>
        /// <param name="message">Optional prefix message to include in the assertion failure.</param>
        public static void IsWithinRange(DateTimeOffset expected, DateTimeOffset actual, TimeSpan range, string message = "")
        {
            TimeSpan t = expected - actual;
            ValidationHelpers.AssertIsTrue(t >= -range && t <= range,
                $"{message} The dates are off by {t.TotalMilliseconds}ms, which is more than the expected {range.TotalMilliseconds}ms");
        }

        /// <summary>
        /// Asserts that two dictionaries are equal.
        /// </summary>
        /// <typeparam name="TKey">The dictionary key type.</typeparam>
        /// <typeparam name="TValue">The dictionary value type.</typeparam>
        /// <param name="dict1">The first dictionary.</param>
        /// <param name="dict2">The second dictionary.</param>
        /// <param name="valueComparer">Comparer used to evaluate value equality.</param>
        /// <remarks>
        /// Dictionary equality is based on: same count, same keys, and values comparing equal via <paramref name="valueComparer"/>.
        /// </remarks>
        public static void AssertDictionariesAreEqual<TKey, TValue>(
          IDictionary<TKey, TValue> dict1,
          IDictionary<TKey, TValue> dict2,
          IEqualityComparer<TValue> valueComparer)
        {
            ValidationHelpers.AssertIsTrue(DictionariesAreEqual(dict1, dict2, valueComparer));
        }

        /// <summary>
        /// Asserts that the specified type is immutable.
        /// </summary>
        /// <typeparam name="T">The type to validate for immutability.</typeparam>
        /// <remarks>
        /// A type is considered immutable if:
        /// - It is a primitive, enum, or string; or
        /// - All instance fields are readonly and their field types are also immutable (recursive check).
        /// </remarks>
        public static void IsImmutable<T>()
        {
            ValidationHelpers.AssertIsTrue(IsImmutable(typeof(T)));
        }

        private static bool IsImmutable(Type type)
        {
            if (type == typeof(string) || type.IsPrimitive || type.IsEnum)
            {
                return true;
            }

            // Suppress trim-analysis warnings for this test-only helper because it uses
            // recursive reflection over arbitrary runtime types to inspect instance fields.
            // This pattern is intentional here and is not intended to be trim-safe.
            #pragma warning disable IL2070, IL2067
            var fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            #pragma warning restore IL2070, IL2067
            var isShallowImmutable = fieldInfos.All(f => f.IsInitOnly);

            if (!isShallowImmutable)
            {
                return false;
            }

            return fieldInfos.All(f => IsImmutable(f.FieldType));
        }

        private static bool DictionariesAreEqual<TKey, TValue>(
            IDictionary<TKey, TValue> dict1,
            IDictionary<TKey, TValue> dict2,
            IEqualityComparer<TValue> valueComparer)
        {
            if (dict1 == dict2)
                return true;
            if ((dict1 == null) || (dict2 == null))
                return false;
            if (dict1.Count != dict2.Count)
                return false;

            foreach (var kvp in dict1)
            {
                if (!dict2.TryGetValue(kvp.Key, out TValue value2))
                    return false;
                if (!valueComparer.Equals(kvp.Value, value2))
                    return false;
            }
            return true;
        }
    }
}
