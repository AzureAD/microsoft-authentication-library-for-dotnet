// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    /// <summary>
    /// Coordinates acquisition, not results. Every admitted caller uses its existing cache session.
    /// No token, exception, or completed operation is retained by the coordinator.
    /// </summary>
    internal sealed class TokenRequestCoalescer
    {
        private static readonly ConditionalWeakTable<object, GatePool> s_cacheGates = new ConditionalWeakTable<object, GatePool>();
        private static readonly object s_sharedCache = new object();
        private readonly GatePool _pool;
        private readonly string _key;
        private readonly AuthenticationRequestParameters _parameters;
        private readonly CacheRefreshReason _initialRefreshReason;
        private readonly Func<Task<MsalAccessTokenCacheItem>> _findToken;
        private readonly Func<MsalAccessTokenCacheItem, CancellationToken, Task<AuthenticationResult>> _createResult;

        internal TokenRequestCoalescer(
            AuthenticationRequestParameters parameters,
            string flow,
            Func<Task<MsalAccessTokenCacheItem>> findToken,
            Func<MsalAccessTokenCacheItem, CancellationToken, Task<AuthenticationResult>> createResult,
            string tokenHashToRefresh = null)
        {
            _parameters = parameters;
            _initialRefreshReason = parameters.RequestContext.ApiEvent.CacheInfo;
            _findToken = findToken;
            _createResult = createResult;
            // Only the existing explicit shared-cache setting joins different cache objects.
            // External persistence and its visibility/reload semantics are deliberately not inferred.
            object cache = parameters.AppConfig.AccessorOptions?.UseSharedCache == true
                ? s_sharedCache
                : parameters.CacheSessionManager.TokenCacheInternal;
            _pool = s_cacheGates.GetValue(cache, _ => new GatePool());
            _key = CreateKey(parameters, flow, tokenHashToRefresh);
        }

        internal async Task<AuthenticationResult> ExecuteAsync(
            Func<Task<AuthenticationResult>> acquire,
            CancellationToken cancellationToken)
        {
            using (await _pool.EnterAsync(_key, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await ExecuteUnderGateAsync(acquire, null, cancellationToken).ConfigureAwait(false);
            }
        }

        internal IDisposable TryEnter()
        {
            var admission = _pool.TryEnter(_key);
            if (admission is null)
            {
                _parameters.RequestContext.ServiceBundle.HttpTelemetryManager.RecordCoalescedRequest(_initialRefreshReason);
            }
            return admission;
        }

        /// <summary>
        /// Returns null when a proactive refresh is already satisfied, without constructing
        /// another cache result or invoking its formatting and telemetry callbacks.
        /// </summary>
        internal async Task<AuthenticationResult> ExecuteUnderGateAsync(
            Func<Task<AuthenticationResult>> acquire,
            MsalAccessTokenCacheItem observedToken,
            CancellationToken cancellationToken)
        {
            var cachedToken = await _findToken().ConfigureAwait(false);
            cachedToken = await RequestBase.ValidateCachedAccessTokenAsync(
                _parameters, cachedToken, nameof(TokenRequestCoalescer)).ConfigureAwait(false);

            if (cachedToken is not null &&
                (observedToken is null || !IsSameGeneration(cachedToken, observedToken) ||
                 !SilentRequestHelper.NeedsRefresh(cachedToken)))
            {
                var result = observedToken is null
                    ? await _createResult(cachedToken, cancellationToken).ConfigureAwait(false)
                    : null;
                _parameters.RequestContext.ServiceBundle.HttpTelemetryManager.RecordCoalescedRequest(_initialRefreshReason);
                return result;
            }

            if (observedToken is not null)
            {
                // A cache lookup can overwrite CacheInfo. A refresh elected after that lookup is
                // still a proactive acquisition, not a new foreground miss.
                _parameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ProactivelyRefreshed;
            }
            var acquired = await acquire().ConfigureAwait(false);
            if (acquired.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider)
            {
                _parameters.RequestContext.ServiceBundle.HttpTelemetryManager.RecordSuccessfulAcquisition(_initialRefreshReason);
            }
            return acquired;
        }

        private static bool IsSameGeneration(MsalAccessTokenCacheItem current, MsalAccessTokenCacheItem observed)
        {
            return current.CachedAt == observed.CachedAt &&
                current.ExpiresOn == observed.ExpiresOn &&
                current.RefreshOn == observed.RefreshOn &&
                string.Equals(current.Secret, observed.Secret, StringComparison.Ordinal);
        }

        private static string CreateKey(AuthenticationRequestParameters parameters, string flow, string tokenHashToRefresh)
        {
            var key = new StringBuilder();
            void Add(string value)
            {
                key.Append(value?.Length ?? -1).Append(':').Append(value);
            }

            Add(flow);
            Add(parameters.AuthorityManager.OriginalAuthority.AuthorityInfo.CanonicalAuthority.AbsoluteUri.ToLowerInvariant());
            Add(parameters.AppConfig.ClientId);
            Add(CacheKeyFactory.GetKeyFromRequest(parameters));
            foreach (string scope in parameters.Scope.OrderBy(s => s, StringComparer.Ordinal))
            {
                Add(scope);
            }
            Add(null);
            Add(parameters.AuthenticationScheme.AccessTokenType);
            Add(parameters.AuthenticationScheme.KeyId);
            Add(parameters.MtlsCertificate?.ComputeX5tS256KeyId());
            Add(parameters.IsMtlsPopRequested.ToString());
            Add(parameters.PreferMsiV2.ToString());
            Add(parameters.MtlsPopMinStrength.ToString());
            Add(parameters.ClaimsAndClientCapabilities);
            Add(tokenHashToRefresh);
            Add(parameters.FmiPathSuffix);
            Add(parameters.ClientAssertionFmiPath);
            Add(parameters.PartitionRefreshToken.ToString());
            if (parameters.CacheKeyComponents is not null)
            {
                foreach (var component in parameters.CacheKeyComponents)
                {
                    Add(component.Key);
                    Add(component.Value);
                }
            }
            Add(null);
            foreach (var parameter in parameters.ExtraQueryParameters.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                Add(parameter.Key);
                Add(parameter.Value);
            }

            // Assertion/session identifiers and policy parameters must never be retained as raw keys.
            return parameters.RequestContext.ServiceBundle.PlatformProxy.CryptographyManager.CreateSha256Hash(key.ToString());
        }

        /// <summary>
        /// Entries live only while held or awaited. Registration, release and removal share one lock,
        /// so a queued waiter cannot be stranded on a removed gate while a second gate is created.
        /// </summary>
        internal sealed class GatePool
        {
            private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);

            internal int Count
            {
                get
                {
                    lock (_entries)
                    {
                        return _entries.Count;
                    }
                }
            }

            internal async Task<IDisposable> EnterAsync(string key, CancellationToken cancellationToken)
            {
                Entry entry;
                lock (_entries)
                {
                    entry = GetEntry(key);
                    entry.References++;
                }

                try
                {
                    await entry.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    return new Lease(this, key, entry);
                }
                catch
                {
                    Release(key, entry, acquired: false);
                    throw;
                }
            }

            internal IDisposable TryEnter(string key)
            {
                lock (_entries)
                {
                    var entry = GetEntry(key);
                    if (!entry.Semaphore.Wait(0))
                    {
                        return null;
                    }

                    entry.References++;
                    return new Lease(this, key, entry);
                }
            }

            private Entry GetEntry(string key)
            {
                if (!_entries.TryGetValue(key, out var entry))
                {
                    entry = new Entry();
                    _entries.Add(key, entry);
                }
                return entry;
            }

            private void Release(string key, Entry entry, bool acquired)
            {
                lock (_entries)
                {
                    if (acquired)
                    {
                        entry.Semaphore.Release();
                    }
                    if (--entry.References == 0)
                    {
                        _entries.Remove(key);
                        entry.Semaphore.Dispose();
                    }
                }
            }

            private sealed class Entry
            {
                internal readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
                internal int References;
            }

            private sealed class Lease : IDisposable
            {
                private GatePool _pool;
                private readonly string _key;
                private readonly Entry _entry;

                internal Lease(GatePool pool, string key, Entry entry)
                {
                    _pool = pool;
                    _key = key;
                    _entry = entry;
                }

                public void Dispose()
                {
                    Interlocked.Exchange(ref _pool, null)?.Release(_key, _entry, acquired: true);
                }
            }
        }
    }
}
