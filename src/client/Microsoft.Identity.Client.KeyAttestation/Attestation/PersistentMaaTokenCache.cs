// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

#if SUPPORTS_SYSTEM_TEXT_JSON
using System.Text.Json;
using System.Text.Json.Nodes;
#else
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;
#endif

namespace Microsoft.Identity.Client.KeyAttestation.Attestation
{
    /// <summary>
    /// Windows-only file-based persistent cache for MAA attestation tokens.
    /// Stores tokens in %LOCALAPPDATA%\Microsoft\MSAL\MaaTokenCache.
    /// Uses InterprocessLock for cross-process coordination.
    /// All operations are best-effort with 300ms timeout - never blocks token acquisition.
    /// On non-Windows platforms, this is a no-op implementation.
    /// </summary>
    internal sealed class PersistentMaaTokenCache : IPersistentMaaTokenCache
    {
        private static readonly bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static readonly string s_cacheDirectory = GetCacheDirectory();
        private static readonly TimeSpan s_operationTimeout = TimeSpan.FromMilliseconds(300);

        private static string GetCacheDirectory()
        {
            if (!s_isWindows)
                return null;

            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrWhiteSpace(localAppData))
                    return null;

                string cacheDir = Path.Combine(localAppData, "Microsoft", "MSAL", "MaaTokenCache");
                return cacheDir;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to read a cached token from disk.
        /// </summary>
        public bool TryRead(string cacheKey, out MaaTokenCacheEntry entry, Action<string> logVerbose)
        {
            entry = null;

            if (!s_isWindows || string.IsNullOrWhiteSpace(s_cacheDirectory))
            {
                logVerbose?.Invoke("[MaaTokenCache] Persistent cache not available (non-Windows or no cache directory).");
                return false;
            }

            string fileName = GetCacheFileName(cacheKey);
            string filePath = Path.Combine(s_cacheDirectory, fileName);

            bool success = false;

            // Use InterprocessLock with short timeout for cross-process coordination
            InterprocessLock.TryWithAliasLock(
                alias: cacheKey,
                timeout: s_operationTimeout,
                action: () =>
                {
                    try
                    {
                        if (!File.Exists(filePath))
                        {
                            logVerbose?.Invoke($"[MaaTokenCache] Cache file not found: {fileName}");
                            return;
                        }

                        string json = File.ReadAllText(filePath, Encoding.UTF8);
                        if (string.IsNullOrWhiteSpace(json))
                        {
                            logVerbose?.Invoke("[MaaTokenCache] Cache file is empty.");
                            return;
                        }

#if SUPPORTS_SYSTEM_TEXT_JSON
                        var jsonObj = JsonNode.Parse(json)?.AsObject();
                        if (jsonObj == null)
                        {
                            logVerbose?.Invoke("[MaaTokenCache] Failed to parse cache file JSON.");
                            return;
                        }

                        if (!jsonObj.TryGetPropertyValue("token", out var tokenNode) ||
                            !jsonObj.TryGetPropertyValue("issuedAt", out var iatNode) ||
                            !jsonObj.TryGetPropertyValue("expiresAt", out var expNode))
                        {
                            logVerbose?.Invoke("[MaaTokenCache] Cache file missing required fields.");
                            return;
                        }

                        string token = tokenNode.GetValue<string>();
                        long iatUnix = iatNode.GetValue<long>();
                        long expUnix = expNode.GetValue<long>();
#else
                        var jsonObj = JObject.Parse(json);
                        if (jsonObj == null)
                        {
                            logVerbose?.Invoke("[MaaTokenCache] Failed to parse cache file JSON.");
                            return;
                        }

                        if (!jsonObj.TryGetValue("token", out var tokenToken) ||
                            !jsonObj.TryGetValue("issuedAt", out var iatToken) ||
                            !jsonObj.TryGetValue("expiresAt", out var expToken))
                        {
                            logVerbose?.Invoke("[MaaTokenCache] Cache file missing required fields.");
                            return;
                        }

                        string token = tokenToken.Value<string>();
                        long iatUnix = iatToken.Value<long>();
                        long expUnix = expToken.Value<long>();
#endif

                        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(iatUnix);
                        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix);

                        // Only return if not expired
                        if (expiresAt > DateTimeOffset.UtcNow)
                        {
                            entry = new MaaTokenCacheEntry(token, issuedAt, expiresAt);
                            success = true;
                            logVerbose?.Invoke($"[MaaTokenCache] Successfully read from persistent cache: {fileName}");
                        }
                        else
                        {
                            logVerbose?.Invoke($"[MaaTokenCache] Cache file expired: {fileName}");
                            // Delete expired file
                            try { File.Delete(filePath); } catch { /* best-effort */ }
                        }
                    }
                    catch (Exception ex)
                    {
                        logVerbose?.Invoke($"[MaaTokenCache] Read failed: {ex.Message}");
                    }
                },
                logVerbose: s => logVerbose?.Invoke(s));

