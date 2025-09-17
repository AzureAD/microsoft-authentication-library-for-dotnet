// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity.KeyGuard;

namespace Microsoft.Identity.Client.ManagedIdentity.KeyProviders
{
    /// <summary>
    /// Provides CNG-backed cryptographic key operations for Windows platforms, supporting both 
    /// KeyGuard-protected keys (with VBS/TPM integration) and hardware-backed TPM/KSP keys
    /// for managed identity authentication scenarios.
    /// </summary>
    /// <remarks>
    /// This class handles two primary key protection mechanisms:
    /// <list type="bullet">
    /// <item><description>KeyGuard: Requires Virtualization Based Security (VBS) and provides enhanced key protection</description></item>
    /// <item><description>Hardware TPM/KSP: Uses Platform Crypto Provider (PCP) for TPM-backed keys</description></item>
    /// </list>
    /// All operations are performed in user scope with silent key access patterns.
    /// </remarks>
    internal static class WindowsCngKeyOperations
    {
        private const string HardwareKeyName = "HardwareRSAKey";

        /// <summary>
        /// Attempts to get or create a KeyGuard-protected RSA key for managed identity operations.
        /// This method first tries to open an existing key, and if not found, creates a fresh KeyGuard-protected key.
        /// KeyGuard requires VBS (Virtualization Based Security) to be enabled and supported.
        /// </summary>
        /// <param name="logger">Logger adapter for diagnostic messages and error reporting</param>
        /// <param name="rsa">When this method returns <see langword="true"/>, contains the RSA instance with the KeyGuard-protected key; 
        /// when this method returns <see langword="false"/>, this parameter is set to <see langword="null"/></param>
        /// <returns><see langword="true"/> if a KeyGuard-protected RSA key was successfully obtained or created; 
        /// <see langword="false"/> if KeyGuard is unavailable, VBS is not supported, or the operation failed</returns>
        /// <remarks>
        /// <para>This method performs the following operations in sequence:</para>
        /// <list type="number">
        /// <item><description>Attempts to open an existing KeyGuard key using the software KSP in user scope</description></item>
        /// <item><description>If the key doesn't exist, creates a new KeyGuard-protected key</description></item>
        /// <item><description>Validates that the key is actually KeyGuard-protected</description></item>
        /// <item><description>If validation fails, recreates the key and re-validates</description></item>
        /// <item><description>Ensures the RSA key size is at least 2048 bits when possible</description></item>
        /// </list>
        /// <para>The method gracefully handles scenarios where VBS is disabled or not supported by returning <see langword="false"/>.</para>
        /// </remarks>
        /// <exception cref="PlatformNotSupportedException">Thrown when VBS/Core Isolation is not available on the platform</exception>
        /// <exception cref="CryptographicException">Thrown when cryptographic operations fail during key creation or access</exception>
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
                        Constants.KeyGuardKeyName,
                        new CngProvider(Constants.SoftwareKspName),
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
                if (rsa.KeySize < Constants.KeySize2048)
                {
                    try
                    { rsa.KeySize = Constants.KeySize2048; }
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

        /// <summary>
        /// Attempts to get or create a hardware-backed RSA key using the Platform Crypto Provider (PCP) 
        /// for TPM-based key storage and operations.
        /// </summary>
        /// <param name="logger">Logger adapter for diagnostic messages and error reporting</param>
        /// <param name="rsa">When this method returns <see langword="true"/>, contains the RSA instance backed by hardware (TPM); 
        /// when this method returns <see langword="false"/>, this parameter is set to <see langword="null"/></param>
        /// <returns><see langword="true"/> if a hardware-backed RSA key was successfully obtained or created; 
        /// <see langword="false"/> if hardware key operations are not available or the operation failed</returns>
        /// <remarks>
        /// <para>This method performs the following operations:</para>
        /// <list type="number">
        /// <item><description>Checks if a hardware key with the predefined name already exists in user scope</description></item>
        /// <item><description>Opens the existing key if found, or creates a new hardware-backed key if not found</description></item>
        /// <item><description>Configures the key with non-exportable policy (standard for TPM keys)</description></item>
        /// <item><description>Ensures the RSA key size is at least 2048 bits when supported by the provider</description></item>
        /// </list>
        /// <para>The created keys are stored in user scope and are non-exportable for security reasons.
        /// TPM providers typically ignore post-creation key size changes.</para>
        /// </remarks>
        /// <exception cref="CryptographicException">Thrown when hardware key creation, opening, or configuration fails.
        /// The exception's HResult property provides additional diagnostic information</exception>
        public static bool TryGetOrCreateHardwareRsa(ILoggerAdapter logger, out RSA rsa)
        {
            rsa = default(RSA);

            try
            {
                // PCP (TPM) in USER scope
                CngProvider provider = new CngProvider(Constants.SoftwareKspName);
                CngKeyOpenOptions openOpts = CngKeyOpenOptions.UserKey | CngKeyOpenOptions.Silent;

                CngKey key = CngKey.Exists(HardwareKeyName, provider, openOpts)
                    ? CngKey.Open(HardwareKeyName, provider, openOpts)
                    : CreateUserPcpRsa(provider, HardwareKeyName);

                rsa = new RSACng(key);

                if (rsa.KeySize < Constants.KeySize2048)
                {
                    try
                    { rsa.KeySize = Constants.KeySize2048; }
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
        }

        /// <summary>
        /// Creates a new RSA key using the Platform Crypto Provider (PCP) in user scope
        /// with non-exportable policy suitable for TPM-backed operations.
        /// </summary>
        /// <param name="provider">The CNG provider to use for key creation (typically PCP for TPM)</param>
        /// <param name="name">The name to assign to the created key for future reference</param>
        /// <returns>A new <see cref="CngKey"/> instance configured for signing operations with 2048-bit key size</returns>
        /// <remarks>
        /// The created key has the following characteristics:
        /// <list type="bullet">
        /// <item><description>Algorithm: RSA</description></item>
        /// <item><description>Key size: 2048 bits</description></item>
        /// <item><description>Usage: Signing operations</description></item>
        /// <item><description>Export policy: None (non-exportable)</description></item>
        /// <item><description>Scope: User scope</description></item>
        /// </list>
        /// </remarks>
        private static CngKey CreateUserPcpRsa(CngProvider provider, string name)
        {
            var ckcParams = new CngKeyCreationParameters
            {
                Provider = provider,
                KeyUsage = CngKeyUsages.Signing,
                ExportPolicy = CngExportPolicies.None,          // non-exportable (expected for TPM)
                KeyCreationOptions = CngKeyCreationOptions.None // USER scope
            };

            ckcParams.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(Constants.KeySize2048), CngPropertyOptions.None));

            return CngKey.Create(CngAlgorithm.Rsa, name, ckcParams);
        }
    }
}
