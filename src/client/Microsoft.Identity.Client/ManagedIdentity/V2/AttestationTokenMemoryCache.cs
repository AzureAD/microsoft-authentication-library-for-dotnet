// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Phase 1: process-local in-memory cache for attestation tokens.
    /// - Key: KeyHandle pointer value
    /// - TTL (Time to live): 8 hours (until provider exposes an explicit expiry)
    /// - Background refresh: kicks off at half-time (4h) without blocking callers
    /// - Thread-safe across callers; no cross-process guarantees (by design for Phase 1)
    ///
    /// Phase 2 (hand-off notes for persistent cache):
    /// - Add an IAttestationTokenCache interface to the provider input
    /// - Add a persistent cache implementation
    /// - Use a named OS mutex 
    /// - Persist using the same key (KeyHandle pointer value) for simplicity
    /// - needs logging 
    /// - details around background refresh and process exit needs some discussion
    /// </summary>
    internal static class AttestationTokenMemoryCache
    {
        // Today MAA does not give expiry info; assume 8h TTL for now.
        // We have manually validated this with MAA tokens. 
        private static readonly TimeSpan s_defaultTtl = TimeSpan.FromHours(8); // provider has no expiry yet
        private static readonly TimeSpan s_halfTime = TimeSpan.FromHours(4); // background refresh point
        private static readonly TimeSpan s_expirySkew = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan s_bgRetryBackoff = TimeSpan.FromMinutes(15);

        // One Entry per key handle value
        private static readonly ConcurrentDictionary<long, Entry> s_entries =
            new ConcurrentDictionary<long, Entry>();

        /// <summary>
        /// Returns a valid token. If missing/expired, mints via <paramref name="provider"/> and caches it.
        /// If past half-time, returns the current token and schedules a background refresh.
        /// </summary>
        internal static async Task<AttestationTokenResponse> GetOrCreateAsync(
            AttestationTokenInput input,
            Func<AttestationTokenInput, CancellationToken, Task<AttestationTokenResponse>> provider,
            CancellationToken ct)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            long key = GetHandleValue(input);
            var entry = s_entries.GetOrAdd(key, k => new Entry(k));

            // Gate all mutations per key
            await entry.Gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var now = DateTimeOffset.UtcNow;

                // Happy path: valid token in memory
                if (!string.IsNullOrEmpty(entry.Token) && now + s_expirySkew < entry.ExpiresOnUtc)
                {
                    // Past refresh time? Kick a non-blocking background refresh.
                    if (now >= entry.RefreshOnUtc)
                    {
                        KickBackgroundRefresh(entry, input, provider);
                    }

                    return new AttestationTokenResponse { AttestationToken = entry.Token };
                }

                // Miss / expired -> mint synchronously and update cache
                var minted = await provider(input, ct).ConfigureAwait(false);
                if (minted == null || string.IsNullOrEmpty(minted.AttestationToken))
                {
                    throw new MsalClientException("attestation_failed", "Attestation provider returned no token.");
                }

                var now2 = DateTimeOffset.UtcNow;
                entry.Token = minted.AttestationToken;
                entry.ExpiresOnUtc = now2 + s_defaultTtl;
                entry.RefreshOnUtc = now2 + s_halfTime;

                // Store the refresh factory so background timer can re-mint without caller context.
                entry.Mint = ctk => provider(input, ctk);

                // (Re)schedule the per-key timer to fire at RefreshOnUtc
                ScheduleTimer(entry);

                return minted;
            }
            finally
            {
                entry.Gate.Release();
            }
        }

        // ---------------- internals ----------------

        private static long GetHandleValue(AttestationTokenInput input)
        {
            try
            {
                if (input.KeyHandle != null && !input.KeyHandle.IsInvalid)
                {
                    return input.KeyHandle.DangerousGetHandle().ToInt64();
                }
            }
            catch { /* ignore */ }
            return 0L;
        }

        private static void KickBackgroundRefresh(
            Entry entry,
            AttestationTokenInput lastInput,
            Func<AttestationTokenInput, CancellationToken, Task<AttestationTokenResponse>> provider)
        {
            // Background: do not block the caller thread; dedupe via Gate.TryEnter
            Task.Run(async () =>
            {
                if (!entry.Gate.Wait(0))
                    return; // another refresh in progress
                try
                {
                    // Freshen only if still past refresh (re-check)
                    var now = DateTimeOffset.UtcNow;
                    if (string.IsNullOrEmpty(entry.Token) || now < entry.RefreshOnUtc)
                    {
                        return;
                    }

                    // Prefer stored Mint; if null (first call), mint with the last input/provider
                    var mint = entry.Mint ?? (ct => provider(lastInput, ct));

                    var minted = await mint(CancellationToken.None).ConfigureAwait(false);
                    if (minted != null && !string.IsNullOrEmpty(minted.AttestationToken))
                    {
                        var now2 = DateTimeOffset.UtcNow;
                        entry.Token = minted.AttestationToken;
                        entry.ExpiresOnUtc = now2 + s_defaultTtl;
                        entry.RefreshOnUtc = now2 + s_halfTime;
                        ScheduleTimer(entry); // push next half-time
                    }
                    else
                    {
                        // Best-effort retry before expiry
                        ScheduleRetry(entry, s_bgRetryBackoff);
                    }
                }
                catch
                {
                    // Swallow background errors; keep current token; try again later
                    ScheduleRetry(entry, s_bgRetryBackoff);
                }
                finally
                {
                    entry.Gate.Release();
                }
            });
        }

        private static void ScheduleTimer(Entry entry)
        {
            var due = entry.RefreshOnUtc - DateTimeOffset.UtcNow;
            if (due < TimeSpan.Zero)
                due = TimeSpan.Zero;

            int dueMs = SafeMs(due);
            if (entry.RefreshTimer == null)
            {
                entry.RefreshTimer = new Timer(TimerCallback, entry, dueMs, Timeout.Infinite);
            }
            else
            {
                entry.RefreshTimer.Change(dueMs, Timeout.Infinite);
            }
        }

        private static void ScheduleRetry(Entry entry, TimeSpan delay)
        {
            int dueMs = SafeMs(delay);
            if (entry.RefreshTimer == null)
            {
                entry.RefreshTimer = new Timer(TimerCallback, entry, dueMs, Timeout.Infinite);
            }
            else
            {
                entry.RefreshTimer.Change(dueMs, Timeout.Infinite);
            }
        }

        private static int SafeMs(TimeSpan ts)
        {
            if (ts <= TimeSpan.Zero)
                return 0;
            double ms = ts.TotalMilliseconds;
            if (ms > int.MaxValue)
                return int.MaxValue;
            return (int)ms;
        }

        private static void TimerCallback(object state)
        {
            var entry = (Entry)state;
            // We only schedule; actual minting happens in KickBackgroundRefresh semantics:
            // Acquire lock, check refresh condition again, then mint.
            // Using stored Mint delegate to avoid needing caller context.
            if (entry.Mint == null)
                return; // no way to mint yet
            Task.Run(async () =>
            {
                if (!entry.Gate.Wait(0))
                    return;
                try
                {
                    var now = DateTimeOffset.UtcNow;
                    if (now < entry.RefreshOnUtc)
                        return; // not due anymore (rescheduled)
                    var minted = await entry.Mint(CancellationToken.None).ConfigureAwait(false);
                    if (minted != null && !string.IsNullOrEmpty(minted.AttestationToken))
                    {
                        var now2 = DateTimeOffset.UtcNow;
                        entry.Token = minted.AttestationToken;
                        entry.ExpiresOnUtc = now2 + s_defaultTtl;
                        entry.RefreshOnUtc = now2 + s_halfTime;
                        ScheduleTimer(entry);
                    }
                    else
                    {
                        ScheduleRetry(entry, s_bgRetryBackoff);
                    }
                }
                catch
                {
                    ScheduleRetry(entry, s_bgRetryBackoff);
                }
                finally
                {
                    entry.Gate.Release();
                }
            });
        }

        // Per-key state
        private sealed class Entry : IDisposable
        {
            internal Entry(long key) { Key = key; Gate = new SemaphoreSlim(1, 1); }
            internal long Key;
            internal string Token;                         // opaque JWT (never parsed)
            internal DateTimeOffset ExpiresOnUtc;
            internal DateTimeOffset RefreshOnUtc;
            internal SemaphoreSlim Gate;
            internal Timer RefreshTimer;
            internal Func<CancellationToken, Task<AttestationTokenResponse>> Mint; // stored mint delegate

            public void Dispose()
            {
                try
                { RefreshTimer?.Dispose(); }
                catch { }
                try
                { Gate?.Dispose(); }
                catch { }
            }
        }
    }
}
