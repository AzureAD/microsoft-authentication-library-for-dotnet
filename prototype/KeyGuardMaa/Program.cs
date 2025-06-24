// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using KeyGuard.Attestation;
using KeyGuard.Security;

class Program
{
    static void Main()
    {
        /* ── Ask for the attestation endpoint ────────────────────── */
        string maaEndpoint;
        do
        {
            Console.Write("Enter Azure Attestation endpoint: ");
            maaEndpoint = Console.ReadLine()?.Trim() ?? string.Empty;
        }
        while (string.IsNullOrWhiteSpace(maaEndpoint));

        Console.WriteLine($"Using endpoint: {maaEndpoint}\n");

        /* ── Decide if Key Guard is supported on this host ───────── */
        bool hasKeyGuard = OsSupport.IsServer2022OrLater();

        Console.WriteLine(hasKeyGuard
            ? "Server 2022/2025 detected – Key Guard path enabled."
            : "Non-server or pre-2022 build – using software RSA key.");

        /* ── Create the key (Key Guard if possible) ───────────────── */
        CngKey key;
        if (hasKeyGuard)
        {
            try
            {
                key = KeyGuardKey.CreateFresh();
            }
            catch (PlatformNotSupportedException ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Error -  {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("Using software RSA key instead.");
                key = CngKey.Create(CngAlgorithm.Rsa, null);
                hasKeyGuard = false;              // no attestation
            }
        }
        else
        {
            key = CngKey.Create(CngAlgorithm.Rsa, null);
        }

        using (key)
        {
            /* ── Attest only when we really have a Key Guard key ──── */
            if (hasKeyGuard && KeyGuardKey.IsKeyGuardProtected(key))
            {
                using var client = new AttestationClient();
                var r = client.Attest(maaEndpoint, key.Handle);

                switch (r.Status)
                {
                    case AttestationStatus.Success:
                        Console.WriteLine($"\nAttestation JWT:\n{r.Jwt}");
                        break;

                    case AttestationStatus.NativeError:
                        var rc = (AttestationResultErrorCode)r.NativeCode;
                        Console.WriteLine($"Native error {rc} (0x{r.NativeCode:X}):");
                        Console.WriteLine(AttestationErrors.Describe(rc));
                        break;

                    default:
                        Console.WriteLine($"Error - {r.Status}: {r.Message}");
                        break;
                }
            }
            else
            {
                Console.WriteLine("Attestation skipped (Key Guard not available).");
            }

            /* ── Optional sign demo ───────────────────────────────── */
            using RSA rsa = new RSACng(key);
            byte[] sig = rsa.SignData(
                System.Text.Encoding.UTF8.GetBytes("Hello KeyGuard"),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            Console.WriteLine($"\nSignature length: {sig.Length} bytes");
        }
    }
}
