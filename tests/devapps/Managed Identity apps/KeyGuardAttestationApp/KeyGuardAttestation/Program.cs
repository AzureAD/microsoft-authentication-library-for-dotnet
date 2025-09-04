// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using Microsoft.Identity.Client.MtlsPop.Attestation;

class Program
{
    static void Main(string[] args)
    {
        var endpoint = Environment.GetEnvironmentVariable("TOKEN_ATTESTATION_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            Console.WriteLine("TOKEN_ATTESTATION_ENDPOINT not set.");
            return;
        }
        Console.WriteLine($"Endpoint = {endpoint}");

        string clientId = Environment.GetEnvironmentVariable("MSI_CLIENT_ID") ?? "Test_Client_Id";
        Console.WriteLine($"ClientId = {clientId}");

        // Create a KeyGuard key
        string keyName = "TestKey_" + Guid.NewGuid().ToString("N");
        const string ProviderName = "Microsoft Software Key Storage Provider";
        const int NCRYPT_USE_VIRTUAL_ISOLATION_FLAG = 0x00020000;
        const int NCRYPT_USE_PER_BOOT_KEY_FLAG = 0x00040000;

        var p = new CngKeyCreationParameters
        {
            Provider = new CngProvider(ProviderName),
            ExportPolicy = CngExportPolicies.None,
            KeyUsage = CngKeyUsages.AllUsages,
            KeyCreationOptions =
                CngKeyCreationOptions.OverwriteExistingKey |
                (CngKeyCreationOptions)NCRYPT_USE_VIRTUAL_ISOLATION_FLAG |
                (CngKeyCreationOptions)NCRYPT_USE_PER_BOOT_KEY_FLAG,
        };
        p.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(2048), CngPropertyOptions.None));

        using var key = CngKey.Create(CngAlgorithm.Rsa, keyName, p);

        // Try attestation
        try
        {
            using var client = new AttestationClient();
            var result = client.Attest(endpoint, key.Handle, clientId);

            Console.WriteLine($"Status = {result.Status}, NativeRc = {result.NativeErrorCode}");
            if (!string.IsNullOrEmpty(result.Jwt))
            {
                Console.WriteLine(string.Concat("JWT (truncated): ", result.Jwt.AsSpan(0, 50), "..."));
            }
            else
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex);
        }
        finally
        {
            try
            { CngKey.Open(keyName, new CngProvider(ProviderName)).Delete(); }
            catch { }
        }
    }
}

