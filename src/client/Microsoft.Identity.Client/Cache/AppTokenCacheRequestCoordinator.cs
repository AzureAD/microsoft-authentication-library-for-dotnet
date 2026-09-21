// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache.Items;

namespace Microsoft.Identity.Client.Cache
{
    internal sealed class AppTokenCacheRequestCoordinator
    {
        private readonly object _gate = new object();
        private readonly Dictionary<string, ReadFlight> _readFlights =
            new Dictionary<string, ReadFlight>(StringComparer.Ordinal);
        private readonly Dictionary<string, object> _proactiveRefreshFlights =
            new Dictionary<string, object>(StringComparer.Ordinal);
        private int _activeExternalReads;
        private long _readGeneration;

        internal int ReadFlightCount
        {
            get
            {
                lock (_gate)
                {
                    return _readFlights.Count;
                }
            }
        }

        internal int ProactiveRefreshFlightCount
        {
            get
            {
                lock (_gate)
                {
                    return _proactiveRefreshFlights.Count;
                }
            }
        }

        internal async Task<AppTokenCacheReadResponse> RunCacheReadAsync(
            string key,
            Func<Task<AppTokenCacheReadResult>> readAsync,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (readAsync is null)
            {
                throw new ArgumentNullException(nameof(readAsync));
            }

            cancellationToken.ThrowIfCancellationRequested();

            ReadFlight flight;
            bool isLeader;

            lock (_gate)
            {
                if (_readFlights.TryGetValue(key, out flight))
                {
                    isLeader = false;
                }
                else
                {
                    flight = new ReadFlight();
                    _readFlights.Add(key, flight);
                    _activeExternalReads++;
                    _readGeneration++;
                    isLeader = true;
                }
            }

            if (isLeader)
            {
                try
                {
                    AppTokenCacheReadResult leaderResult = await readAsync().ConfigureAwait(false);
                    flight.Completion.TrySetResult(leaderResult);
                }
                catch
                {
                    // Followers retry through their own cache session rather than sharing
                    // an exception instance that request telemetry may mutate.
                    flight.Completion.TrySetResult(
                        AppTokenCacheReadResult.RetryIndependently());
                    throw;
                }
                finally
                {
                    RemoveReadFlight(key, flight);
                }
            }

            AppTokenCacheReadResult result = await WaitWithCancellationAsync(
                flight.Completion.Task,
                cancellationToken).ConfigureAwait(false);

            return new AppTokenCacheReadResponse(result, isLeader);
        }

        internal bool TryStartProactiveRefresh(string key, out object owner)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            lock (_gate)
            {
                if (_proactiveRefreshFlights.ContainsKey(key))
                {
                    owner = null;
                    return false;
                }

                owner = new object();
                _proactiveRefreshFlights.Add(key, owner);
                return true;
            }
        }

        internal bool TryBeginInMemoryRead(out long generation)
        {
            lock (_gate)
            {
                if (_activeExternalReads != 0)
                {
                    generation = 0;
                    return false;
                }

                generation = _readGeneration;
                return true;
            }
        }

        internal bool IsInMemoryReadStable(long generation)
        {
            lock (_gate)
            {
                return _activeExternalReads == 0 &&
                    _readGeneration == generation;
            }
        }

        internal async Task<AppTokenCacheReadResult> RunIndependentCacheReadAsync(
            Func<Task<AppTokenCacheReadResult>> readAsync)
        {
            if (readAsync is null)
            {
                throw new ArgumentNullException(nameof(readAsync));
            }

            BeginExternalRead();
            try
            {
                return await readAsync().ConfigureAwait(false);
            }
            finally
            {
                EndExternalRead();
            }
        }

        internal void CompleteProactiveRefresh(string key, object owner)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (owner is null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            lock (_gate)
            {
                if (_proactiveRefreshFlights.TryGetValue(key, out object currentOwner) &&
                    ReferenceEquals(currentOwner, owner))
                {
                    _proactiveRefreshFlights.Remove(key);
                }
            }
        }

        private void RemoveReadFlight(string key, ReadFlight flight)
        {
            lock (_gate)
            {
                if (_readFlights.TryGetValue(key, out ReadFlight currentFlight) &&
                    ReferenceEquals(currentFlight, flight))
                {
                    _readFlights.Remove(key);
                    EndExternalReadUnderLock();
                }
            }
        }

        private void BeginExternalRead()
        {
            lock (_gate)
            {
                _activeExternalReads++;
                _readGeneration++;
            }
        }

        private void EndExternalRead()
        {
            lock (_gate)
            {
                EndExternalReadUnderLock();
            }
        }

        private void EndExternalReadUnderLock()
        {
            _activeExternalReads--;
            _readGeneration++;
        }

#pragma warning disable VSTHRD003 // This helper safely detaches a follower from a shared read flight.
        private static async Task<T> WaitWithCancellationAsync<T>(
            Task<T> task,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                return await task.ConfigureAwait(false);
            }

            var canceled = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            using (cancellationToken.Register(
                state => ((TaskCompletionSource<bool>)state).TrySetResult(true),
                canceled))
            {
                Task completedTask = await Task.WhenAny(task, canceled.Task).ConfigureAwait(false);
                if (!ReferenceEquals(completedTask, task))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await task.ConfigureAwait(false);
        }
#pragma warning restore VSTHRD003

        private sealed class ReadFlight
        {
            internal TaskCompletionSource<AppTokenCacheReadResult> Completion { get; } =
                new TaskCompletionSource<AppTokenCacheReadResult>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    internal sealed class AppTokenCacheReadResult
    {
        internal AppTokenCacheReadResult(
            MsalAccessTokenCacheItem cacheItem,
            CacheLevel cacheLevel,
            CacheRefreshReason cacheInfo,
            int cachedAccessTokenCount,
            long durationInCacheInMs,
            bool shouldRetryIndependently = false)
        {
            CacheItem = cacheItem;
            CacheLevel = cacheLevel;
            CacheInfo = cacheInfo;
            CachedAccessTokenCount = cachedAccessTokenCount;
            DurationInCacheInMs = durationInCacheInMs;
            ShouldRetryIndependently = shouldRetryIndependently;
        }

        internal MsalAccessTokenCacheItem CacheItem { get; }

        internal CacheLevel CacheLevel { get; }

        internal CacheRefreshReason CacheInfo { get; }

        internal int CachedAccessTokenCount { get; }

        internal long DurationInCacheInMs { get; }

        internal bool ShouldRetryIndependently { get; }

        internal static AppTokenCacheReadResult RetryIndependently()
        {
            return new AppTokenCacheReadResult(
                cacheItem: null,
                CacheLevel.None,
                CacheRefreshReason.NotApplicable,
                cachedAccessTokenCount: 0,
                durationInCacheInMs: 0,
                shouldRetryIndependently: true);
        }
    }

    internal sealed class AppTokenCacheReadResponse
    {
        internal AppTokenCacheReadResponse(
            AppTokenCacheReadResult result,
            bool isLeader)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            IsLeader = isLeader;
        }

        internal AppTokenCacheReadResult Result { get; }

        internal bool IsLeader { get; }
    }
}
