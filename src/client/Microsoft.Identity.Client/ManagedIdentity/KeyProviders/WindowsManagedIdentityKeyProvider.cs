// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// src/client/Microsoft.Identity.Client/ManagedIdentity/Providers/WindowsManagedIdentityKeyProvider.cs
#if !NETSTANDARD2_0
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ManagedIdentity.KeyGuard;

namespace Microsoft.Identity.Client.ManagedIdentity.Providers
{
    /// <summary>
    /// Windows policy:
    ///   1) KeyGuard (CVM/TVM) if available
    ///   2) Hardware (TPM/KSP via Microsoft Platform Crypto Provider)
    ///   3) In-memory fallback (delegates to InMemoryManagedIdentityKeyProvider)
    /// No certs; no attestation; no long-lived handles kept.
    /// </summary>
    internal sealed class WindowsManagedIdentityKeyProvider : IManagedIdentityKeyProvider
    {
        private static readonly SemaphoreSlim s_once = new SemaphoreSlim(1, 1);
        private volatile MiKeyInfo _cached;

        private const string KgProviderName = "Microsoft Software Key Storage Provider";
        private const string KgKeyName = "KeyGuardRSAKey";
        private const string TpmProvider = "Microsoft Platform Crypto Provider";
        private const string TpmKeyName = "MSAL_MI_PLATFORM_RSA";

        public async Task<MiKeyInfo> GetOrCreateKeyAsync(CancellationToken ct)
        {
            if (_cached != null)
                return _cached;

            await s_once.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_cached != null)
                    return _cached;

                // 1) KeyGuard (RSA-2048 under VBS isolation)
                RSA kgRsa;
                if (TryGetOrCreateKeyGuard(out kgRsa))
                {
                    _cached = new MiKeyInfo(kgRsa, MiKeyType.KeyGuard);
                    return _cached;
                }

                // 2) Hardware TPM/KSP (RSA-2048, non-exportable)
                RSA hwRsa;
                if (TryGetOrCreateHardwareRsa(out hwRsa))
                {
                    _cached = new MiKeyInfo(hwRsa, MiKeyType.Hardware);
                    return _cached;
                }

                // 3) Delegate fallback to portable in-memory provider
                var memProvider = new InMemoryManagedIdentityKeyProvider();
                _cached = await memProvider.GetOrCreateKeyAsync(ct).ConfigureAwait(false);
                return _cached;
            }
            finally
            {
                s_once.Release();
            }
        }

        // --- KeyGuard path (RSA) ---
        private static bool TryGetOrCreateKeyGuard(out RSA rsa)
        {
            rsa = default(RSA);

            try
            {
                CngProvider provider = new CngProvider(KgProviderName);

                CngKey key;
                if (CngKey.Exists(KgKeyName, provider))
                {
                    key = CngKey.Open(KgKeyName, provider);

                    // Ensure actually KeyGuard-protected; if not, recreate as KeyGuard.
                    if (!KeyGuardKey.IsKeyGuardProtected(key))
                    {
                        key.Dispose();
                        key = KeyGuardKey.CreateFresh(KgProviderName, KgKeyName);
                    }
                }
                else
                {
                    key = KeyGuardKey.CreateFresh(KgProviderName, KgKeyName);
                }

                rsa = new RSACng(key);
                if (rsa.KeySize < 2048)
                {
                    try
                    { rsa.KeySize = 2048; }
                    catch { }
                }
                return true;
            }
            catch (PlatformNotSupportedException)
            {
                // VBS/Core Isolation not available => KeyGuard unavailable
                return false;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        // --- Hardware (TPM/KSP) path (RSA) ---
        private static bool TryGetOrCreateHardwareRsa(out RSA rsa)
        {
            rsa = default(RSA);

            try
            {
                CngProvider provider = new CngProvider(TpmProvider);
                CngKeyOpenOptions openOpts = CngKeyOpenOptions.MachineKey;

                CngKey key = CngKey.Exists(TpmKeyName, provider, openOpts)
                    ? CngKey.Open(TpmKeyName, provider, openOpts)
                    : CngKey.Create(
                        CngAlgorithm.Rsa,
                        TpmKeyName,
                        new CngKeyCreationParameters
                        {
                            Provider = provider,
                            KeyUsage = CngKeyUsages.Signing,
                            ExportPolicy = CngExportPolicies.None,   // non-exportable
                            KeyCreationOptions = CngKeyCreationOptions.MachineKey
                        });

                rsa = new RSACng(key);
                if (rsa.KeySize < 2048)
                {
                    try
                    { rsa.KeySize = 2048; }
                    catch { }
                }
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }
    }
}
#endif
