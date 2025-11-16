// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Executes paramref name="action"/ under a cross-process, per-alias mutex.
    /// We attempt 2 namespaces, in order:
    /// 1) <c>Global\</c> — preferred so we dedupe across all sessions on the machine
    ///    (e.g., service + user session). This can be denied by OS policy or missing
    ///    SeCreateGlobalPrivilege in some contexts.
    /// 2) <c>Local\</c> — fallback to still dedupe within the current session when
    ///    <c>Global\</c> is not permitted.
    /// Using both ensures we never throw (persistence is best-effort) while getting
    /// machine-wide dedupe when allowed and session-local dedupe otherwise.
    /// Notes:
    /// - The mutex name is derived from <c>alias</c> (= cacheKey) via SHA-256 hex (truncated)
    ///   to avoid invalid characters / length issues.
    /// - On non-Windows runtimes the Global/Local prefixes are treated as part of the name;
    ///   behavior remains correct but dedupe scope is platform-defined.
    /// - Abandoned mutexes are treated as acquired to avoid blocking after a crash.
    /// </summary>
    internal static class InterprocessLock
    {
        // Prefer Global\ for cross-session dedupe; fall back to Local\
        // if ACLs block Global\ to remain non-throwing.
        public static bool TryWithAliasLock(
            string alias,
            TimeSpan timeout,
            Action action,
            Action<string> logVerbose = null)
        {
            var nameGlobal = GetMutexNameForAlias(alias, preferGlobal: true);
            var nameLocal = GetMutexNameForAlias(alias, preferGlobal: false);

            foreach (var name in new[] { nameGlobal, nameLocal })
            {
                try
                {
                    // Create or open existing. If this throws (e.g. ACL issues),
                    // the outer catch below logs and returns false (best-effort).
                    using var m = new Mutex(initiallyOwned: false, name);

                    bool entered;
                    var sw = Stopwatch.StartNew();

                    try
                    {
                        entered = m.WaitOne(timeout);
                    }
                    catch (AbandonedMutexException ex)
                    {
                        // Prior holder crashed; we still own the mutex now.
                        entered = true;
                        logVerbose?.Invoke(
                            $"[PersistentCert] Abandoned mutex '{name}', treating as acquired. {ex.Message}");
                    }
                    finally
                    {
                        sw.Stop();
                    }

                    if (!entered)
                    {
                        logVerbose?.Invoke(
                            $"[PersistentCert] Skip persist (lock busy '{name}', waited {sw.Elapsed.TotalMilliseconds:F0} ms).");
                        return false;
                    }

                    try
                    {
                        action();
                    }
                    finally
                    {
                        try
                        {
                            m.ReleaseMutex();
                        }
                        catch
                        {
                            // best-effort; do not propagate
                        }
                    }

                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    logVerbose?.Invoke($"[PersistentCert] No access to mutex scope '{name}', trying next.");
                    continue; // try Local if Global blocked
                }
                catch (Exception ex)
                {
                    logVerbose?.Invoke($"[PersistentCert] Lock failure '{name}': {ex.Message}");
                    return false;
                }
            }

            return false;
        }

        public static string GetMutexNameForAlias(string alias, bool preferGlobal = true)
        {
            string suffix = HashAlias(Canonicalize(alias));
            return (preferGlobal ? @"Global\" : @"Local\") + "MSAL_MI_P_" + suffix;
        }

        private static string Canonicalize(string alias) =>
            (alias ?? string.Empty).Trim().ToUpperInvariant();

        private static string HashAlias(string s)
        {
            try
            {
                var hex = new CommonCryptographyManager().CreateSha256HashHex(s);
                // Truncate to 32 chars to fit mutex name length limits
                return string.IsNullOrEmpty(hex)
                    ? "0"
                    : (hex.Length > 32 ? hex.Substring(0, 32) : hex);
            }
            catch
            {
                return "0";
            }
        }
    }
}
