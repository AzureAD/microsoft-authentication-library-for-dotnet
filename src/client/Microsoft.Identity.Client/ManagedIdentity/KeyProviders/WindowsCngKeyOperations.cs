// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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

        // Issuer used by IMDSv2 mTLS PoP binding certificates. Matched as a case-insensitive
        // substring against the certificate's Issuer DN, so any cert in CurrentUser\My issued
        // by IMDSv2 can be wiped when we mint a fresh KeyGuard key (the previously persisted
        // certs are bound to the now-replaced key by name and would fail the mTLS handshake).
        internal const string ManagedIdentityIssuerCnFragment = "managedidentitysnissuer.login.microsoft.com";

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
                    logger?.Info(() => $"[MI][WinKeyProvider] Attempting to open existing KeyGuard key. " +
                                       $"Provider='{SoftwareKspName}', KeyName='{KeyGuardKeyName}', Scope=UserKey, Silent=true.");

                    key = CngKey.Open(
                        KeyGuardKeyName,
                        new CngProvider(SoftwareKspName),
                        CngKeyOpenOptions.UserKey | CngKeyOpenOptions.Silent);

                    logger?.Info(() => $"[MI][WinKeyProvider] CngKey.Open succeeded for '{KeyGuardKeyName}'. " +
                                       "Running liveness sign probe to detect stale per-boot key material " +
                                       "(metadata file can survive a reboot while the VBS-isolated key material is destroyed).");

                    // Liveness probe: per-boot KeyGuard keys (NCryptUsePerBootKeyFlag) leave a stale
                    // metadata file on disk after reboot. CngKey.Open returns a handle, but the actual
                    // VBS-protected key material is gone, so the first real sign operation fails.
                    // Detect this here so we can recreate cleanly instead of failing later in the
                    // mTLS handshake or signing path.
                    if (!CanSign(key, logger))
                    {
                        logger?.Info(() => "[MI][WinKeyProvider] KeyGuard liveness sign probe FAILED. " +
                                           "Treating handle as stale (likely post-reboot per-boot key reaped). " +
                                           "Disposing stale handle and recreating fresh KeyGuard key.");
                        key.Dispose();
                        key = CreateFresh(logger);

                        if (key == null)
                        {
                            logger?.Info(() => "[MI][WinKeyProvider] CreateFresh returned null after failed liveness probe " +
                                               "(VBS unavailable). KeyGuard path will be skipped.");
                        }
                        else
                        {
                            logger?.Info(() => "[MI][WinKeyProvider] Fresh KeyGuard key created successfully after stale handle replacement. " +
                                               "Purging persisted IMDSv2 mTLS binding certificates that were bound to the replaced key.");

                            // The new KeyGuard key reuses the container name 'KeyGuardRSAKey', but its
                            // public/private pair is different from the one any persisted cert was issued
                            // against. Wipe all certs in CurrentUser\My issued by IMDSv2 so the next request
                            // mints fresh instead of failing the mTLS handshake.
                            PurgeManagedIdentityCertificates(logger);
                        }
                    }
                    else
                    {
                        logger?.Info(() => "[MI][WinKeyProvider] KeyGuard liveness sign probe PASSED. Reusing existing handle.");
                    }
                }
                catch (CryptographicException openEx)
                {
                    // Not found -> create fresh (helper may return null if VBS unavailable)
                    logger?.Info(() => $"[MI][WinKeyProvider] CngKey.Open threw CryptographicException for '{KeyGuardKeyName}'. " +
                                       $"HR=0x{openEx.HResult:X8}, Message='{openEx.Message}'. " +
                                       "Treating as 'key not found' and creating fresh.");
                    key = CreateFresh(logger);

                    if (key == null)
                    {
                        logger?.Info(() => "[MI][WinKeyProvider] CreateFresh returned null after Open failure (VBS unavailable).");
                    }
                    else
                    {
                        logger?.Info(() => "[MI][WinKeyProvider] Fresh KeyGuard key created successfully after Open failure. " +
                                           "Purging persisted IMDSv2 mTLS binding certificates that were bound to the replaced key.");

                        // Same rationale as the probe-failed branch: any persisted IMDSv2 cert in
                        // CurrentUser\My is bound to the previous KeyGuard key and will fail the mTLS
                        // handshake. Wipe them so the next request mints fresh.
                        PurgeManagedIdentityCertificates(logger);
                    }
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
        /// Performs a small RSA sign operation against the supplied CNG key to verify the
        /// underlying key material is actually usable.
        /// </summary>
        /// <param name="key">The CNG key handle returned from <see cref="CngKey.Open(string, CngProvider, CngKeyOpenOptions)"/>.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <returns>
        /// <see langword="true"/> if the key signs successfully; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// KeyGuard keys created with <c>NCryptUsePerBootKeyFlag</c> have their VBS-isolated
        /// key material destroyed on every reboot, but the on-disk metadata file produced by the
        /// Microsoft Software KSP often survives. As a result, <see cref="CngKey.Open(string, CngProvider, CngKeyOpenOptions)"/>
        /// can return a handle that looks valid (correct algorithm, "Virtual Iso" property still set)
        /// but whose first real cryptographic operation throws.
        /// </para>
        /// <para>
        /// Probing with a one-byte sign here surfaces that condition cheaply (~1-3 ms for RSA-2048)
        /// on the cold-start path. Subsequent calls reuse the cached key in
        /// <c>WindowsManagedIdentityKeyProvider</c>, so the probe runs at most once per process.
        /// </para>
        /// </remarks>
        private static bool CanSign(CngKey key, ILoggerAdapter logger)
        {
            try
            {
                logger?.Verbose(() => "[MI][WinKeyProvider] Liveness probe: attempting RSA-SHA256 sign of 1-byte payload.");

                using (var rsa = new RSACng(key))
                {
                    _ = rsa.SignData(
                        new byte[] { 0 },
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);
                }

                logger?.Verbose(() => "[MI][WinKeyProvider] Liveness probe: sign succeeded; key material is live.");
                return true;
            }
            catch (CryptographicException ex)
            {
                logger?.Info(() => $"[MI][WinKeyProvider] Liveness probe: sign threw CryptographicException. " +
                                   $"HR=0x{ex.HResult:X8}, Message='{ex.Message}'. Key handle is stale.");
                return false;
            }
            catch (Exception ex)
            {
                logger?.Info(() => $"[MI][WinKeyProvider] Liveness probe: sign threw unexpected exception. " +
                                   $"{ex.GetType().Name}: '{ex.Message}'. Treating as stale.");
                return false;
            }
        }

        /// <summary>
        /// Deletes every certificate in the <c>CurrentUser\My</c> store whose issuer matches the
        /// IMDSv2 mTLS PoP binding-certificate issuer.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <remarks>
        /// <para>
        /// IMDSv2 binding certificates are issued by
        /// <c>CN=managedidentitysnissuer.login.microsoft.com</c> and stored in the user's personal
        /// store. They reference the private key by KSP container name (<c>KeyGuardRSAKey</c>),
        /// not by key material. When the KeyGuard key is re-minted (post-reboot, or after a failed
        /// liveness probe), the new key reuses the same container name but with different
        /// public/private parameters — leaving the persisted certs bound to a key that no longer
        /// matches them, which then fails the mTLS handshake.
        /// </para>
        /// <para>
        /// Purging the store at the moment we mint a fresh KeyGuard key eliminates the
        /// failed-handshake + retry round trip that the SChannel-error catch in
        /// <c>ImdsV2ManagedIdentitySource.AuthenticateAsync</c> would otherwise have to recover from.
        /// </para>
        /// <para>
        /// All store I/O is best-effort and non-throwing.
        /// </para>
        /// </remarks>
        internal static void PurgeManagedIdentityCertificates(ILoggerAdapter logger)
        {
            int removed = 0;
            int inspected = 0;

            try
            {
                logger?.Info(() =>
                    $"[MI][WinKeyProvider] PurgeManagedIdentityCertificates: opening CurrentUser\\My to remove " +
                    $"certs whose Issuer contains '{ManagedIdentityIssuerCnFragment}'.");

                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadWrite);

                    // Snapshot to avoid 'collection modified during enumeration' provider quirks.
                    var snapshot = new X509Certificate2[store.Certificates.Count];
                    try
                    {
                        store.Certificates.CopyTo(snapshot, 0);
                    }
                    catch (Exception copyEx)
                    {
                        logger?.Info(() =>
                            $"[MI][WinKeyProvider] PurgeManagedIdentityCertificates: store snapshot via CopyTo failed " +
                            $"({copyEx.GetType().Name}: {copyEx.Message}). Falling back to enumeration.");

                        int i = 0;
                        snapshot = new X509Certificate2[store.Certificates.Count];
                        foreach (X509Certificate2 c in store.Certificates)
                        {
                            snapshot[i++] = c;
                        }
                    }

                    foreach (X509Certificate2 candidate in snapshot)
                    {
                        if (candidate is null)
                        {
                            // Defensive: snapshot slot may be null if the store enumeration
                            // yielded fewer items than Certificates.Count reported (TOCTOU).
                            continue;
                        }

                        try
                        {
                            inspected++;

                            string issuer = candidate.Issuer ?? string.Empty;
                            if (issuer.IndexOf(ManagedIdentityIssuerCnFragment, StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                continue;
                            }

                            string thumb = candidate.Thumbprint;
                            DateTime notAfter = candidate.NotAfter;

                            try
                            {
                                store.Remove(candidate);
                                removed++;
                                logger?.Info(() =>
                                    $"[MI][WinKeyProvider] PurgeManagedIdentityCertificates: removed cert. " +
                                    $"Thumbprint={thumb}, NotAfter={notAfter:O}, Issuer='{issuer}'.");
                            }
                            catch (Exception removeEx)
                            {
                                logger?.Info(() =>
                                    $"[MI][WinKeyProvider] PurgeManagedIdentityCertificates: failed to remove cert " +
                                    $"Thumbprint={thumb}. {removeEx.GetType().Name}: '{removeEx.Message}'.");
                            }
                        }
                        finally
                        {
                            candidate.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.Info(() =>
                    $"[MI][WinKeyProvider] PurgeManagedIdentityCertificates: store access failed. " +
                    $"{ex.GetType().Name}: '{ex.Message}'. Removed={removed}, Inspected={inspected}.");
                return;
            }

            int removedFinal = removed;
            int inspectedFinal = inspected;
            logger?.Info(() =>
                $"[MI][WinKeyProvider] PurgeManagedIdentityCertificates: complete. " +
                $"Removed={removedFinal}, Inspected={inspectedFinal}.");
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
