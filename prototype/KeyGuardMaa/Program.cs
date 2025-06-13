// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using KeyGuard.Attestation;

class Program
{
    private const string ProviderName = "Microsoft Software Key Storage Provider";
    private const string KeyName = "KeyGuardRSAKey";
    private const CngKeyCreationOptions NCryptUseVirtualIsolationFlag = (CngKeyCreationOptions)0x00020000;
    private const CngKeyCreationOptions NCryptUsePerBootKeyFlag = (CngKeyCreationOptions)0x00040000;

    private const string MaaEndpoint = "";

    static void Main()
    {
        /* Create a fresh KeyGuard-protected key */
        using CngKey key = CreateFreshKey();

        /* Attest it through the managed wrapper */
        using var client = new AttestationClient();          // auto-initializes native DLL
        var result = client.Attest(MaaEndpoint, key.Handle);

        switch (result.Status)
        {
            case AttestationStatus.Success:
                Console.WriteLine($"\nAttestation JWT:\n{result.Jwt}");
                break;

            case AttestationStatus.NativeError:
                var rc = (AttestationResultErrorCode)result.NativeCode;
                Console.WriteLine(
                    $"❌ Native error {rc} (0x{result.NativeCode:X}):\n" +
                    $"{AttestationErrors.Describe(rc)}");
                break;

            default:
                Console.WriteLine($"❌ {result.Status}: {result.Message}");
                break;
        }

        /* Quick signature sanity-check */ // this is not part of the attestation flow
        using RSA rsa = new RSACng(key);
        byte[] sig = rsa.SignData(
            System.Text.Encoding.UTF8.GetBytes("hello keyguard"),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        Console.WriteLine($"\nSignature length: {sig.Length} bytes");
    }

    // ────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// creates a new KeyGuard-protected RSA key.
    /// </summary>
    /// <returns></returns>
    private static CngKey CreateFreshKey()
    {
        Console.WriteLine($"Creating NEW '{KeyName}' with KeyGuard protection…");

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

        p.Parameters.Add(
            new CngProperty("Length",
                            BitConverter.GetBytes(2048),
                            CngPropertyOptions.None));

        CngKey key = CngKey.Create(CngAlgorithm.Rsa, KeyName, p);

        Console.WriteLine($"Key created, size {key.KeySize} bits");
        Console.WriteLine(IsKeyGuardProtected(key)
            ? "KeyGuard flag set."
            : "KeyGuard flag missing!");

        return key;
    }

    /// <summary>
    /// is the KeyGuard flag set on the key?
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private static bool IsKeyGuardProtected(CngKey key)
    {
        if (!key.HasProperty("Virtual Iso", CngPropertyOptions.None))
            return false;

        byte[]? val = key.GetProperty("Virtual Iso", CngPropertyOptions.None).GetValue();
        return val is { Length: > 0 } && val[0] != 0;
    }
}
