// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Microsoft.Identity.Client.KeyAttestation.Attestation
{
    /// <summary>
    /// Executes an action under a cross-process, per-key mutex.
    /// Attempts Global\ first, then falls back to Local\ if Global\ is denied.
    /// Best-effort: never throws exceptions that block token acquisition.
    /// </summary>
    internal static class MaaInterprocessLock
    {
        /// <summary>
        /// Tries to execute an action under a named mutex lock.
        /// </summary>
        public static bool TryWithLock(
            string key,
            TimeSpan timeout,
            Action action,
            Action<string> logVerbose)
        {
            var globalName = GetMutexNameForKey(key, preferGlobal: true);
            var localName = GetMutexNameForKey(key, preferGlobal: false);

            // Try Global\ first, then Local\ if Global\ is unauthorized
            bool TryScope(string name, out bool unauthorized)
            {
                unauthorized = false;
                try
                {
                    using var mutex = new Mutex(initiallyOwned: false, name);

                    bool entered;
                    var waitTimer = Stopwatch.StartNew();
                    try
                    {
                        entered = mutex.WaitOne(timeout);
                    }
                    catch (AbandonedMutexException ex)
                    {
                        entered = true;
                        logVerbose?.Invoke($"[MaaTokenCache] Abandoned mutex '{name}', treating as acquired. {ex.Message}");
                    }
                    finally
                    {
                        waitTimer.Stop();
                    }

                    if (!entered)
                    {
                        logVerbose?.Invoke(
                            $"[MaaTokenCache] Skip persist (lock busy '{name}', waited {waitTimer.Elapsed.TotalMilliseconds:F0} ms).");
                        return false;
                    }

                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        logVerbose?.Invoke($"[MaaTokenCache] Action failed under '{name}': {ex.Message}");
                        return false;
                    }
                    finally
                    {
                        try
                        { mutex.ReleaseMutex(); }
                        catch { /* best-effort */ }
                    }

                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    logVerbose?.Invoke($"[MaaTokenCache] No access to mutex scope '{name}', trying next.");
                    unauthorized = true;
                    return false;
                }
                catch (Exception ex)
                {
                    logVerbose?.Invoke($"[MaaTokenCache] Lock failure '{name}': {ex.Message}");
                    return false;
                }
            }

            // Try Global\ first; only fallback to Local\ if Global\ is unauthorized
            if (TryScope(globalName, out var unauthorizedGlobal))
            {
                return true;
            }

            // Fallback only if Global\ is disallowed by ACLs
            if (unauthorizedGlobal)
            {
                if (TryScope(localName, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetMutexNameForKey(string key, bool preferGlobal = true)
        {
            string suffix = HashKey(Canonicalize(key));
            return (preferGlobal ? @"Global\" : @"Local\") + "MSAL_MAA_" + suffix;
        }

        private static string Canonicalize(string key) =>
            (key ?? string.Empty).Trim().ToUpperInvariant();

        private static string HashKey(string s)
        {
            // SHA-256 produces 64 hex characters; truncate to 32 for mutex name length limits
            const int MutexNameHashLength = 32;

            try
            {
                using var sha256 = SHA256.Create();
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(s));
                string hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                
                // Truncate to fit mutex name length limits while maintaining uniqueness
                return hex.Length > MutexNameHashLength 
                    ? hex.Substring(0, MutexNameHashLength) 
                    : hex;
            }
            catch
            {
                // Fallback to simple hash code on error
                return Math.Abs(s.GetHashCode()).ToString();
            }
        }
    }
}
