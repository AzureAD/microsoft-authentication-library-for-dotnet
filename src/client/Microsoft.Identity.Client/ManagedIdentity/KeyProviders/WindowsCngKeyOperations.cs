// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity.KeyGuard;

namespace Microsoft.Identity.Client.ManagedIdentity.KeyProviders
{
    /// <summary>
    /// CNG-backed key operations for Windows (KeyGuard + TPM/KSP).
    /// </summary>
    internal static class WindowsCngKeyOperations
    {
        private const string ProviderName = "Microsoft Software Key Storage Provider";
        private const string KeyName = "KeyGuardRSAKey";

        // --- KeyGuard path (RSA) ---
        public static bool TryGetOrCreateKeyGuard(ILoggerAdapter logger, out RSA rsa)
        {
            rsa = default(RSA);

            try
            {
                // Try open by the known name first
                CngKey key;
                try
                {
                    key = CngKey.Open(KeyName, new CngProvider(ProviderName));
                }
                catch (CryptographicException)
                {
                    // Not found -> create fresh
                    logger?.Verbose(() => "[MI][WinKeyProvider] KeyGuard key not found; creating fresh.");
                    key = KeyGuardHelper.CreateFresh(logger);
                }

                // Ensure actually KeyGuard-protected; recreate if not
                if (!KeyGuardHelper.IsKeyGuardProtected(key))
                {
                    logger?.Verbose(() => "[MI][WinKeyProvider] KeyGuard key found but not protected; recreating.");
                    key.Dispose();
                    key = KeyGuardHelper.CreateFresh(logger);
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
                logger?.Verbose(() => "[MI][WinKeyProvider] Exception creating KeyGuard key.");
                return false;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        // --- Hardware (TPM/KSP) path (RSA) ---
        public static bool TryGetOrCreateHardwareRsa(ILoggerAdapter logger, out RSA rsa)
        {
            rsa = default(RSA);

            try
            {
                CngProvider provider = new CngProvider(ProviderName);
                CngKeyOpenOptions openOpts = CngKeyOpenOptions.UserKey;

                CngKey key = CngKey.Exists(KeyName, provider, openOpts)
                    ? CngKey.Open(KeyName, provider, openOpts)
                    : CngKey.Create(
                        CngAlgorithm.Rsa,
                        KeyName,
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
                
                logger?.Info("[MI][WinKeyProvider] Using Hardware key (RSA).");
                return true;
            }
            catch (CryptographicException)
            {
                logger?.Verbose(() => "[MI][WinKeyProvider] Exception creating Hardware key.");
                return false;
            }
        }
    }
}
