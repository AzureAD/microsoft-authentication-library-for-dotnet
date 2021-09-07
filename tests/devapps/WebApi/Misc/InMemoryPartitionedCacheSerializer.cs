// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.;

using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ICoreLogger _logger;

        public InMemoryPartitionedCacheSerializer(ICoreLogger logger, ConcurrentDictionary<string, byte[]> dictionary = null)
        {
            CachePartition = dictionary ?? new ConcurrentDictionary<string, byte[]>();
            _logger = logger;
        }

        protected override byte[] ReadCacheBytes(string cacheKey)
        {
            if (CachePartition.TryGetValue(cacheKey, out byte[] blob))
            {
                _logger.Verbose($"[InMemoryPartitionedTokenCache] ReadCacheBytes found cacheKey {cacheKey}");
                return blob;
            }

            _logger.Verbose($"[InMemoryPartitionedTokenCache] ReadCacheBytes did not find cacheKey {cacheKey}");

            return null;
        }

        protected override void RemoveKey(string cacheKey)
        {
            bool removed = CachePartition.TryRemove(cacheKey, out _);
            _logger.Verbose($"[InMemoryPartitionedTokenCache] RemoveKeyAsync cacheKey {cacheKey} success {removed}");
        }

        protected override void WriteCacheBytes(string cacheKey, byte[] bytes)
        {
            // As per https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2?redirectedfrom=MSDN&view=net-5.0#remarks
            // the indexer is ok to store a key/value pair unconditionally
            if (_logger.IsLoggingEnabled(LogLevel.Verbose))
            {
                _logger.Verbose($"[InMemoryPartitionedTokenCache] WriteCacheBytes with cacheKey {cacheKey}. Cache partitions: {CachePartition.Count}"); // note: Count is expensive
            }
            CachePartition[cacheKey] = bytes;
        }
    }

 

    internal class FilePartionedCacheSerializer
        : AbstractPartitionedCacheSerializer
    {
        private readonly string _filePath;

        public FilePartionedCacheSerializer(string filePath = "c:\\temp")
        {
            _filePath = filePath;
        }

        protected override byte[] ReadCacheBytes(string cacheKey)
        {
            string file = GetPath(cacheKey);

            if (File.Exists(file))
            {
                //_logger.Verbose($"[InMemoryPartitionedTokenCache] ReadCacheBytes found cacheKey {cacheKey}");
                return File.ReadAllBytes(file);
            }

            //_logger.Verbose($"[InMemoryPartitionedTokenCache] ReadCacheBytes did not find cacheKey {cacheKey}");

            return null;
        }

        private string GetPath(string cacheKey)
        {
            return Path.Combine(_filePath, cacheKey) + ".json";
        }

        protected override void RemoveKey(string cacheKey)
        {
            string file = GetPath(cacheKey);

            if (File.Exists(file))
            {
                File.Delete(cacheKey);
            }
        }

        protected override void WriteCacheBytes(string cacheKey, byte[] bytes)
        {
            string file = GetPath(cacheKey);
            File.WriteAllBytes(file, bytes);
        }
    }

}


