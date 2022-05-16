// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.;

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace WebApi.Misc
{
    internal class FilePartitionedR9DistributedCache 
    {
        private readonly string _filePath;
        private readonly Func<string, string> _expandKey;
        public FilePartitionedR9DistributedCache(string filePath = "c:\\temp\\r9")
        {
            _filePath = filePath;
            _expandKey = (s) => s;
        }

        public byte[] Get(string key)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public void Refresh(string key)
        {
            throw new NotImplementedException();
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public void Remove(string key)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            string file = GetPath(key);

            if (File.Exists(file))
            {
                File.Delete(key);
            }

            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            throw new NotImplementedException();
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task SetMemoryAsync(string key, ReadOnlyMemory<byte> value, DistributedCacheEntryOptions options, CancellationToken cancellationToken)
        {
            string file = GetPath(key);
            byte[] bytes = value.ToArray();
            File.WriteAllBytes(file, bytes);
            return Task.CompletedTask;
        }

        public async Task<bool> TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken cancellationToken)
        {
            var value = await GetValueAsync(key).ConfigureAwait(false);
            if (value == null)
            {
                return false;
            }

            // sadly, we need to make a copy as there's no way for SE.Redis to decode into our preallocated buffer directly :-(
            var mem = destination.GetMemory(value.Length);
            value.AsMemory().CopyTo(mem);
            destination.Advance(value.Length);
            return true;
        }

        private async Task<byte[]> GetValueAsync(string key)
        {
            var cacheKey = _expandKey(key);
            string file = GetPath(cacheKey);

            if (File.Exists(file))
            {
                return await File.ReadAllBytesAsync(file).ConfigureAwait(false);
            }

            return null;
        }

        private string GetPath(string cacheKey)
        {
            return Path.Combine(_filePath, cacheKey) + ".json";
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

