// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.ManagedIdentity.KeyGuard
{
    /// <summary>
    /// Helper class for creating and managing Key Guard protected cryptographic keys.
    /// Provides functionality for creating RSA keys with Virtualization Based Security (VBS) isolation.
    /// </summary>
    internal static class KeyGuardHelper
    {
        private const string KeyGuardVirtualIsoProperty = "Virtual Iso";
        private const string VbsNotAvailable = "VBS key isolation is not available";

        // KeyGuard + per-boot flags
        private const CngKeyCreationOptions NCryptUseVirtualIsolationFlag = (CngKeyCreationOptions)0x00020000;
        private const CngKeyCreationOptions NCryptUsePerBootKeyFlag = (CngKeyCreationOptions)0x00040000;

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
        public static CngKey CreateFresh(ILoggerAdapter logger)
        {
            var ckcParams = new CngKeyCreationParameters
            {
                Provider = new CngProvider(Constants.SoftwareKspName),
                KeyUsage = CngKeyUsages.AllUsages,
                ExportPolicy = CngExportPolicies.None,
                KeyCreationOptions =
                      CngKeyCreationOptions.OverwriteExistingKey
                    | NCryptUseVirtualIsolationFlag
                    | NCryptUsePerBootKeyFlag
            };

            ckcParams.Parameters.Add(new CngProperty("Length",
                              BitConverter.GetBytes(Constants.KeySize2048),
                              CngPropertyOptions.None));

            try
            {
                return CngKey.Create(CngAlgorithm.Rsa, Constants.KeyGuardKeyName, ckcParams);
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
