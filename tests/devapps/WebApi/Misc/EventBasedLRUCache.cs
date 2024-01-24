// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;

namespace WebApi.Misc
{
    /// <summary>
    /// This is an LRU cache implementation that relies on an event queue rather than locking to achieve thread safety.
    /// This approach has been decided on in order to optimize the performance of the get and set operations on the cache.
    /// This cache contains a doubly linked list in order to maintain LRU order, as well as a dictionary (map) to keep track of
    /// keys and expiration times. The linked list (a structure which is not thread-safe) is NEVER modified directly inside
    /// an API call (e.g. get, set, remove); it is only ever modified sequentially by a background thread. On the other hand,
    /// the map is a <see cref="ConcurrentDictionary{TKey, TValue}"/> which may be modified directly inside an API call or
    /// through eventual processing of the event queue. This implementation relies on the principle of 'eventual consistency':
    /// though the map and it's corresponding linked list may be out of sync at any given point in time, they will eventually line up.
    /// See here for more details:
    /// https://aka.ms/identitymodel/caching
    /// </summary>
    /// <typeparam name="TKey">The key type to be used by the cache.</typeparam>
    /// <typeparam name="TValue">The value type to be used by the cache</typeparam>
    internal class EventBasedLRUCache<TKey, TValue> : IDisposable
    {
        internal delegate void ItemRemoved(TValue Value);

        private readonly int _capacity;
        // The percentage of the cache to be removed when _maxCapacityPercentage is reached.
        private readonly double _compactionPercentage = .20;
        private LinkedList<LRUCacheItem<TKey, TValue>> _doubleLinkedList = new LinkedList<LRUCacheItem<TKey, TValue>>();
        private BlockingCollection<Action> _eventQueue = new BlockingCollection<Action>();
        private readonly Task _eventQueueTask;
        private ConcurrentDictionary<TKey, LRUCacheItem<TKey, TValue>> _map;
        // When the current cache size gets to this percentage of _capacity, _compactionPercentage% of the cache will be removed.
        private readonly double _maxCapacityPercentage = .95;
        private bool _disposed = false;
        private readonly int _tryTakeTimeout;
        // if true, expired values will not be added to the cache and clean-up of expired values will occur on a 5 minute interval
        private readonly bool _removeExpiredValues;

