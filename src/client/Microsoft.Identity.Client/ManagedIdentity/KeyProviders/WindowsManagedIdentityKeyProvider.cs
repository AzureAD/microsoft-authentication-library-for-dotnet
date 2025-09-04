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
    /// Windows policy:
    ///   1) KeyGuard (CVM/TVM) if available
    ///   2) Hardware (TPM/KSP via Microsoft Platform Crypto Provider)
    ///   3) In-memory fallback (delegates to InMemoryManagedIdentityKeyProvider)
    /// </summary>
    internal sealed class WindowsManagedIdentityKeyProvider : IManagedIdentityKeyProvider
    {
        private static readonly SemaphoreSlim s_once = new (1, 1);
        private volatile ManagedIdentityKeyInfo _cached;

        public async Task<ManagedIdentityKeyInfo> GetOrCreateKeyAsync(
            ILoggerAdapter logger,
            CancellationToken ct)
        {
            // Return cached if available
            if (_cached != null)
            {
                logger?.Info("[MI][WinKeyProvider] Returning cached key.");
                return _cached;
            }

            // Ensure only one creation at a time
            logger?.Verbose(() => "[MI][WinKeyProvider] Waiting on creation semaphore.");
            await s_once.WaitAsync(ct).ConfigureAwait(false);

            try
            {
                if (_cached != null)
                {
                    logger?.Verbose(() => "[MI][WinKeyProvider] Cached key created while waiting; returning it.");
                    return _cached;
                }

                if (ct.IsCancellationRequested)
                {
                    logger?.Verbose(() => "[MI][WinKeyProvider] Cancellation requested after entering critical section.");
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
                        _cached = new ManagedIdentityKeyInfo(kgRsa, ManagedIdentityKeyType.KeyGuard, messageBuilder.ToString());
                        logger?.Info("[MI][WinKeyProvider] Using KeyGuard key (RSA).");
                        return _cached;
                    }
                    else
                    {
                        messageBuilder.AppendLine("KeyGuard RSA key creation not available or failed.");
                        logger?.Verbose(() => "[MI][WinKeyProvider] KeyGuard key not available.");
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
                    logger?.Verbose(() => "[MI][WinKeyProvider] Trying Hardware (TPM/KSP) key.");
                    if (WindowsCngKeyOperations.TryGetOrCreateHardwareRsa(logger, out RSA hwRsa))
                    {
                        messageBuilder.AppendLine("Hardware RSA key created successfully.");
                        _cached = new ManagedIdentityKeyInfo(hwRsa, ManagedIdentityKeyType.Hardware, messageBuilder.ToString());
                        logger?.Info("[MI][WinKeyProvider] Using Hardware key (RSA).");
                        return _cached;
                    }
                    else
                    {
                        messageBuilder.AppendLine("Hardware RSA key creation not available or failed.");
                        logger?.Verbose(() => "[MI][WinKeyProvider] Hardware key not available.");
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
                    logger?.Verbose(() => "[MI][WinKeyProvider] Cancellation requested before in-memory fallback.");
                    ct.ThrowIfCancellationRequested();
                }

                var fallback = new InMemoryManagedIdentityKeyProvider();
                _cached = await fallback.GetOrCreateKeyAsync(logger, ct).ConfigureAwait(false);

                if (messageBuilder.Length > 0)
                {
                    logger?.Verbose(() => "[MI][WinKeyProvider] Fallback reasons:\n" + messageBuilder.ToString().Trim());
                }

                return _cached;

            }
            finally
            {
                s_once.Release();
            }
        }
    }
}

