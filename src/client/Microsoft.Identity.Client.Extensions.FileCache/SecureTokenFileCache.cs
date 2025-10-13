// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Microsoft.Identity.Client.Extensions.FileCache
{
    /// <summary>
    /// File-backed cache with:
    ///  - Per-key in-process gate + cross-process named mutex
    ///  - Atomic writes (replace/move)
    ///  - No encryption (plain file write)
    ///  - Freshness based solely on expires_on / refresh_on supplied by caller
    ///  - Robust to partial/corrupt files (treated as miss)
    /// </summary>
    public sealed class SecureTokenFileCache : ISecureTokenCache
    {
        private const string DefaultTemplate = "{bucket}_{keyId}.bin";
        private static readonly TimeSpan s_defaultExpirySkew = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan s_defaultRefreshLockTimeout = TimeSpan.FromSeconds(60);

        private readonly IRefreshLockFactory _lockFactory = new NamedMutexRefreshLockFactory();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _localGates = new ConcurrentDictionary<string, SemaphoreSlim>();

        private readonly string _baseDirectory;     // empty => resolve default per OS
        private readonly string _fileNameTemplate;  // must contain {bucket} and {keyId}
        private readonly TimeSpan _expirySkew = s_defaultExpirySkew;
        private readonly TimeSpan _refreshLockTimeout = s_defaultRefreshLockTimeout;

        /// <summary>
        /// Secure token cache with default settings:
        /// </summary>
        public SecureTokenFileCache()
        {
            _baseDirectory = string.Empty;
            _fileNameTemplate = DefaultTemplate;
        }

        /// <summary>
        /// Secure token cache with custom settings:
        /// </summary>
        /// <param name="baseDirectory"></param>
        /// <param name="fileNameTemplate"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SecureTokenFileCache(string baseDirectory, string fileNameTemplate)
            : this()
        {
            if (fileNameTemplate == null)
                throw new ArgumentNullException(nameof(fileNameTemplate));
            _baseDirectory = baseDirectory ?? string.Empty;
            _fileNameTemplate = fileNameTemplate.Length == 0 ? DefaultTemplate : fileNameTemplate;
        }

        /// <summary>
        /// Tries to read a valid payload for the specified <paramref name="bucket"/> and <paramref name="keyId"/>.
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="keyId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public System.Threading.Tasks.Task<byte[]> TryReadAsync(string bucket, string keyId, CancellationToken ct)
        {
            if (bucket == null)
                throw new ArgumentNullException(nameof(bucket));
            if (keyId == null)
                throw new ArgumentNullException(nameof(keyId));
            var path = BuildPath(bucket, keyId);

            try
            {
                if (!File.Exists(path))
                    return CompletedAsync(Array.Empty<byte>());

                var data = File.ReadAllBytes(path);
                Envelope env;
                if (!TryDeserializeEnvelope(data, out env))
                    return CompletedAsync(Array.Empty<byte>());

                var now = DateTimeOffset.UtcNow;
                if (IsExpired(env, now, _expirySkew))
                    return CompletedAsync(Array.Empty<byte>());

                return CompletedAsync(env.Payload);
            }
            catch (IOException)
            {
                return CompletedAsync(Array.Empty<byte>());
            }
        }

        /// <summary>
        /// Writes (or overwrites) a payload and its timing metadata atomically.
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="keyId"></param>
        /// <param name="value"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public System.Threading.Tasks.Task WriteAsync(string bucket, string keyId, CacheValue value, CancellationToken ct)
        {
            if (bucket == null)
                throw new ArgumentNullException(nameof(bucket));
            if (keyId == null)
                throw new ArgumentNullException(nameof(keyId));

            var path = BuildPath(bucket, keyId);
            var dir = Path.GetDirectoryName(path);
            if (dir == null)
                throw new InvalidOperationException("Invalid cache directory.");
            Directory.CreateDirectory(dir);

            var env = new Envelope
            {
                Ver = 1,
                ExpiresOnUnix = value.ExpiresOnUtc.ToUnixTimeSeconds(),
                HasRefreshOnUnix = value.HasRefreshOnUtc,
                RefreshOnUnix = value.HasRefreshOnUtc ? value.RefreshOnUtc.ToUnixTimeSeconds() : 0L,
                Payload = value.Payload ?? Array.Empty<byte>()
            };

            var blob = SerializeEnvelope(env);

            var tmp = path + ".tmp";
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                fs.Write(blob, 0, blob.Length);
                fs.Flush(true);
            }

            try
            {
#if NET8_0_OR_GREATER
                File.Move(tmp, path, true); // atomic within same directory
#elif NETSTANDARD2_0
                TryReplaceOrMove(tmp, path);
#else
                // Fallback for any other TFM; we don't expect to hit this in your targets.
                TryReplaceOrMove(tmp, path);
#endif
            }
            catch
            {
                SafeDelete(tmp);
                throw;
            }

            return CompletedAsync();
        }

        /// <summary>
        /// get or create with cross-process synchronization
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="keyId"></param>
        /// <param name="factory"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public System.Threading.Tasks.Task<byte[]> GetOrCreateAsync(
            string bucket,
            string keyId,
            Func<CancellationToken, System.Threading.Tasks.Task<CacheValue>> factory,
            CancellationToken ct)
        {
            if (bucket == null)
                throw new ArgumentNullException(nameof(bucket));
            if (keyId == null)
                throw new ArgumentNullException(nameof(keyId));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            return GetOrCreateCoreAsync(bucket, keyId, factory, ct);
        }

        // ---------- internals ----------

        private async System.Threading.Tasks.Task<byte[]> GetOrCreateCoreAsync(
            string bucket,
            string keyId,
            Func<CancellationToken, System.Threading.Tasks.Task<CacheValue>> factory,
            CancellationToken ct)
        {
            var state = TryReadState(bucket, keyId);
            if (state != null && !state.IsExpired && !state.NeedsRefresh)
                return state.Envelope.Payload;

            var gateKey = bucket + "|" + keyId;
            var gate = _localGates.GetOrAdd(gateKey, k => new SemaphoreSlim(1, 1));
            await gate.WaitAsync().ConfigureAwait(false);
            try
            {
                state = TryReadState(bucket, keyId);
                if (state != null && !state.IsExpired && !state.NeedsRefresh)
                    return state.Envelope.Payload;

                var scope = ResolveBaseDirectory();
                using (var refreshLock = _lockFactory.ForKey(scope, bucket, keyId))
                {
                    var acquired = refreshLock.TryEnter(_refreshLockTimeout);
                    if (!acquired)
                    {
                        if (state != null && !state.IsExpired)
                            return state.Envelope.Payload;
                        throw new TimeoutException("Could not acquire refresh lock.");
                    }

                    try
                    {
                        state = TryReadState(bucket, keyId);
                        if (state != null && !state.IsExpired && !state.NeedsRefresh)
                            return state.Envelope.Payload;

                        var mustRefresh = state == null || state.IsExpired;

                        CacheValue result;
                        try
                        {
                            result = await factory(ct).ConfigureAwait(false);
                        }
                        catch
                        {
                            if (!mustRefresh && state != null && state.Envelope.Payload != null)
                                return state.Envelope.Payload;
                            throw;
                        }

                        await WriteAsync(bucket, keyId, result, ct).ConfigureAwait(false);
                        return result.Payload;
                    }
                    finally
                    {
                        refreshLock.Exit();
                    }
                }
            }
            finally
            {
                gate.Release();
            }
        }

        private sealed class Envelope
        {
            public int Ver;
            public long ExpiresOnUnix;
            public bool HasRefreshOnUnix;
            public long RefreshOnUnix;
            public byte[] Payload = Array.Empty<byte>();
        }

        private sealed class ReadState
        {
            public Envelope Envelope;
            public bool IsExpired;
            public bool NeedsRefresh;
        }

        private ReadState TryReadState(string bucket, string keyId)
        {
            var path = BuildPath(bucket, keyId);
            if (!File.Exists(path))
                return null;

            try
            {
                var data = File.ReadAllBytes(path);
                Envelope env;
                if (!TryDeserializeEnvelope(data, out env))
                    return null;

                var now = DateTimeOffset.UtcNow;
                var expired = IsExpired(env, now, _expirySkew);
                var needsRefresh = !expired && IsPastRefresh(env, now, _expirySkew);

                var st = new ReadState();
                st.Envelope = env;
                st.IsExpired = expired;
                st.NeedsRefresh = needsRefresh;
                return st;
            }
            catch (IOException)
            {
                return null;
            }
        }

        // Binary envelope: "XFC2" | v(1) | exp(int64) | hasRef(byte) | [ref(int64)] | len(int32) | payload

        private static byte[] SerializeEnvelope(Envelope env)
        {
            int size = 4 + 1 + 8 + 1 + (env.HasRefreshOnUnix ? 8 : 0) + 4 + env.Payload.Length;
            var buf = new byte[size];
            int o = 0;

            buf[o++] = (byte)'X';
            buf[o++] = (byte)'F';
            buf[o++] = (byte)'C';
            buf[o++] = (byte)'2';
            buf[o++] = 1;

            WriteInt64LE(buf, ref o, env.ExpiresOnUnix);
            buf[o++] = env.HasRefreshOnUnix ? (byte)1 : (byte)0;
            if (env.HasRefreshOnUnix)
                WriteInt64LE(buf, ref o, env.RefreshOnUnix);

            WriteInt32LE(buf, ref o, env.Payload.Length);
            Buffer.BlockCopy(env.Payload, 0, buf, o, env.Payload.Length);

            return buf;
        }

        private static bool TryDeserializeEnvelope(byte[] data, out Envelope env)
        {
            env = null;
            if (data == null || data.Length < 4 + 1 + 8 + 1 + 4)
                return false;

            int o = 0;
            if (data[o++] != (byte)'X' || data[o++] != (byte)'F' || data[o++] != (byte)'C' || data[o++] != (byte)'2')
                return false;

            byte ver = data[o++];
            if (ver != 1)
                return false;

            long exp = ReadInt64LE(data, ref o);
            byte hasRef = data[o++];
            long refresh = 0;
            if (hasRef != 0)
            {
                if (o + 8 > data.Length)
                    return false;
                refresh = ReadInt64LE(data, ref o);
            }

            int len = ReadInt32LE(data, ref o);
            if (len < 0 || o + len > data.Length)
                return false;

            var payload = new byte[len];
            Buffer.BlockCopy(data, o, payload, 0, len);

            env = new Envelope
            {
                Ver = 1,
                ExpiresOnUnix = exp,
                HasRefreshOnUnix = hasRef != 0,
                RefreshOnUnix = refresh,
                Payload = payload
            };
            return true;
        }

        private static bool IsExpired(Envelope env, DateTimeOffset now, TimeSpan skew)
        {
            var expUtc = DateTimeOffset.FromUnixTimeSeconds(env.ExpiresOnUnix);
            return now + skew >= expUtc;
        }

        private static bool IsPastRefresh(Envelope env, DateTimeOffset now, TimeSpan skew)
        {
            if (env.HasRefreshOnUnix)
            {
                var r = DateTimeOffset.FromUnixTimeSeconds(env.RefreshOnUnix);
                return now >= r;
            }
            var exp = DateTimeOffset.FromUnixTimeSeconds(env.ExpiresOnUnix);
            return now >= (exp - skew);
        }

        private static void WriteInt64LE(byte[] b, ref int o, long v)
        {
            unchecked
            {
                b[o++] = (byte)v;
                b[o++] = (byte)(v >> 8);
                b[o++] = (byte)(v >> 16);
                b[o++] = (byte)(v >> 24);
                b[o++] = (byte)(v >> 32);
                b[o++] = (byte)(v >> 40);
                b[o++] = (byte)(v >> 48);
                b[o++] = (byte)(v >> 56);
            }
        }

        private static long ReadInt64LE(byte[] b, ref int o)
        {
            unchecked
            {
                long v = (long)b[o]
                       | ((long)b[o + 1] << 8)
                       | ((long)b[o + 2] << 16)
                       | ((long)b[o + 3] << 24)
                       | ((long)b[o + 4] << 32)
                       | ((long)b[o + 5] << 40)
                       | ((long)b[o + 6] << 48)
                       | ((long)b[o + 7] << 56);
                o += 8;
                return v;
            }
        }

        private static void WriteInt32LE(byte[] b, ref int o, int v)
        {
            unchecked
            {
                b[o++] = (byte)v;
                b[o++] = (byte)(v >> 8);
                b[o++] = (byte)(v >> 16);
                b[o++] = (byte)(v >> 24);
            }
        }

        private static int ReadInt32LE(byte[] b, ref int o)
        {
            unchecked
            {
                int v = (int)b[o]
                      | ((int)b[o + 1] << 8)
                      | ((int)b[o + 2] << 16)
                      | ((int)b[o + 3] << 24);
                o += 4;
                return v;
            }
        }

        private string BuildPath(string bucket, string keyId)
        {
            var baseDir = ResolveBaseDirectory();
            var file = _fileNameTemplate
                .Replace("{bucket}", Sanitize(bucket))
                .Replace("{keyId}", Sanitize(keyId));
            return Path.Combine(baseDir, file);
        }

        private string ResolveBaseDirectory()
        {
            if (!string.IsNullOrEmpty(_baseDirectory))
                return EnsureDirectory(_baseDirectory);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return EnsureDirectory(Path.Combine(local, "Microsoft", "Identity", "FileCache"));
            }
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return EnsureDirectory(Path.Combine(home, ".msal", "filecache"));
        }

        private static string EnsureDirectory(string dir)
        {
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static string Sanitize(string s)
        {
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                var ch = s[i];
                sb.Append(char.IsLetterOrDigit(ch) ? ch : '-');
            }
            return sb.ToString();
        }

        private static void TryReplaceOrMove(string tmp, string path)
        {
            // netstandard2.0 path: prefer Replace if available; fall back to delete+move
            try
            {
                if (File.Exists(path))
                {
                    File.Replace(tmp, path, null); // NS2.0 has the 3‑arg overload
                }
                else
                {
                    File.Move(tmp, path);
                }
            }
            catch
            {
                try
                { if (File.Exists(path)) File.Delete(path); }
                catch { /* ignore */ }
                File.Move(tmp, path);
            }
        }

        private static void SafeDelete(string p)
        {
            try
            { if (File.Exists(p)) File.Delete(p); }
            catch { /* ignore */ }
        }

        private static System.Threading.Tasks.Task<byte[]> CompletedAsync(byte[] result)
            => System.Threading.Tasks.Task.FromResult(result);

        private static System.Threading.Tasks.Task CompletedAsync()
            => System.Threading.Tasks.Task.CompletedTask;
    }
}