            return success;
        }

        /// <summary>
        /// Attempts to write a token to disk.
        /// </summary>
        public void TryWrite(string cacheKey, MaaTokenCacheEntry entry, Action<string> logVerbose)
        {
            if (!s_isWindows || string.IsNullOrWhiteSpace(s_cacheDirectory))
            {
                return;
            }

            string fileName = GetCacheFileName(cacheKey);
            string filePath = Path.Combine(s_cacheDirectory, fileName);

            InterprocessLock.TryWithAliasLock(
                alias: cacheKey,
                timeout: s_operationTimeout,
                action: () =>
                {
                    try
                    {
                        // Ensure directory exists
                        Directory.CreateDirectory(s_cacheDirectory);

                        // Create JSON representation
#if SUPPORTS_SYSTEM_TEXT_JSON
                        var jsonObj = new JsonObject
                        {
                            ["token"] = entry.Token,
                            ["issuedAt"] = entry.IssuedAt.ToUnixTimeSeconds(),
                            ["expiresAt"] = entry.ExpiresAt.ToUnixTimeSeconds(),
                            ["cacheKey"] = cacheKey
                        };
                        string json = jsonObj.ToJsonString();
#else
                        var jsonObj = new JObject
                        {
                            ["token"] = entry.Token,
                            ["issuedAt"] = entry.IssuedAt.ToUnixTimeSeconds(),
                            ["expiresAt"] = entry.ExpiresAt.ToUnixTimeSeconds(),
                            ["cacheKey"] = cacheKey
                        };
                        string json = jsonObj.ToString(Formatting.None);
#endif

                        File.WriteAllText(filePath, json, Encoding.UTF8);
                        logVerbose?.Invoke($"[MaaTokenCache] Successfully wrote to persistent cache: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        logVerbose?.Invoke($"[MaaTokenCache] Write failed: {ex.Message}");
                    }
                },
                logVerbose: s => logVerbose?.Invoke(s));
        }

        /// <summary>
        /// Attempts to delete expired cache files.
        /// </summary>
        public void TryDelete(string cacheKey, Action<string> logVerbose)
        {
            if (!s_isWindows || string.IsNullOrWhiteSpace(s_cacheDirectory))
            {
                return;
            }

            InterprocessLock.TryWithAliasLock(
                alias: cacheKey,
                timeout: s_operationTimeout,
                action: () =>
                {
                    try
                    {
                        if (!Directory.Exists(s_cacheDirectory))
                            return;

                        var now = DateTimeOffset.UtcNow;
                        int deleted = 0;

                        // Delete all expired cache files
                        foreach (var file in Directory.GetFiles(s_cacheDirectory, "*.json"))
                        {
                            try
                            {
                                string json = File.ReadAllText(file, Encoding.UTF8);
                                
#if SUPPORTS_SYSTEM_TEXT_JSON
                                var jsonObj = JsonNode.Parse(json)?.AsObject();
                                if (jsonObj != null && jsonObj.TryGetPropertyValue("expiresAt", out var expNode))
                                {
                                    long expUnix = expNode.GetValue<long>();
                                    var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix);
                                    if (expiresAt <= now)
                                    {
                                        File.Delete(file);
                                        deleted++;
                                    }
                                }
#else
                                var jsonObj = JObject.Parse(json);
                                if (jsonObj != null && jsonObj.TryGetValue("expiresAt", out var expToken))
                                {
                                    long expUnix = expToken.Value<long>();
                                    var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix);
                                    if (expiresAt <= now)
                                    {
                                        File.Delete(file);
                                        deleted++;
                                    }
                                }
#endif
                            }
                            catch
                            {
                                // Best-effort; skip files we can't process
                            }
                        }

                        if (deleted > 0)
                        {
                            logVerbose?.Invoke($"[MaaTokenCache] Deleted {deleted} expired cache file(s).");
                        }
                    }
                    catch (Exception ex)
                    {
                        logVerbose?.Invoke($"[MaaTokenCache] Delete failed: {ex.Message}");
                    }
                },
                logVerbose: s => logVerbose?.Invoke(s));
        }

        /// <summary>
        /// Generates a safe file name from the cache key using SHA-256 hash.
        /// </summary>
        private static string GetCacheFileName(string cacheKey)
        {
            try
            {
                using var sha256 = SHA256.Create();
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(cacheKey));
                string hashHex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                return $"maa_{hashHex.Substring(0, 32)}.json";
            }
            catch
            {
                // Fallback to simple hash if SHA256 fails
                return $"maa_{Math.Abs(cacheKey.GetHashCode())}.json";
            }
        }
    }
}