        internal EventBasedLRUCache(
            int capacity,
            TaskCreationOptions options = TaskCreationOptions.LongRunning,
            IEqualityComparer<TKey> comparer = null,
            int tryTakeTimeout = 500,
            bool removeExpiredValues = true,
            int cleanUpIntervalInSeconds = 300)
        {
            _tryTakeTimeout = tryTakeTimeout;
            _capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));
            _map = new ConcurrentDictionary<TKey, LRUCacheItem<TKey, TValue>>(comparer ?? EqualityComparer<TKey>.Default);
            _removeExpiredValues = removeExpiredValues;
            _eventQueueTask = new Task(OnStart, options);
            _eventQueueTask.Start();
            if (_removeExpiredValues)
                _ = RemoveExpiredValuesPeriodicallyAsync(TimeSpan.FromSeconds(cleanUpIntervalInSeconds));

            
        }

        ILoggerAdapter _logger = new NullLogger();

        private void OnStart()
        {
            while (!_disposed)
            {
                try
                {
                    if (_eventQueue.TryTake(out var action, _tryTakeTimeout))
                        action.Invoke();
                }
                catch (Exception ex)
                {                    
                    _logger.ErrorPii(ex);
                    throw; // TODO: do not throw
                }
            }
        }

        public bool Contains(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return _map.ContainsKey(key);
        }

        /// <summary>
        /// FOR TESTING PURPOSES ONLY.
        /// </summary>
        internal void WaitForProcessing()
        {
            while (!_disposed)
            {
                if (_eventQueue.Count == 0)
                    return;
            }
        }

        internal int RemoveExpiredValues()
        {
            int numItemsRemoved = 0;
            try
            {
                var node = _doubleLinkedList.First;
                while (node != null)
                {
                    var nextNode = node.Next;
                    if (node.Value.ExpirationTime < DateTime.UtcNow)
                    {
                        _doubleLinkedList.Remove(node);
                        if (_map.TryRemove(node.Value.Key, out var cacheItem))
                            OnItemRemoved?.Invoke(cacheItem.Value);

                        numItemsRemoved++;
                    }

                    node = nextNode;
                }
            }
            catch (ObjectDisposedException)
            {
                //LogHelper.LogWarning(LogHelper.FormatInvariant(LogMessages.IDX10902, nameof(RemoveExpiredValues), ex));
                throw; // TODO: do not throw
            }

            return numItemsRemoved;
        }

        internal void RemoveLRUs()
        {
            // use the _capacity for the newCacheSize calculation in the case where the cache is experiencing overflow
            int currentCount = _map.Count <= _capacity ? _capacity : _map.Count;
            var newCacheSize = currentCount - (int)(currentCount * _compactionPercentage);
            while (_map.Count > newCacheSize && _doubleLinkedList.Count > 0)
            {
                var lru = _doubleLinkedList.Last;
                if (_map.TryRemove(lru.Value.Key, out var cacheItem))
                    OnItemRemoved?.Invoke(cacheItem.Value);

                _doubleLinkedList.Remove(lru);
            }
        }

        async Task RemoveExpiredValuesPeriodicallyAsync(TimeSpan interval)
        {
            try
            {
                while (!_disposed)
                {
                    _eventQueue.Add(() => RemoveExpiredValues());
                    await Task.Delay(interval).ConfigureAwait(false);
                }
            }
            catch (ObjectDisposedException)
            {
                //LogHelper.LogWarning(LogHelper.FormatInvariant(LogMessages.IDX10902, nameof(RemoveExpiredValuesPeriodically), ex));
                throw; // TODO: do not throw
            }
        }

        public void SetValue(TKey key, TValue value)
        {
            SetValue(key, value, DateTime.MaxValue);
        }

        public bool SetValue(TKey key, TValue value, DateTime expirationTime)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // if item already expired, do not add it to the cache if the _removeExpiredValues setting is set to true
            if (_removeExpiredValues && expirationTime < DateTime.UtcNow)
                return false;

            // just need to update value and move it to the top
            if (_map.TryGetValue(key, out var cacheItem))
            {
                cacheItem.Value = value;
                cacheItem.ExpirationTime = expirationTime;
                _eventQueue.Add(() =>
                {
                    _doubleLinkedList.Remove(cacheItem);
                    _doubleLinkedList.AddFirst(cacheItem);
                });
            }
            else
            {
                // if cache is at _maxCapacityPercentage, trim it by _compactionPercentage
                if ((double)_map.Count / _capacity >= _maxCapacityPercentage)
                {
                    _eventQueue.Add(RemoveLRUs);
                }
                // add the new node
                var newCacheItem = new LRUCacheItem<TKey, TValue>(key, value, expirationTime);
                _eventQueue.Add(() =>
                {
                    // Add a remove operation in case two threads are trying to add the same value. Only the second remove will succeed in this case.
                    _doubleLinkedList.Remove(newCacheItem);
                    _doubleLinkedList.AddFirst(newCacheItem);
                });
                _map[key] = newCacheItem;
            }

            return true;
        }

        /// Each time a node gets accessed, it gets moved to the beginning (head) of the list.
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (!_map.ContainsKey(key))
            {
                value = default;
                return false;
            }

            // make sure node hasn't been removed by a different thread
            if (_map.TryGetValue(key, out var cacheItem))
                _eventQueue.Add(() =>
                {
                    _doubleLinkedList.Remove(cacheItem);
                    _doubleLinkedList.AddFirst(cacheItem);
                });

            value = cacheItem != null ? cacheItem.Value : default;
            return cacheItem != null;
        }

        /// Removes a particular key from the cache.
        public bool TryRemove(TKey key, out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (!_map.TryGetValue(key, out var cacheItem))
            {
                value = default;
                return false;
            }

            value = cacheItem.Value;
            _eventQueue.Add(() => _doubleLinkedList.Remove(cacheItem));
            if (_map.TryRemove(key, out cacheItem))
            {
                OnItemRemoved?.Invoke(cacheItem.Value);
                return true;
            }

            return false;
        }

        internal ItemRemoved OnItemRemoved
        {
            get;
            set;
        }

        #region FOR TESTING (INTERNAL ONLY)

        /// <summary>
        /// FOR TESTING ONLY.
        /// </summary>
        /// <returns></returns>
        internal LinkedList<LRUCacheItem<TKey, TValue>> LinkedList => _doubleLinkedList;

        /// <summary>
        /// FOR TESTING ONLY.
        /// </summary>
        internal long LinkedListCount => _doubleLinkedList.Count;

        /// <summary>
        /// FOR TESTING ONLY.
        /// </summary>
        internal long MapCount => _map.Count;

        /// <summary>
        /// FOR TESTING ONLY.
        /// </summary>
        /// <returns></returns>
        internal ICollection<LRUCacheItem<TKey, TValue>> MapValues => _map.Values;

        /// <summary>
        /// FOR TESTING ONLY.
        /// </summary>
        internal long EventQueueCount => _eventQueue.Count;
        #endregion

        /// <summary>
        /// Calls <see cref="Dispose(bool)"/> and <see cref="GC.SuppressFinalize"/>
        /// </summary>
        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// If <paramref name="disposing"/> is true, this method disposes of <see cref="_eventQueue"/>.
        /// </summary>
        /// <param name="disposing">True if called from the <see cref="Dispose()"/> method, false otherwise.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    _eventQueueTask.Wait();
                    _eventQueue.Dispose();
                    _eventQueue = null;
                    _map = null;
                    _doubleLinkedList = null;
                }
            }
        }
    }

    internal class LRUCacheItem<TKey, TValue>
    {
        internal TKey Key { get; }
        internal TValue Value { get; set; }
        internal DateTime ExpirationTime { get; set; }

        internal LRUCacheItem(TKey key, TValue value)
        {
            if (key== null || value == null )
            {
                throw new ArgumentNullException("key or value");
            }

            Key = key;
            Value = value;
        }

        internal LRUCacheItem(TKey key, TValue value, DateTime expirationTime)
        {
            if (key == null || value == null)
            {
                throw new ArgumentNullException("key or value");
            }

            Key = key;
            Value = value;
            ExpirationTime = expirationTime;
        }

        public override bool Equals(object obj)
        {
            LRUCacheItem<TKey, TValue> item = obj as LRUCacheItem<TKey, TValue>;
            return item != null && Key.Equals(item.Key);
        }

        public override int GetHashCode() => 990326508 + EqualityComparer<TKey>.Default.GetHashCode(Key);
    }

    internal class MsalCacheBasedOnWilson : AbstractPartitionedCacheSerializer
    {
        private readonly EventBasedLRUCache<string, byte[]> _cache;

        public MsalCacheBasedOnWilson(EventBasedLRUCache<string, byte[]> cache)
        {
            _cache = cache;
        }

        protected override byte[] ReadCacheBytes(string cacheKey)
        {
            if (_cache.TryGetValue(cacheKey, out byte[] val))
            {
                return val;
            }
            return null;
        }

        protected override void RemoveKey(string cacheKey)
        {
            _cache.TryRemove(cacheKey, out _);
        }

        protected override void WriteCacheBytes(string cacheKey, byte[] bytes)
        {
            _cache.SetValue(cacheKey, bytes);
        }
    }
}
