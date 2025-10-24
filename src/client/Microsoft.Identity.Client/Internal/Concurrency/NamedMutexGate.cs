// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Internal.Concurrency
{
    /// <summary>
    /// Named OS mutex with Global/Local fallback. Best-effort: if unavailable, we fail open.
    /// </summary>
    internal sealed class NamedMutexGate : IDisposable
    {
        private readonly Mutex _mutex;
        private readonly bool _acquired;

        private NamedMutexGate(Mutex m, bool acquired)
        {
            _mutex = m;
            _acquired = acquired;
        }

        public bool Acquired => _acquired;

        public static NamedMutexGate TryAcquire(string name, TimeSpan timeout, ILoggerAdapter logger = null)
        {
            string[] names = { @"Global\" + name, @"Local\" + name, name };

            foreach (var n in names)
            {
                try
                {
                    var m = new Mutex(false, n);
                    bool gotIt;
                    try
                    { gotIt = m.WaitOne(timeout); }
                    catch (AbandonedMutexException) { gotIt = true; }

                    if (gotIt)
                        return new NamedMutexGate(m, true);
                    m.Dispose();
                }
                catch (Exception ex)
                {
                    logger?.Info(() => $"[Mutex] '{n}' unavailable: {ex.Message}");
                }
            }

            // Fail open
            return new NamedMutexGate(null, false);
        }

        public void Dispose()
        {
            if (_acquired)
            {
                try
                { _mutex.ReleaseMutex(); }
                catch { }
            }
            _mutex?.Dispose();
        }

        /// <summary>Short, OS-safe mutex name.</summary>
        public static string BuildName(string prefix, string tenantGuid, string miGuid, string tokenType)
        {
            static string S(string s) => (s ?? string.Empty).Trim().ToLowerInvariant()
                                              .Replace('\\', '_').Replace('/', '_').Replace(':', '_');
            return $"{S(prefix)}_{S(tenantGuid)}_{S(miGuid)}_{S(tokenType)}";
        }
    }
}
