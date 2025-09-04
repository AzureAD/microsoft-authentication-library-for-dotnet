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
        private const string SoftwareKspName = "Microsoft Software Key Storage Provider";
        private const string KeyGuardKeyName = "KeyGuardRSAKey";
        private const string HardwareKeyName = "HardwareRSAKey";

        // --- KeyGuard path (RSA) ---
        public static bool TryGetOrCreateKeyGuard(ILoggerAdapter logger, out RSA rsa)
        {
            rsa = default(RSA);

            try
            {
                // Try open by the known name first (Software KSP, user scope, silent)
                CngKey key;
                try
                {
                    key = CngKey.Open(
                        KeyGuardKeyName,
                        new CngProvider(SoftwareKspName),
                        CngKeyOpenOptions.UserKey | CngKeyOpenOptions.Silent);
                }
                catch (CryptographicException)
                {
                    // Not found -> create fresh (helper may return null if VBS unavailable)
                    logger?.Verbose(() => "[MI][WinKeyProvider] KeyGuard key not found; creating fresh.");
                    key = KeyGuardHelper.CreateFresh(logger);
                }

                // If VBS is unavailable, CreateFresh() returns null. Bail out cleanly.
                if (key == null)
                {
                    logger?.Verbose(() => "[MI][WinKeyProvider] KeyGuard unavailable (VBS off or not supported).");
                    return false;
                }

                // Ensure actually KeyGuard-protected; recreate if not
                if (!KeyGuardHelper.IsKeyGuardProtected(key))
                {
                    logger?.Verbose(() => "[MI][WinKeyProvider] KeyGuard key found but not protected; recreating.");
                    key.Dispose();
                    key = KeyGuardHelper.CreateFresh(logger);

                    // Check again after recreate; still null or not protected -> give up KeyGuard path
                    if (key == null || !KeyGuardHelper.IsKeyGuardProtected(key))
                    {
                        key?.Dispose();
                        logger?.Verbose(() => "[MI][WinKeyProvider] Unable to obtain a KeyGuard-protected key.");
                        return false;
                    }
                }

                rsa = new RSACng(key);
                if (rsa.KeySize < 2048)
                {
                    try
                    { rsa.KeySize = 2048; }
                    catch { /* some providers don't allow */ }
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
                // PCP (TPM) in USER scope
                CngProvider provider = new CngProvider(SoftwareKspName);
                CngKeyOpenOptions openOpts = CngKeyOpenOptions.UserKey | CngKeyOpenOptions.Silent;

                CngKey key = CngKey.Exists(HardwareKeyName, provider, openOpts)
                    ? CngKey.Open(HardwareKeyName, provider, openOpts)
                    : CreateUserPcpRsa(provider, HardwareKeyName);

                rsa = new RSACng(key);

                if (rsa.KeySize < 2048)
                {
                    try
                    { rsa.KeySize = 2048; }
                    catch { /* PCP typically ignores post-create change */ }
                }

                logger?.Info("[MI][WinKeyProvider] Using Hardware key (RSA, PCP user).");
                return true;
            }
            catch (CryptographicException e)
            {
                // Add HResult to make CI diagnostics actionable
                logger?.Verbose(() => "[MI][WinKeyProvider] Hardware key creation/open failed. " +
                                       $"HR=0x{e.HResult:X8}. {e.GetType().Name}: {e.Message}");
                return false;
            }

            static CngKey CreateUserPcpRsa(CngProvider provider, string name)
            {
                var p = new CngKeyCreationParameters
                {
                    Provider = provider,
                    KeyUsage = CngKeyUsages.Signing,
                    ExportPolicy = CngExportPolicies.None,          // non-exportable (expected for TPM)
                    KeyCreationOptions = CngKeyCreationOptions.None // USER scope
                };

                p.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(2048), CngPropertyOptions.None));

                return CngKey.Create(CngAlgorithm.Rsa, name, p);
            }
        }
    }
}
