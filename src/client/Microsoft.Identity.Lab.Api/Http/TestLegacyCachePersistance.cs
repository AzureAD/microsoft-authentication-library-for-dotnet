// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Cache;

namespace Microsoft.Identity.Lab.Api.Core.Mocks
{
    /// <summary>
    /// legacy cache persistence implementation for testing purposes. This class provides a simple in-memory storage mechanism for MSAL legacy cache data, allowing tests to simulate caching behavior without relying on external storage or file systems. It implements the <see cref="ILegacyCachePersistence"/> interface, enabling it to be used in scenarios where legacy cache persistence is required for testing MSAL functionality.
    /// </summary>
    public class TestLegacyCachePersistance : ILegacyCachePersistence
    {
        private byte[] data;
        /// <summary>
        /// loads the cache data from the in-memory storage. This method returns the byte array representing the serialized cache data that was previously stored using the <see cref="WriteCache"/> method. It allows tests to retrieve the cached data for validation or further processing as needed.
        /// </summary>
        /// <returns></returns>
        public byte[] LoadCache()
        {
            return data;
        }

        /// <summary>
        /// Writes the cache data to the in-memory storage. This method accepts a byte array representing the serialized cache data and stores it in memory, allowing subsequent calls to <see cref="LoadCache"/> to retrieve the same data.
        /// </summary>
        /// <param name="serializedCache">The serialized cache data to store.</param>
        public void WriteCache(byte[] serializedCache)
        {
            data = serializedCache;
        }
    }
}
