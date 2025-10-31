// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Cross-process lock based on a per-alias named mutex.
    /// </summary>
    internal static class InterprocessLock
    {
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
                    using var m = new Mutex(false, name);
                    bool entered;
                    try
                    {
                        entered = m.WaitOne(timeout);
                    }
                    catch (AbandonedMutexException)
                    {
                        entered = true; // prior holder crashed
                    }

                    if (!entered)
                    {
                        logVerbose?.Invoke($"[PersistentCert] Skip persist (lock busy '{name}').");
                        return false;
                    }

                    try
                    { action(); }
                    finally
                    {
                        try
                        { m.ReleaseMutex(); }
                        catch { /* best-effort */ }
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

        private static string Canonicalize(string alias) => (alias ?? string.Empty).Trim().ToUpperInvariant();

        private static string HashAlias(string s)
        {
            try
            {
                var hex = new CommonCryptographyManager().CreateSha256HashHex(s);
                // Truncate to 32 chars to fit mutex name length limits
                return string.IsNullOrEmpty(hex) ? "0" : (hex.Length > 32 ? hex.Substring(0, 32) : hex);
            }
            catch
            {
                return "0";
            }
        }
    }
}
