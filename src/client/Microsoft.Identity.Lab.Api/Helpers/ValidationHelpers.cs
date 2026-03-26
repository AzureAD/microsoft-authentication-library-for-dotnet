// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// Provides framework-agnostic assertion helpers that throw <see cref="InvalidOperationException"/>
    /// on failure, removing the dependency on any specific test framework (e.g. MSTest).
    /// </summary>
    public static class ValidationHelpers
    {
        /// <summary>
        /// Asserts that two values are equal.
        /// </summary>
        public static void AssertAreEqual<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"Expected: <{expected}>. Actual: <{actual}>. {message}".Trim());
            }
        }

        /// <summary>
        /// Asserts that the specified object is not null.
        /// </summary>
        public static void AssertIsNotNull(object value, string message = null)
        {
            if (value == null)
            {
                throw new InvalidOperationException(
                    $"Value is null. {message}".Trim());
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> unconditionally with the provided message.
        /// </summary>
        public static void AssertFail(string message)
        {
            throw new InvalidOperationException(message);
        }

        /// <summary>
        /// Asserts that the specified condition is true.
        /// </summary>
        public static void AssertIsTrue(bool condition, string message = null)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    $"Condition is false. {message}".Trim());
            }
        }

        /// <summary>
        /// Asserts that the specified condition is false.
        /// </summary>
        public static void AssertIsFalse(bool condition, string message = null)
        {
            if (condition)
            {
                throw new InvalidOperationException(
                    $"Condition is true but expected false. {message}".Trim());
            }
        }

        /// <summary>
        /// Asserts that the collection is empty.
        /// </summary>
        public static void AssertIsEmpty<T>(IEnumerable<T> collection, string message = null)
        {
            int count = collection is ICollection<T> c ? c.Count : collection?.Count() ?? 0;
            if (count != 0)
            {
                throw new InvalidOperationException(
                    $"Collection is not empty. {message}".Trim());
            }
        }

        /// <summary>
        /// Asserts that the collection has the expected number of elements.
        /// </summary>
        public static void AssertHasCount<T>(int expected, IEnumerable<T> collection, string message = null)
        {
            int actual = collection is ICollection<T> c ? c.Count : collection?.Count() ?? 0;
            if (actual != expected)
            {
                throw new InvalidOperationException(
                    $"Expected count <{expected}> but was <{actual}>. {message}".Trim());
            }
        }

        /// <summary>
        /// Asserts that the collection contains the expected element.
        /// </summary>
        public static void AssertContains<T>(T expected, IEnumerable<T> collection, string message = null)
        {
            if (collection == null || !collection.Contains(expected))
            {
                throw new InvalidOperationException(
                    $"Collection does not contain expected element <{expected}>. {message}".Trim());
            }
        }
    }
}
