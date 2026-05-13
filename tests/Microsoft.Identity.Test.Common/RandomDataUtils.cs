// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// RandomDataUtils provides utility methods for generating random byte arrays of a specified size. This can be useful in tests that require random data for input, such as testing encryption, hashing, or any functionality that operates on byte arrays. The GetRandomData method uses the Random class to fill a byte array with random values and returns it to the caller.
    /// </summary>
    public static class RandomDataUtils
    {
        private static readonly Random Random = new Random();

        /// <summary>
        /// Gets a byte array of the specified size filled with random data. The method creates a new byte array of the given size, uses the Random instance to fill it with random bytes, and then returns the populated byte array to the caller.
        /// </summary>
        /// <param name="size">The size of the byte array to generate.</param>
        /// <returns>A byte array filled with random data.</returns>
        public static byte[] GetRandomData(int size)
        {
            var data = new byte[size];
            Random.NextBytes(data);
            return data;
        }
    }
}
