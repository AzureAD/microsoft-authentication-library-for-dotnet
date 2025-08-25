// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        private static readonly SemaphoreSlim s_once = new SemaphoreSlim(1, 1);
        private volatile ManagedIdentityKeyInfo _cached;

        public async Task<ManagedIdentityKeyInfo> GetOrCreateKeyAsync(CancellationToken ct)
        {
            if (_cached != null)
                return _cached;

            await s_once.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_cached != null)
                    return _cached;

                // Respect cancellation after entering critical section (optional).
                ct.ThrowIfCancellationRequested();

                var messageBuilder = new StringBuilder();

#if SUPPORTS_CNG
                // 1) KeyGuard (RSA-2048 under VBS isolation)
                try
                {
                    if (WindowsCngKeyOperations.TryGetOrCreateKeyGuard(out RSA kgRsa))
                    {
                        messageBuilder.AppendLine("KeyGuard RSA key created successfully. ");
                        _cached = new ManagedIdentityKeyInfo(kgRsa, ManagedIdentityKeyType.KeyGuard, messageBuilder.ToString());
                        return _cached;
                    }
                    else
                    {
                        messageBuilder.AppendLine("KeyGuard RSA key creation not available or failed. ");
                    }
                }
                catch (Exception ex)
                {
                    messageBuilder.AppendLine($"KeyGuard RSA key creation threw exception: {ex.Message} ");
                }

                // 2) Hardware TPM/KSP (RSA-2048, non-exportable)
                try
                {
                    if (WindowsCngKeyOperations.TryGetOrCreateHardwareRsa(out RSA hwRsa))
                    {
                        messageBuilder.AppendLine("Hardware RSA key created successfully. ");
                        _cached = new ManagedIdentityKeyInfo(hwRsa, ManagedIdentityKeyType.Hardware, messageBuilder.ToString());
                        return _cached;
                    }
                    else
                    {
                        messageBuilder.AppendLine("Hardware RSA key creation not available or failed.");
                    }
                }
                catch (Exception ex)
                {
                    messageBuilder.AppendLine($"Hardware RSA key creation threw exception: {ex.Message} ");
                }
#endif
                // 3) Fallback to portable in-memory provider
                messageBuilder.AppendLine("Falling back to in-memory RSA key provider. ");
                var memProvider = new InMemoryManagedIdentityKeyProvider();
                ManagedIdentityKeyInfo memKeyInfo = await memProvider.GetOrCreateKeyAsync(ct).ConfigureAwait(false);
                _cached = new ManagedIdentityKeyInfo(memKeyInfo.Key, ManagedIdentityKeyType.InMemory, messageBuilder.ToString());
                return _cached;
            }
            finally
            {
                s_once.Release();
            }
        }
    }
}

