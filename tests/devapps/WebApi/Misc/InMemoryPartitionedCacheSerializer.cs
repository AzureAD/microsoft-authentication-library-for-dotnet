// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.;

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;

namespace WebApi.Misc
{
    /// <summary>
    /// A simple partitioned cache, useful for Confidential Client flows. 
    /// </summary>
    internal class InMemoryPartitionedCacheSerializer
        : AbstractPartitionedCacheSerializer
    {
        internal /* internal for test only */ ConcurrentDictionary<string, byte[]> CachePartition { get; }
        private readonly ILogger _logger;

        public InMemoryPartitionedCacheSerializer(ILogger logger, ConcurrentDictionary<string, byte[]> dictionary = null)
        {
            CachePartition = dictionary ?? new ConcurrentDictionary<string, byte[]>();
            _logger = logger;
        }

        protected override byte[] ReadCacheBytes(string cacheKey)
        {
            if (CachePartition.TryGetValue(cacheKey, out byte[] blob))
            {
                _logger.LogInformation($"[InMemoryPartitionedTokenCache] ReadCacheBytes found cacheKey {cacheKey}");
                return blob;
            }

            _logger.LogInformation($"[InMemoryPartitionedTokenCache] ReadCacheBytes did not find cacheKey {cacheKey}");

            return null;
        }

        protected override void RemoveKey(string cacheKey)
        {
            bool removed = CachePartition.TryRemove(cacheKey, out _);
            _logger.LogInformation($"[InMemoryPartitionedTokenCache] RemoveKeyAsync cacheKey {cacheKey} success {removed}");
        }

        protected override void WriteCacheBytes(string cacheKey, byte[] bytes)
        {
            // As per https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2?redirectedfrom=MSDN&view=net-5.0#remarks
            // the indexer is ok to store a key/value pair unconditionally
            
            
            _logger.LogInformation($"[InMemoryPartitionedTokenCache] WriteCacheBytes with cacheKey {cacheKey}. Cache partitions: {CachePartition.Count}"); // note: Count is expensive
            
            CachePartition[cacheKey] = bytes;
        }
    }

}

