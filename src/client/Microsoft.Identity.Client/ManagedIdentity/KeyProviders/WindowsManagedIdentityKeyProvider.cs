// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.KeyProviders
{
    /// <summary>
    /// Windows-specific managed identity key provider that implements a hierarchical key selection strategy.
    /// Attempts to use the most secure key source available in the following priority order:
    /// 1. KeyGuard (CVM/TVM) if available - provides VBS (Virtualization-based Security) isolation
    /// 2. Hardware (TPM/KSP via Microsoft Platform Crypto Provider) - hardware-backed keys
    /// 3. In-memory fallback - software-based keys stored in memory
    /// </summary>
    /// <remarks>
    /// This provider ensures that only one key creation operation occurs at a time using a semaphore,
    /// and caches the created key for subsequent requests to improve performance.
    /// </remarks>
    internal sealed class WindowsManagedIdentityKeyProvider : IManagedIdentityKeyProvider
    {
        private static readonly SemaphoreSlim s_once = new (1, 1);
        private volatile ManagedIdentityKeyInfo _cachedKey;

        /// <summary>
        /// Gets or creates a managed identity key using the best available security mechanism.
        /// </summary>
        /// <param name="logger">Logger adapter for recording key creation attempts and results.</param>
        /// <param name="ct">Cancellation token to cancel the operation if needed.</param>
        /// <returns>
        /// A task that represents the asynchronous key creation operation. 
        /// The task result contains <see cref="ManagedIdentityKeyInfo"/> with the created key and its type.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is cancelled via the <paramref name="ct"/> parameter.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This method implements a thread-safe, single-creation pattern using a semaphore.
        /// If a key has already been created and cached, it returns immediately.
        /// </para>
        /// <para>
        /// The key creation follows this priority order:
        /// <list type="number">
        /// <item><description>KeyGuard: Uses VBS isolation for maximum security (RSA-2048)</description></item>
        /// <item><description>Hardware: Uses TPM or hardware security module (RSA-2048, non-exportable)</description></item>
        /// <item><description>In-memory: Software fallback when hardware options are unavailable</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Exceptions during key creation are logged but do not prevent fallback to the next option.
        /// Only the final in-memory fallback can throw exceptions that terminate the operation.
        /// </para>
        /// </remarks>
        public async Task<ManagedIdentityKeyInfo> GetOrCreateKeyAsync(
            ILoggerAdapter logger,
            CancellationToken ct)
        {
            // Return cached if available
            if (_cachedKey != null)
            {
                logger?.Info("[MI][WinKeyProvider] Returning cached key.");
                return _cachedKey;
            }

            // Ensure only one creation at a time
            logger?.Info(() => "[MI][WinKeyProvider] Waiting on creation semaphore.");
            await s_once.WaitAsync(ct).ConfigureAwait(false);

            try
            {
                if (_cachedKey != null)
                {
                    logger?.Info(() => "[MI][WinKeyProvider] Cached key created while waiting; returning it.");
                    return _cachedKey;
                }

                if (ct.IsCancellationRequested)
                {
                    logger?.Info(() => "[MI][WinKeyProvider] Cancellation requested after entering critical section.");
                    ct.ThrowIfCancellationRequested();
                }

                var messageBuilder = new StringBuilder();

                // 1) KeyGuard (RSA-2048 under VBS isolation)
                try
                {
                    logger.Info("[MI][WinKeyProvider] Trying KeyGuard key.");
                    if (WindowsCngKeyOperations.TryGetOrCreateKeyGuard(logger, out RSA kgRsa))
                    {
                        messageBuilder.AppendLine("KeyGuard RSA key created successfully.");
                        _cachedKey = new ManagedIdentityKeyInfo(kgRsa, ManagedIdentityKeyType.KeyGuard, messageBuilder.ToString());
                        logger?.Info("[MI][WinKeyProvider] Using KeyGuard key (RSA).");
                        return _cachedKey;
                    }
                    else
                    {
                        messageBuilder.AppendLine("KeyGuard RSA key creation not available or failed.");
                        logger?.Info(() => "[MI][WinKeyProvider] KeyGuard key not available.");
                    }
                }
                catch (Exception ex)
                {
                    messageBuilder.AppendLine($"KeyGuard RSA key creation threw exception: {ex.GetType().Name}: {ex.Message}");
                    logger?.WarningPii(
                        $"[MI][WinKeyProvider] Exception creating KeyGuard key: {ex}",
                        $"[MI][WinKeyProvider] Exception creating KeyGuard key: {ex.GetType().Name}");
                }

                // 2) Hardware TPM/KSP (RSA-2048, non-exportable)
                try
                {
                    logger?.Info(() => "[MI][WinKeyProvider] Trying Hardware (TPM/KSP) key.");
                    if (WindowsCngKeyOperations.TryGetOrCreateHardwareRsa(logger, out RSA hwRsa))
                    {
                        messageBuilder.AppendLine("Hardware RSA key created successfully.");
                        _cachedKey = new ManagedIdentityKeyInfo(hwRsa, ManagedIdentityKeyType.Hardware, messageBuilder.ToString());
                        logger?.Info("[MI][WinKeyProvider] Using Hardware key (RSA).");
                        return _cachedKey;
                    }
                    else
                    {
                        messageBuilder.AppendLine("Hardware RSA key creation not available or failed.");
                        logger?.Info(() => "[MI][WinKeyProvider] Hardware key not available.");
                    }
                }
                catch (Exception ex)
                {
                    messageBuilder.AppendLine($"Hardware RSA key creation threw exception: {ex.GetType().Name}: {ex.Message}");
                    logger?.WarningPii(
                        $"[MI][WinKeyProvider] Exception creating Hardware key: {ex}",
                        $"[MI][WinKeyProvider] Exception creating Hardware key: {ex.GetType().Name}");
                }

                // 3) In-memory fallback (software RSA)
                logger?.Info("[MI][WinKeyProvider] Falling back to in-memory RSA key (software).");
                if (ct.IsCancellationRequested)
                {
                    logger?.Info(() => "[MI][WinKeyProvider] Cancellation requested before in-memory fallback.");
                    ct.ThrowIfCancellationRequested();
                }

                var fallbackIMMIKP = new InMemoryManagedIdentityKeyProvider();
                _cachedKey = await fallbackIMMIKP.GetOrCreateKeyAsync(logger, ct).ConfigureAwait(false);

                if (messageBuilder.Length > 0)
                {
                    logger?.Info(() => "[MI][WinKeyProvider] Fallback reasons:\n" + messageBuilder.ToString().Trim());
                }

                return _cachedKey;

            }
            finally
            {
                s_once.Release();
            }
        }
    }
}

