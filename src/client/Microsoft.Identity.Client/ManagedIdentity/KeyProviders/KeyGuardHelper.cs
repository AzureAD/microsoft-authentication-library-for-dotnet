// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.KeyGuard
{
    /// <summary>
    /// Helper that creates or opens a Key Guard–protected RSA key.
    /// </summary>
    internal static class KeyGuardHelper
    {
        private const string ProviderName = "Microsoft Software Key Storage Provider";
        private const string KeyName = "KeyGuardRSAKey";

        // KeyGuard + per-boot flags
        private const CngKeyCreationOptions NCryptUseVirtualIsolationFlag = (CngKeyCreationOptions)0x00020000;
        private const CngKeyCreationOptions NCryptUsePerBootKeyFlag = (CngKeyCreationOptions)0x00040000;

        /// <summary>
        /// Creates a new RSA-2048 Key Guard key.
        /// </summary>
        public static CngKey CreateFresh(ILoggerAdapter logger)
        {
            var p = new CngKeyCreationParameters
            {
                Provider = new CngProvider(ProviderName),
                KeyUsage = CngKeyUsages.AllUsages,
                ExportPolicy = CngExportPolicies.None,
                KeyCreationOptions =
                      CngKeyCreationOptions.OverwriteExistingKey
                    | NCryptUseVirtualIsolationFlag
                    | NCryptUsePerBootKeyFlag
            };

            p.Parameters.Add(new CngProperty("Length",
                              BitConverter.GetBytes(2048),
                              CngPropertyOptions.None));

            try
            {
                return CngKey.Create(CngAlgorithm.Rsa, KeyName, p);
            }
            catch (CryptographicException ex)
                when (IsVbsUnavailable(ex))
            {
                logger?.Warning(
                    "[MI][KeyGuardHelper] VBS key isolation is not available; falling back to software keys. " +
                    "Ensure that Virtualization Based Security (VBS) is enabled on this machine " +
                    "(e.g. Credential Guard, Hyper-V, or Windows Defender Application Guard). " +
                    "Inner exception: " + ex.Message);
                
                return null;
            }
        }

        /// <summary>Returns <c>true</c> if the key has the Key Guard flag.</summary>
        public static bool IsKeyGuardProtected(CngKey key)
        {
            if (!key.HasProperty("Virtual Iso", CngPropertyOptions.None))
                return false;

            byte[] val = key.GetProperty("Virtual Iso", CngPropertyOptions.None).GetValue();
            return val?.Length > 0 && val[0] != 0;
        }

        private static bool IsVbsUnavailable(CryptographicException ex)
        {
            // HResult for “NTE_NOT_SUPPORTED” = 0x80890014
            const int NTE_NOT_SUPPORTED = unchecked((int)0x80890014);

            return ex.HResult == NTE_NOT_SUPPORTED ||
                   ex.Message.Contains("VBS key isolation is not available");
        }
    }
}
