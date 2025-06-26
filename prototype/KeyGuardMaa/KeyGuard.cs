// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;

namespace KeyGuard.Security;

/// <summary>
/// Helper that creates or opens a Key Guard–protected RSA key.
/// </summary>
public static class KeyGuardKey
{
    private const string ProviderName = "Microsoft Software Key Storage Provider";
    private const string KeyName = "KeyGuardRSAKey";

    // KeyGuard + per-boot flags (not in the enum)
    private const CngKeyCreationOptions NCryptUseVirtualIsolationFlag = (CngKeyCreationOptions)0x00020000;
    private const CngKeyCreationOptions NCryptUsePerBootKeyFlag = (CngKeyCreationOptions)0x00040000;

    /// <summary>
    /// Creates a new RSA-2048 Key Guard key.
    /// Throws <see cref="PlatformNotSupportedException"/> on machines
    /// that do not have VBS / Core-Isolation enabled.
    /// </summary>
    public static CngKey CreateFresh()
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
            // wrap in a clearer exception so callers can decide how to react
            // for MSAL we can just log and fall back to software keys
            throw new PlatformNotSupportedException(
                "Key Guard requires Windows Core Isolation (VBS). " +
                "Enable it in Windows Security ► Device Security ► Core isolation.",
                ex);
        }
    }

    /// <summary>Returns <c>true</c> if the key has the Key Guard flag.</summary>
    public static bool IsKeyGuardProtected(CngKey key)
    {
        if (!key.HasProperty("Virtual Iso", CngPropertyOptions.None))
            return false;

        byte[]? val = key.GetProperty("Virtual Iso", CngPropertyOptions.None).GetValue();
        return val?.Length > 0 && val[0] != 0;
    }

    private static bool IsVbsUnavailable(CryptographicException ex)
    {
        // official HResult for “NTE_NOT_SUPPORTED” = 0x80890014
        const int NTE_NOT_SUPPORTED = unchecked((int)0x80890014);

        return ex.HResult == NTE_NOT_SUPPORTED ||
               ex.Message.Contains("VBS key isolation is not available",
                                   StringComparison.OrdinalIgnoreCase);
    }
}
