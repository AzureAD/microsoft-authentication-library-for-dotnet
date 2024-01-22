// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    public static class CoreAssert
    {
        public static void AreScopesEqual(string scopesExpected, string scopesActual)
        {
            var expectedScopes = ScopeHelper.ConvertStringToScopeSet(scopesExpected);
            var actualScopes = ScopeHelper.ConvertStringToScopeSet(scopesActual);

            // can't use Assert.AreEqual on HashSet, so we'll compare by hand.
            Assert.AreEqual(expectedScopes.Count, actualScopes.Count);
            foreach (string expectedScope in expectedScopes)
            {
                Assert.IsTrue(actualScopes.Contains(expectedScope));
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
                Assert.AreEqual(expectedUsername, account.Username);
                Assert.AreEqual(expectedEnv, account.Environment);
                Assert.AreEqual(expectedId, account.HomeAccountId.Identifier);
                Assert.AreEqual(expectedTid, account.HomeAccountId.TenantId);
                Assert.AreEqual(expectedOid, account.HomeAccountId.ObjectId);
            }
        }

        public static void AreEqual<T>(T val1, T val2, T val3)
        {
            Assert.AreEqual(val1, val2, "First and second values differ");
            Assert.AreEqual(val1, val3, "First and third values differ");
        }

        public static void AreWithinOneSecond(DateTimeOffset expected, DateTimeOffset actual, string message = "")
        {
            IsWithinRange(expected, actual, TimeSpan.FromSeconds(1), message);
        }

        public static void IsWithinRange(DateTimeOffset expected, DateTimeOffset actual, TimeSpan range, string message = "")
        {
            TimeSpan t = expected - actual;
            Assert.IsTrue(t >= -range && t <= range,
                $"{message} The dates are off by {t.TotalMilliseconds}ms, which is more than the expected {range.TotalMilliseconds}ms");
        }

        public static void AssertDictionariesAreEqual<TKey, TValue>(
          IDictionary<TKey, TValue> dict1,
          IDictionary<TKey, TValue> dict2,
          IEqualityComparer<TValue> valueComparer)
        {
            Assert.IsTrue(DictionariesAreEqual(dict1, dict2, valueComparer));
        }

        public static void IsImmutable<T>()
        {
            Assert.IsTrue(IsImmutable(typeof(T)));
        }

        private static bool IsImmutable(Type type)
        {
            if (type == typeof(string) || type.IsPrimitive || type.IsEnum)
            {
                return true;
            }

            var fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var isShallowImmutable = fieldInfos.All(f => f.IsInitOnly);
            
            if (!isShallowImmutable)
            {
                return false;
            }

            var isDeepImmutable = fieldInfos.All(f => IsImmutable(f.FieldType));
            return isDeepImmutable;
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
