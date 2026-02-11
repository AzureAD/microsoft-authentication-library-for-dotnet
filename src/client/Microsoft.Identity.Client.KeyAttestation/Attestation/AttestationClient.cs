// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.KeyAttestation.Attestation
{
    /// <summary>
    /// Managed façade for <c>AttestationClientLib.dll</c>. Holds initialization state,
    /// does ref-count hygiene on <see cref="SafeNCryptKeyHandle"/>, and returns a JWT.
    /// Supports optional token caching with intelligent refresh at 50% remaining lifetime.
    /// </summary>
    internal sealed class AttestationClient : IDisposable
    {
        private bool _initialized;
        private static readonly Lazy<MaaTokenCache> s_defaultCache = new Lazy<MaaTokenCache>(
            () => new MaaTokenCache(new PersistentMaaTokenCache()),
            LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// AttestationClient constructor. Relies on the default OS loader to locate the native DLL.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public AttestationClient()
        {
            string dllError = NativeDiagnostics.ProbeNativeDll();
            // intentionally not throwing on dllError

            // Load & initialize (logger is required by native lib)
            var info = new AttestationClientLib.AttestationLogInfo
            {
                Log = AttestationLogger.ConsoleLogger,
                Ctx = IntPtr.Zero
            };

            _initialized = AttestationClientLib.InitAttestationLib(ref info) == 0;
            if (!_initialized)
                throw new InvalidOperationException("Failed to initialize AttestationClientLib.");
        }

        /// <summary>
        /// Calls the native <c>AttestKeyGuardImportKey</c> and returns a structured result.
        /// </summary>
        public AttestationResult Attest(string endpoint,
                                        SafeNCryptKeyHandle keyHandle,
                                        string clientId)
        {
            if (!_initialized)
                return new(AttestationStatus.NotInitialized, null, -1,
                    "Native library not initialized.");

            IntPtr buf = IntPtr.Zero;
            bool addRef = false;

            try
            {
                keyHandle.DangerousAddRef(ref addRef);

                int rc = AttestationClientLib.AttestKeyGuardImportKey(
                    endpoint, null, null, keyHandle, out buf, clientId);

                if (rc != 0)
                    return new(AttestationStatus.NativeError, null, rc, null);

                if (buf == IntPtr.Zero)
                    return new(AttestationStatus.TokenEmpty, null, 0,
                        "rc==0 but token buffer was null.");

                string jwt = Marshal.PtrToStringAnsi(buf)!;
                return new(AttestationStatus.Success, jwt, 0, null);
            }
            catch (DllNotFoundException ex)
            {
                return new(AttestationStatus.Exception, null, -1,
                    $"Native DLL not found: {ex.Message}");
            }
            catch (BadImageFormatException ex)
            {
                return new(AttestationStatus.Exception, null, -1,
                    $"Architecture mismatch (x86/x64) or corrupted DLL: {ex.Message}");
            }
            catch (SEHException ex)
            {
                return new(AttestationStatus.Exception, null, -1,
                    $"Native library raised SEHException: {ex.Message}");
            }
            catch (Exception ex)
            {
                return new(AttestationStatus.Exception, null, -1, ex.Message);
            }
            finally
            {
                if (buf != IntPtr.Zero)
                    AttestationClientLib.FreeAttestationToken(buf);
                if (addRef)
                    keyHandle.DangerousRelease();
            }
        }

        /// <summary>
        /// Calls the native attestation with caching support.
        /// Tokens are cached and automatically refreshed when less than 50% of their lifetime remains.
        /// </summary>
        /// <param name="endpoint">The attestation endpoint URL.</param>
        /// <param name="keyHandle">The CNG key handle to attest.</param>
        /// <param name="clientId">The client ID for attestation.</param>
        /// <param name="useCache">Whether to use token caching (default: true).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The attestation JWT token.</returns>
        public async Task<string> AttestAsync(
            string endpoint,
            SafeNCryptKeyHandle keyHandle,
            string clientId,
            bool useCache = true,
            CancellationToken cancellationToken = default)
        {
            if (!useCache)
            {
                // Direct call without caching
                var result = Attest(endpoint, keyHandle, clientId);
                if (result.Status != AttestationStatus.Success || string.IsNullOrWhiteSpace(result.Jwt))
                {
                    throw new InvalidOperationException(
                        $"Attestation failed: status={result.Status}, nativeRc={result.NativeErrorCode}, msg={result.ErrorMessage}");
                }
                return result.Jwt;
            }

            // Build cache key from endpoint + clientId + key identifier
            string cacheKey = BuildCacheKey(endpoint, keyHandle, clientId);

            // Use the default cache with factory
            return await s_defaultCache.Value.GetOrCreateAsync(
                cacheKey,
                factory: () => Task.FromResult(Attest(endpoint, keyHandle, clientId)),
                cancellationToken,
                logVerbose: null).ConfigureAwait(false);
        }

        /// <summary>
        /// Builds a cache key from the attestation parameters.
        /// The key handle pointer is used as a unique identifier for the key.
        /// </summary>
        private static string BuildCacheKey(string endpoint, SafeNCryptKeyHandle keyHandle, string clientId)
        {
            // Use the handle pointer as part of the cache key
            // This ensures different keys get different cache entries
            string keyId = keyHandle.DangerousGetHandle().ToString("X");
            return $"{endpoint}|{clientId}|{keyId}";
        }

        /// <summary>
        /// Gets the default MAA token cache instance for testing purposes.
        /// </summary>
        internal static MaaTokenCache GetDefaultCache()
        {
            return s_defaultCache.Value;
        }

        /// <summary>
        /// Disposes the client, releasing any resources and un-initializing the native library.
        /// </summary>
        public void Dispose()
        {
            if (_initialized)
            {
                AttestationClientLib.UninitAttestationLib();
                _initialized = false;
            }
            GC.SuppressFinalize(this);
        }
    }
}
