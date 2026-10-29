// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    /// <summary>
    /// Coordinates acquisition, not results. Every admitted caller uses its existing cache session.
    /// Tokens and completed operations are not retained; eligible service failures use throttling.
    /// </summary>
    internal sealed class TokenRequestCoalescer
    {
        private static readonly ConditionalWeakTable<object, GatePool> s_cacheGates = new ConditionalWeakTable<object, GatePool>();
        private static readonly object s_sharedCache = new object();
        private static readonly ConditionalWeakTable<IServiceBundle, ThrottlingCache> s_failureCaches =
            new ConditionalWeakTable<IServiceBundle, ThrottlingCache>();
        private static readonly ConditionalWeakTable<object, StrategyIdentity> s_strategyIdentities =
            new ConditionalWeakTable<object, StrategyIdentity>();
        private static long s_nextStrategyId;
        private readonly GatePool _pool;
        private readonly string _key;
        private readonly ThrottlingCache _failureCache;
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
            _failureCache = GetFailureCache(parameters.RequestContext.ServiceBundle);
        }

        internal static ThrottlingCache GetFailureCache(IServiceBundle serviceBundle) =>
            s_failureCaches.GetValue(serviceBundle, _ => new ThrottlingCache());

        internal RequestContext RequestContext => _parameters.RequestContext;

        internal async Task<AuthenticationResult> ExecuteAsync(
            Func<Task<AuthenticationResult>> acquire,
            CancellationToken cancellationToken)
        {
            using (var admission = await _pool.EnterAsync(_key, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await ExecuteUnderGateAsync(acquire, null, cancellationToken, admission).ConfigureAwait(false);
            }
        }

        internal GatePool.Lease TryEnter()
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
            CancellationToken cancellationToken,
            GatePool.Lease admission = null)
        {
            _parameters.ReleaseCoalescingAdmission = exception =>
            {
                // Publish a final eligible failure before observers can start another acquisition.
                if (exception is MsalServiceException serviceException)
                {
                    RecordServiceFailure(serviceException, cancellationToken);
                    _parameters.LastTokenEndpointServiceException = null;
                }
                admission?.Dispose();
            };
            try
            {
                return await ExecuteCoreAsync(acquire, observedToken, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _parameters.ReleaseCoalescingAdmission = null;
            }
        }

        private async Task<AuthenticationResult> ExecuteCoreAsync(
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
                // Validation is coordinated; formatting an accepted token is caller-local.
                _parameters.ReleaseCoalescingAdmission?.Invoke(null);
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
            else if (_parameters.RequestContext.ApiEvent.CacheInfo == CacheRefreshReason.NotApplicable)
            {
                _parameters.RequestContext.ApiEvent.CacheInfo = _initialRefreshReason;
            }

            var logger = _parameters.RequestContext.Logger;
            if (_parameters.AppConfig.IsConfidentialClient)
            {
                ThrottleCommon.TryThrowServiceException(_key, _failureCache, logger, nameof(TokenRequestCoalescer));
            }
            AuthenticationResult acquired;
            _parameters.LastTokenEndpointServiceException = null;
            try
            {
                acquired = await acquire().ConfigureAwait(false);
            }
            catch (MsalServiceException ex)
            {
                RecordServiceFailure(ex, cancellationToken);
                throw;
            }
            finally
            {
                _parameters.LastTokenEndpointServiceException = null;
            }
            if (acquired.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider)
            {
                _parameters.RequestContext.ServiceBundle.HttpTelemetryManager.RecordSuccessfulAcquisition(_initialRefreshReason);
            }
            return acquired;
        }

        private void RecordServiceFailure(MsalServiceException ex, CancellationToken cancellationToken)
        {
            if (_parameters.AppConfig.IsConfidentialClient &&
                ReferenceEquals(ex, _parameters.LastTokenEndpointServiceException) &&
                !cancellationToken.IsCancellationRequested &&
                (ex.StatusCode == 429 || (ex.StatusCode >= 500 && ex.StatusCode < 600)) &&
                ex is not MsalUiRequiredException &&
                !string.Equals(ex.ErrorCode, MsalError.InvalidClient, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(ex.ErrorCode, MsalError.InvalidGrantError, StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrEmpty(ex.Claims) &&
                !RetryAfterProvider.TryGetRetryAfterValue(ex.Headers, out _))
            {
                // Followers can observe the failure before RequestBase assigns the owner's correlation ID.
                if (string.IsNullOrWhiteSpace(ex.CorrelationId))
                {
                    ex.CorrelationId = _parameters.CorrelationId.ToString();
                }
                _failureCache.AddAndCleanup(_key,
                    new ThrottlingCacheEntry(ex, HttpStatusProvider.s_throttleDuration), _parameters.RequestContext.Logger);
            }
        }

        private sealed class StrategyIdentity
        {
            internal string Id { get; } = Interlocked.Increment(ref s_nextStrategyId).ToString(CultureInfo.InvariantCulture);
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
            void AddIdentity(object value)
            {
                Add(value is null ? null : s_strategyIdentities.GetValue(value, _ => new StrategyIdentity()).Id);
            }

            Add(flow);
            var provider = parameters.IsClientCredentialRequest ? parameters.AppConfig.AppTokenProvider : null;
            var credential = provider is null && parameters.AppConfig.ClientCredential?.HasRequestCallback == true
                ? parameters.AppConfig.ClientCredential : null;
            var failureCallback = parameters.IsClientCredentialRequest ? parameters.AppConfig.OnMsalServiceFailure : null;
            var requestCallbacks = parameters.OnBeforeTokenRequestHandler;
            bool hasCallbacks = provider is not null || credential is not null || failureCallback is not null ||
                requestCallbacks?.Count > 0;
            // Only ordinary direct acquisitions coordinate across applications. Callback identity is
            // reference identity, never an inference about equivalent delegate implementations.
            AddIdentity(hasCallbacks ? parameters.RequestContext.ServiceBundle : null);
            AddIdentity(provider);
            AddIdentity(credential);
            AddIdentity(failureCallback);
            if (requestCallbacks is not null)
            {
                foreach (var callback in requestCallbacks)
                {
                    AddIdentity(callback);
                }
            }
            Add(null);
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

            internal async Task<Lease> EnterAsync(string key, CancellationToken cancellationToken)
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

            internal Lease TryEnter(string key)
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

            internal sealed class Entry
            {
                internal readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
                internal int References;
            }

            internal sealed class Lease : IDisposable
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
