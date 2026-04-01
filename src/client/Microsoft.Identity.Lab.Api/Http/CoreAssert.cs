// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Lab.Api.Core.Mocks
{
    /// <summary>
    /// Provides self-contained assertion helpers for MSAL.NET test scenarios.
    /// Throws <see cref="InvalidOperationException"/> on failures instead of depending
    /// on a test framework.
    /// </summary>
    internal static class CoreAssert
    {
        /// <summary>
        /// Asserts that two scope strings represent the same set of scopes.
        /// </summary>
        public static void AreScopesEqual(string scopesExpected, string scopesActual)
        {
            var expectedScopes = ScopeHelper.ConvertStringToScopeSet(scopesExpected);
            var actualScopes = ScopeHelper.ConvertStringToScopeSet(scopesActual);

            if (expectedScopes.Count != actualScopes.Count)
            {
                throw new InvalidOperationException(
                    $"Scope count mismatch. Expected {expectedScopes.Count} scope(s) but found {actualScopes.Count}. " +
                    $"Expected: '{scopesExpected}'. Actual: '{scopesActual}'.");
            }

            foreach (string scope in expectedScopes)
            {
                if (!actualScopes.Contains(scope))
                {
                    throw new InvalidOperationException(
                        $"Expected scope '{scope}' was not found in the actual scopes '{scopesActual}'.");
                }
            }
        }

        /// <summary>
        /// Throws if <paramref name="actual"/> does not equal <paramref name="expected"/>.
        /// </summary>
        public static void AreEqual<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    message ?? $"Expected '{expected}' but got '{actual}'.");
            }
        }

        /// <summary>
        /// Throws if <paramref name="condition"/> is false.
        /// </summary>
        public static void IsTrue(bool condition, string message = null)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message ?? "Expected condition to be true but it was false.");
            }
        }

        /// <summary>
        /// Throws if <paramref name="condition"/> is true.
        /// </summary>
        public static void IsFalse(bool condition, string message = null)
        {
            if (condition)
            {
                throw new InvalidOperationException(message ?? "Expected condition to be false but it was true.");
            }
        }

        /// <summary>
        /// Throws if the collection is not empty.
        /// </summary>
        public static void IsEmpty<T>(IEnumerable<T> collection, string message = null)
        {
            if (collection != null && collection.Any())
            {
                throw new InvalidOperationException(
                    message ?? $"Expected empty collection but found elements: {string.Join(", ", collection)}.");
            }
        }

        /// <summary>
        /// Throws if <paramref name="collection"/> does not contain exactly <paramref name="expectedCount"/> elements.
        /// </summary>
        public static void HasCount<T>(int expectedCount, IEnumerable<T> collection, string message = null)
        {
            int actual = collection?.Count() ?? 0;
            if (actual != expectedCount)
            {
                throw new InvalidOperationException(
                    message ?? $"Expected {expectedCount} element(s) but found {actual}.");
            }
        }
    }
}
