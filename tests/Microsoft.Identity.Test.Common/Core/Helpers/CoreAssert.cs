// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute.Routing.AutoValues;

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

        public static void AreEqual<T>(T val1, T val2, T val3)
        {
            Assert.AreEqual(val1, val2, "First and second values differ");
            Assert.AreEqual(val1, val3, "First and third values differ");
        }

        public static void AreEqual(DateTimeOffset expected, DateTimeOffset actual, TimeSpan delta)
        {
            TimeSpan t = expected - actual;
            Assert.IsTrue(t < delta, 
                $"The dates are off by {t.TotalMilliseconds}ms, which is more than the expected {delta.TotalMilliseconds}ms");
        }

        public static void AssertDictionariesAreEqual<TKey, TValue>(
          IDictionary<TKey, TValue> dict1,
          IDictionary<TKey, TValue> dict2,
          IEqualityComparer<TValue> valueComparer)
        {
            Assert.IsTrue(DictionariesAreEqual(dict1, dict2, valueComparer));
        }

        public static void InterfaceExposesObject(Type theObj, Type theInterface, string[] except = null)
        {
            Func<PropertyInfo, IEnumerable<MethodInfo>> propertyToMethodSelector = m =>
            {
                var ret = new List<MethodInfo>();
                if (m.CanRead)
                    ret.Add(m.GetMethod);
                if (m.CanWrite)
                    ret.Add(m.SetMethod);

                return ret;
            };

            var objectMethods = theObj.GetMethods(
              BindingFlags.Public |
              BindingFlags.Instance |
              BindingFlags.DeclaredOnly)
                .Where(o => o.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(o => !o.IsSpecialName) // ignore properties
                .Select(m => m.ToString()).ToList();

            var objProps = theObj.GetProperties(
              BindingFlags.Public |
              BindingFlags.Instance |
              BindingFlags.DeclaredOnly)
                .Where(o => o.GetCustomAttribute<ObsoleteAttribute>() == null)
                .SelectMany(propertyToMethodSelector)
                .Select(m => m.ToString()).ToList();

            var interfaceMethods = theInterface.GetMethods()
                .Where(o => o.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(o => !o.IsSpecialName) // ignore properties
                .Select(m => m.ToString())
                .ToList();

            var interfaceProps = theInterface.GetProperties()
               .Where(o => o.GetCustomAttribute<ObsoleteAttribute>() == null)
               .SelectMany(propertyToMethodSelector)
               .Select(m => m.ToString()).ToList();

            var missingMethods =
                objectMethods
                    .Except(interfaceMethods)
                    .Except(except ?? new string[0]);

            var missingProperties =
              objProps
                  .Except(interfaceProps)
                  .Except(except ?? new string[0]);


            Assert.IsTrue(missingMethods.Count() == 0,
                "Expecting the object and the interface to have the same public methods." +
                " Methods on the object not found on the interface are: " +
                string.Join(" ", missingMethods));

            Assert.IsTrue(missingMethods.Count() == 0,
                "Expecting the object and the interface to have the same public properties." +
                "Properties on the object not found on the interface are: " +
                string.Join(" ", missingMethods));
        }

        public static void IsImmutable<T>()
        {
            Assert.IsTrue(IsImmutable(typeof(T)));
        }

        private static bool IsImmutable(Type type)
        {
            if (type == typeof(string) || type.GetTypeInfo().IsPrimitive || type.GetTypeInfo().IsEnum)
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
                TValue value2;
                if (!dict2.TryGetValue(kvp.Key, out value2))
                    return false;
                if (!valueComparer.Equals(kvp.Value, value2))
                    return false;
            }
            return true;
        }
    }
}
