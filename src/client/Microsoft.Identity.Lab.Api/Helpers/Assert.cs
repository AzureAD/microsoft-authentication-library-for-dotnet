// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Identity.Lab.Api.Helpers
{
    /// <summary>
    /// Lightweight assertion helper that throws <see cref="InvalidOperationException"/>
    /// instead of depending on MSTest. Intended for use in mock infrastructure only.
    /// </summary>
    internal static class Assert
    {
        public static void AreEqual<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    message ?? $"Assert.AreEqual failed. Expected: <{expected}>. Actual: <{actual}>.");
            }
        }

        public static void IsTrue(bool condition, string message = null)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    message ?? "Assert.IsTrue failed.");
            }
        }

        public static void IsFalse(bool condition, string message = null)
        {
            if (condition)
            {
                throw new InvalidOperationException(
                    message ?? "Assert.IsFalse failed.");
            }
        }

        public static void IsNotNull(object value, string message = null)
        {
            if (value == null)
            {
                throw new InvalidOperationException(
                    message ?? "Assert.IsNotNull failed.");
            }
        }

        public static void IsEmpty<T>(ICollection<T> collection, string message = null)
        {
            if (collection != null && collection.Count > 0)
            {
                throw new InvalidOperationException(
                    message ?? $"Assert.IsEmpty failed. Collection has {collection.Count} element(s).");
            }
        }

        public static void IsEmpty<T>(IEnumerable<T> collection, string message = null)
        {
            if (collection != null && collection.Any())
            {
                throw new InvalidOperationException(
                    message ?? "Assert.IsEmpty failed. Collection is not empty.");
            }
        }

        public static void HasCount<T>(int expectedCount, ICollection<T> collection, string message = null)
        {
            int actual = collection?.Count ?? 0;
            if (actual != expectedCount)
            {
                throw new InvalidOperationException(
                    message ?? $"Assert.HasCount failed. Expected: {expectedCount}. Actual: {actual}.");
            }
        }

        public static void Contains<T>(T item, ICollection<T> collection, string message = null)
        {
            if (collection == null || !collection.Contains(item))
            {
                throw new InvalidOperationException(
                    message ?? $"Assert.Contains failed. Item <{item}> not found in collection.");
            }
        }

        public static void Fail(string message)
        {
            throw new InvalidOperationException(message ?? "Assert.Fail was called.");
        }
    }
}
