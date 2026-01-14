// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.ManagedIdentity.KeyProviders
{
    /// <summary>
    /// Provides CNG-backed cryptographic key operations for Windows platforms, supporting both 
    /// CredentialGuard-protected keys (with VBS/TPM integration) and hardware-backed TPM/KSP keys
    /// for managed identity authentication scenarios.
    /// </summary>
    /// <remarks>
    /// This class handles two primary key protection mechanisms:
    /// <list type="bullet">
    /// <item><description>CredentialGuard: Requires Virtualization Based Security (VBS) and provides enhanced key protection</description></item>
    /// <item><description>Hardware TPM/KSP: Uses Platform Crypto Provider (PCP) for TPM-backed keys</description></item>
    /// </list>
    /// All operations are performed in user scope with silent key access patterns.
    /// </remarks>
    internal static class WindowsCngKeyOperations
    {
        private const string SoftwareKspName = "Microsoft Software Key Storage Provider";
        private const string KeyGuardKeyName = "KeyGuardRSAKey";
        private const string HardwareKeyName = "HardwareRSAKey";
        private const string KeyGuardVirtualIsoProperty = "Virtual Iso";
        private const string VbsNotAvailable = "VBS key isolation is not available";

        // KeyGuard + per-boot flags
        private const CngKeyCreationOptions NCryptUseVirtualIsolationFlag = (CngKeyCreationOptions)0x00020000;
        private const CngKeyCreationOptions NCryptUsePerBootKeyFlag = (CngKeyCreationOptions)0x00040000;

        /// <summary>
        /// Attempts to get or create a CredentialGuard-protected RSA key for managed identity operations.
        /// This method first tries to open an existing key, and if not found, creates a fresh CredentialGuard-protected key.
        /// CredentialGuard requires VBS (Virtualization Based Security) to be enabled and supported.
        /// </summary>
        /// <param name="logger">Logger adapter for diagnostic messages and error reporting</param>
        /// <param name="rsa">When this method returns <see langword="true"/>, contains the RSA instance with the CredentialGuard-protected key; 
        /// when this method returns <see langword="false"/>, this parameter is set to <see langword="null"/></param>
        /// <returns><see langword="true"/> if a CredentialGuard-protected RSA key was successfully obtained or created; 
        /// <see langword="false"/> if CredentialGuard is unavailable, VBS is not supported, or the operation failed</returns>
        /// <remarks>
        /// <para>This method performs the following operations in sequence:</para>
        /// <list type="number">
        /// <item><description>Attempts to open an existing CredentialGuard key using the software KSP in user scope</description></item>
        /// <item><description>If the key doesn't exist, creates a new CredentialGuard-protected key</description></item>
        /// <item><description>Validates that the key is actually CredentialGuard-protected</description></item>
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
                        KeyGuardKeyName,
                        new CngProvider(SoftwareKspName),
                        CngKeyOpenOptions.UserKey | CngKeyOpenOptions.Silent);
                }
                catch (CryptographicException)
                {
                    // Not found -> create fresh (helper may return null if VBS unavailable)
                    logger?.Info(() => "[MI][WinKeyProvider] CredentialGuard key not found; creating fresh.");
                    key = CreateFresh(logger);
                }

                // If VBS is unavailable, CreateFresh() returns null. Bail out cleanly.
                if (key == null)
                {
                    logger?.Info(() => "[MI][WinKeyProvider] CredentialGuard unavailable (VBS off or not supported).");
                    return false;
                }

                // Ensure actually CredentialGuard-protected; recreate if not
                if (!IsKeyGuardProtected(key))
                {
                    logger?.Info(() => "[MI][WinKeyProvider] KeyGuard key found but not protected; recreating.");
                    key.Dispose();
                    key = CreateFresh(logger);

                    // Check again after recreate; still null or not protected -> give up KeyGuard path
                    if (key == null || !IsKeyGuardProtected(key))
                    {
                        key?.Dispose();
                        logger?.Info(() => "[MI][WinKeyProvider] Unable to obtain a KeyGuard-protected key.");
                        return false;
                    }
                }

                rsa = new RSACng(key);
                if (rsa.KeySize < Constants.RsaKeySize)
                {
                    try
                    { rsa.KeySize = Constants.RsaKeySize; }
                    catch { logger?.Info(() => $"[MI][WinKeyProvider] Unable to extend the size of the KeyGuard key to {Constants.RsaKeySize} bits."); }
                }
                return true;
            }
            catch (PlatformNotSupportedException)
            {
                // VBS/Core Isolation not available => KeyGuard unavailable
                logger?.Info(() => "[MI][WinKeyProvider] Exception creating KeyGuard key.");
                return false;
            }
            catch (CryptographicException ex)
            {
                logger?.Info(() => $"[MI][WinKeyProvider] KeyGuard creation failed due to platform limitation. {ex.GetType().Name}: {ex.Message}");
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
                CngProvider provider = new CngProvider(SoftwareKspName);
                CngKeyOpenOptions openOpts = CngKeyOpenOptions.UserKey | CngKeyOpenOptions.Silent;

                CngKey key = CngKey.Exists(HardwareKeyName, provider, openOpts)
                    ? CngKey.Open(HardwareKeyName, provider, openOpts)
                    : CreateUserPcpRsa(provider, HardwareKeyName);

                rsa = new RSACng(key);

                if (rsa.KeySize < Constants.RsaKeySize)
                {
                    try
                    { rsa.KeySize = Constants.RsaKeySize; }
                    catch { logger?.Info(() => $"[MI][WinKeyProvider] Unable to extend the size of the Hardware key to {Constants.RsaKeySize} bits."); }
                }

                logger?.Info("[MI][WinKeyProvider] Using Hardware key (RSA, PCP user).");
                return true;
            }
            catch (CryptographicException e)
            {
                // Add HResult to make CI diagnostics actionable
                logger?.Info(() => "[MI][WinKeyProvider] Hardware key creation/open failed. " +
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

            ckcParams.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(Constants.RsaKeySize), CngPropertyOptions.None));

            return CngKey.Create(CngAlgorithm.Rsa, name, ckcParams);
        }

        /// <summary>
        /// Creates a new RSA-2048 Key Guard key.
        /// </summary>
        /// <param name="logger">Logger adapter for recording diagnostic information and warnings.</param>
        /// <returns>
        /// A <see cref="CngKey"/> instance protected by Key Guard if VBS is available; 
        /// otherwise, <c>null</c> if VBS is not supported on the system.
        /// </returns>
        /// <remarks>
        /// This method attempts to create a cryptographic key with hardware-backed security using 
        /// Virtualization Based Security (VBS). If VBS is not available, the method logs a warning 
        /// and returns null, allowing the caller to fall back to software-based key storage.
        /// </remarks>
        private static CngKey CreateFresh(ILoggerAdapter logger)
        {
            var ckcParams = new CngKeyCreationParameters
            {
                Provider = new CngProvider(SoftwareKspName),
                KeyUsage = CngKeyUsages.AllUsages,
                ExportPolicy = CngExportPolicies.None,
                KeyCreationOptions =
                      CngKeyCreationOptions.OverwriteExistingKey
                    | NCryptUseVirtualIsolationFlag
                    | NCryptUsePerBootKeyFlag
            };

            ckcParams.Parameters.Add(new CngProperty("Length",
                              BitConverter.GetBytes(Constants.RsaKeySize),
                              CngPropertyOptions.None));

            try
            {
                return CngKey.Create(CngAlgorithm.Rsa, KeyGuardKeyName, ckcParams);
            }
            catch (CryptographicException ex)
                when (IsVbsUnavailable(ex))
            {
                logger?.Warning(
                    $"[MI][KeyGuardHelper] {VbsNotAvailable}; falling back to software keys. " +
                    "Ensure that Virtualization Based Security (VBS) is enabled on this machine " +
                    "(e.g. Credential Guard, Hyper-V, or Windows Defender Application Guard). " +
                    "Inner exception: " + ex.Message);

                return null;
            }
        }

        /// <summary>
        /// Determines whether the specified CNG key is protected by Key Guard.
        /// </summary>
        /// <param name="key">The CNG key to check for Key Guard protection.</param>
        /// <returns><c>true</c> if the key has the Key Guard flag; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method checks for the presence of the Virtual Iso property on the key,
        /// which indicates that the key is protected by hardware-backed security features.
        /// </remarks>
        public static bool IsKeyGuardProtected(CngKey key)
        {
            if (!key.HasProperty(KeyGuardVirtualIsoProperty, CngPropertyOptions.None))
                return false;

            byte[] val = key.GetProperty(KeyGuardVirtualIsoProperty, CngPropertyOptions.None).GetValue();
            return val?.Length > 0 && val[0] != 0;
        }

        /// <summary>
        /// Determines whether a cryptographic exception indicates that VBS is unavailable.
        /// </summary>
        /// <param name="ex">The cryptographic exception to examine.</param>
        /// <returns><c>true</c> if the exception indicates VBS is not supported; otherwise, <c>false</c>.</returns>
        private static bool IsVbsUnavailable(CryptographicException ex)
        {
            // HResult for “NTE_NOT_SUPPORTED” = 0x80890014
            const int NTE_NOT_SUPPORTED = unchecked((int)0x80890014);

            return ex.HResult == NTE_NOT_SUPPORTED ||
                   ex.Message.Contains(VbsNotAvailable);
        }
    }
}
