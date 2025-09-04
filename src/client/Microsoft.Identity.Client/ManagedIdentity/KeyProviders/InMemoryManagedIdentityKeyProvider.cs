// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.KeyProviders
{
    /// <summary>
    /// In-memory managed identity key provider.
    /// </summary>
    internal sealed class InMemoryManagedIdentityKeyProvider : IManagedIdentityKeyProvider
    {
        private static readonly SemaphoreSlim s_once = new(1, 1);
        private volatile ManagedIdentityKeyInfo _cached;

        public async Task<ManagedIdentityKeyInfo> GetOrCreateKeyAsync(
            ILoggerAdapter logger,
            CancellationToken ct)
        {
            // Return cached if available
            if (_cached is not null)
            {
                logger?.Info("[MI][InMemoryKeyProvider] Returning cached key.");
                return _cached;
            }

            // Ensure only one creation at a time
            logger?.Verbose(() => "[MI][InMemoryKeyProvider] Waiting on creation semaphore.");
            await s_once.WaitAsync(ct).ConfigureAwait(false);

            try
            {
                if (_cached is not null)
                {
                    logger?.Verbose(() => "[MI][InMemoryKeyProvider] Cached key created while waiting; returning it.");
                    return _cached;
                }

                if (ct.IsCancellationRequested)
                {
                    logger?.Verbose(() => "[MI][InMemoryKeyProvider] Cancellation requested after entering critical section.");
                    ct.ThrowIfCancellationRequested();
                }

                logger?.Verbose(() => "[MI][InMemoryKeyProvider] Starting RSA key creation.");
                RSA rsa = null;
                string message;

                try
                {
                    rsa = CreateRsaKeyPair();
                    message = "In-memory RSA key created for Managed Identity authentication.";
                    logger?.Info("[MI][InMemoryKeyProvider] RSA key created (2048).");
                }
                catch (Exception ex)
                {
                    message = $"Failed to create in-memory RSA key: {ex.GetType().Name} - {ex.Message}";
                    logger?.WarningPii(
                        $"[MI][InMemoryKeyProvider] Exception during RSA creation: {ex}",
                        $"[MI][InMemoryKeyProvider] Exception during RSA creation: {ex.GetType().Name}");
                }

                _cached = new ManagedIdentityKeyInfo(rsa, ManagedIdentityKeyType.InMemory, message);

                logger?.Verbose(() =>
                    $"[MI][InMemoryKeyProvider] Caching key. Success={(rsa != null)}. HasMessage={!string.IsNullOrEmpty(message)}.");

                return _cached;
            }
            finally
            {
                s_once.Release();
            }
        }

        private static RSA CreateRsaKeyPair()
        {
            RSA rsa;
#if NETFRAMEWORK
            // .NET Framework (Windows): use RSACng 
            rsa = new RSACng();
#else
            // Cross‑platform: RSA.Create() -> CNG (Windows) / OpenSSL (Linux).
            rsa = RSA.Create();
#endif
            rsa.KeySize = 2048;
            return rsa;
        }
    }
}
