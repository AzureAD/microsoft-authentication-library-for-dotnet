// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.ManagedIdentity.KeyProviders
{
    /// <summary>
    /// In-memory RSA key provider for managed identity authentication.
    /// </summary>
    internal sealed class InMemoryManagedIdentityKeyProvider : IManagedIdentityKeyProvider
    {
        private static readonly SemaphoreSlim s_once = new (1, 1);
        private volatile ManagedIdentityKeyInfo _cachedKey;

        /// <summary>
        /// Asynchronously retrieves or creates an RSA key pair for managed identity authentication.
        /// Uses thread-safe caching to ensure only one key is created per provider instance.
        /// </summary>
        /// <param name="logger">Logger adapter for recording key creation operations and diagnostics.</param>
        /// <param name="ct">Cancellation token to support cooperative cancellation of the key creation process.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a 
        /// <see cref="ManagedIdentityKeyInfo"/> with the RSA key, key type, and provider message.
        /// </returns>
        public async Task<ManagedIdentityKeyInfo> GetOrCreateKeyAsync(
            ILoggerAdapter logger,
            CancellationToken ct)
        {
            // Return cached if available
            if (_cachedKey is not null)
            {
                logger?.Info("[MI][InMemoryKeyProvider] Returning cached key.");
                return _cachedKey;
            }

            // Ensure only one creation at a time
            logger?.Info(() => "[MI][InMemoryKeyProvider] Waiting on creation semaphore.");
            await s_once.WaitAsync(ct).ConfigureAwait(false);

            try
            {
                if (_cachedKey is not null)
                {
                    logger?.Info(() => "[MI][InMemoryKeyProvider] Cached key created while waiting; returning it.");
                    return _cachedKey;
                }

                if (ct.IsCancellationRequested)
                {
                    logger?.Info(() => "[MI][InMemoryKeyProvider] Cancellation requested after entering critical section.");
                    ct.ThrowIfCancellationRequested();
                }

                logger?.Info(() => "[MI][InMemoryKeyProvider] Starting RSA key creation.");
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

                _cachedKey = new ManagedIdentityKeyInfo(rsa, ManagedIdentityKeyType.InMemory, message);

                logger?.Info(() =>
                    $"[MI][InMemoryKeyProvider] Caching key. Success={(rsa != null)}. HasMessage={!string.IsNullOrEmpty(message)}.");

                return _cachedKey;
            }
            finally
            {
                s_once.Release();
            }
        }

        /// <summary>
        /// Creates a new RSA key pair with 2048-bit key size for cryptographic operations.
        /// Uses platform-specific RSA implementations: RSACng on .NET Framework and RSA.Create() on other platforms.
        /// </summary>
        /// <returns>
        /// An <see cref="RSA"/> instance configured with a 2048-bit key size.
        /// On .NET Framework, returns <see cref="RSACng"/>; on other platforms, returns the default RSA implementation.
        /// </returns>
        /// <remarks>
        /// This method is public instead of private because it is used in unit tests
        /// </remarks>
        public static RSA CreateRsaKeyPair()
        {
#if NET462 || NET472 || NET8_0
            // Windows-only TFMs (Framework or -windows TFMs): compile CNG path
            return CreateWindowsPersistedRsa();

#else
            // netstandard2.0 can run anywhere; pick at runtime
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return CreateWindowsPersistedRsa(); // requires CNG package in csproj
            }
            return CreatePortableRsa();

#endif
        }

        private static RSA CreatePortableRsa()
        {
            var rsa = RSA.Create();
            if (rsa.KeySize < Constants.RsaKeySize)
                rsa.KeySize = Constants.RsaKeySize;
            return rsa;
        }

        private static RSA CreateWindowsPersistedRsa()
        {
            // Persisted CNG key (non-ephemeral) so Schannel can use it for TLS client auth
            var creation = new CngKeyCreationParameters
            {
                ExportPolicy = CngExportPolicies.AllowExport,
                KeyCreationOptions = CngKeyCreationOptions.MachineKey, // try machine store first
                Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider
            };

            // Persist key length with the key
            creation.Parameters.Add(
                new CngProperty("Length", BitConverter.GetBytes(Constants.RsaKeySize), CngPropertyOptions.Persist));

            // Non-null name => persisted; null would be ephemeral (bad for Schannel)
            string keyName = "MSAL-MTLS-" + Guid.NewGuid().ToString("N");

            try
            {
                var key = CngKey.Create(CngAlgorithm.Rsa, keyName, creation);
                return new RSACng(key);
            }
            catch (CryptographicException)
            {
                // Some environments disallow MachineKey. Fall back to user profile.
                creation.KeyCreationOptions = CngKeyCreationOptions.None;
                var key = CngKey.Create(CngAlgorithm.Rsa, keyName, creation);
                return new RSACng(key);
            }
        }
    }
}
