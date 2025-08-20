// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !NETSTANDARD2_0
using System;
using System.Security.Cryptography;

namespace Microsoft.Identity.Client.ManagedIdentity.KeyGuard
{
    /// <summary>
    /// Helper for creating and validating Key Guard–isolated RSA keys.
    /// </summary>
    internal static class KeyGuardKey
    {
        // Flags not exposed in the public enum; values from NCrypt.h
        private const CngKeyCreationOptions NCRYPT_USE_VIRTUAL_ISOLATION_FLAG = (CngKeyCreationOptions)0x00020000;
        private const CngKeyCreationOptions NCRYPT_USE_PER_BOOT_KEY_FLAG = (CngKeyCreationOptions)0x00040000;

        /// <summary>
        /// Create a fresh RSA-2048 key with Key Guard (VBS) isolation.
        /// Overwrites any existing key with the same name.
        /// </summary>
        /// <param name="providerName">Typically "Microsoft Software Key Storage Provider".</param>
        /// <param name="keyName">The CNG key container name to create.</param>
        public static CngKey CreateFresh(string providerName, string keyName)
        {
            var parms = new CngKeyCreationParameters
            {
                Provider = new CngProvider(providerName),
                KeyUsage = CngKeyUsages.AllUsages,
                ExportPolicy = CngExportPolicies.None,
                // Per-boot, VBS/KeyGuard isolation; overwrite if exists
                KeyCreationOptions = CngKeyCreationOptions.OverwriteExistingKey
                                   | NCRYPT_USE_VIRTUAL_ISOLATION_FLAG
                                   | NCRYPT_USE_PER_BOOT_KEY_FLAG
            };

            // Set length = 2048 bits
            parms.Parameters.Add(new CngProperty(
                "Length",
                BitConverter.GetBytes(2048),
                CngPropertyOptions.None));

            try
            {
                return CngKey.Create(CngAlgorithm.Rsa, keyName, parms);
            }
            catch (CryptographicException ex)
            {
                if (IsVbsUnavailable(ex))
                {
                    // Clear, actionable signal to callers; provider can fall back to TPM/in-memory
                    throw new PlatformNotSupportedException(
                        "Key Guard requires Windows Core Isolation (VBS).", ex);
                }
                throw;
            }
        }

        /// <summary>
        /// Returns true if the key has the Key Guard (VBS) isolation flag.
        /// </summary>
        public static bool IsKeyGuardProtected(CngKey key)
        {
            if (!key.HasProperty("Virtual Iso", CngPropertyOptions.None))
                return false;

            byte[] val = key.GetProperty("Virtual Iso", CngPropertyOptions.None).GetValue();
            return val != null && val.Length > 0 && val[0] != 0;
        }

        // NTE_NOT_SUPPORTED or recognizable message => VBS isolation not available
        private static bool IsVbsUnavailable(CryptographicException ex)
        {
            const int NTE_NOT_SUPPORTED = unchecked((int)0x80890014);
            return ex.HResult == NTE_NOT_SUPPORTED
                || (ex.Message != null && ex.Message.IndexOf("VBS key isolation", StringComparison.OrdinalIgnoreCase) >= 0)
                || (ex.Message != null && ex.Message.IndexOf("Virtualization-based", StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
#endif
