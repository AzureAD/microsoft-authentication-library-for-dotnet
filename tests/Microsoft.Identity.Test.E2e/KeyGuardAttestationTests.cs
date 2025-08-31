// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.MtlsPop.Attestation;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Microsoft.Identity.Test.E2E
{
    [TestClass]
    public class KeyGuardAttestationTests
    {
        private static CngKey CreateKeyGuardKey(string keyName)
        {
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

            // Set 2048-bit RSA length
            p.Parameters.Add(new CngProperty(
                "Length",
                BitConverter.GetBytes(2048),
                CngPropertyOptions.None));

            return CngKey.Create(CngAlgorithm.Rsa, keyName, p);
        }

        private static bool IsKeyGuardProtected(CngKey key)
        {
            try
            {
                // KeyGuard exposes a "Virtual Iso" property that is non-zero when protected.
                // Same check used in #5448. :contentReference[oaicite:1]{index=1}
                var prop = key.GetProperty("Virtual Iso", CngPropertyOptions.None);
                var bytes = prop.GetValue();
                return bytes != null && bytes.Length >= 4 && BitConverter.ToInt32(bytes, 0) != 0;
            }
            catch
            {
                return false;
            }
        }

        [TestCategory("MI_E2E_AzureArc")]
        [RunOnAzureDevOps]
        [TestMethod]
        public void Attest_KeyGuardKey_OnAzureArc_Succeeds()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Inconclusive("KeyGuard attestation test runs only on Windows.");
            }

            // Attestation endpoint must be injected via pipeline variables on the Arc agent.
            var endpoint = Environment.GetEnvironmentVariable("TOKEN_ATTESTATION_ENDPOINT");
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                Assert.Inconclusive($"Set {"TOKEN_ATTESTATION_ENDPOINT"} on the Azure Arc agent to run this test.");
            }

            var clientId = "MSI_CLIENT_ID";

            string keyName = "MsalE2E_Keyguard";

            CngKey key = null;
            try
            {
                key = CreateKeyGuardKey(keyName);

                if (!IsKeyGuardProtected(key))
                {
                    Assert.Inconclusive("Key was created but not KeyGuard-protected. Is KeyGuard/VBS enabled on this machine?");
                }

                // Use the new public AttestationClient from the MtlsPop package. :contentReference[oaicite:2]{index=2}
                using var client = new AttestationClient();
                var result = client.Attest(endpoint, key.Handle, clientId);

                // Validate success + JWT shape (3 parts).
                Assert.AreEqual(AttestationStatus.Success, result.Status,
                    $"Attestation failed: status={result.Status}, nativeRc={result.NativeErrorCode}, msg={result.ErrorMessage}");
                Assert.IsFalse(string.IsNullOrEmpty(result.Jwt), "Expected a non-empty attestation JWT.");

                var parts = result.Jwt.Split('.');
                Assert.AreEqual(3, parts.Length, "Expected a JWT (3 parts).");
            }
            catch (CryptographicException ex)
            {
                Assert.Inconclusive("CNG/KeyGuard is not available or access is denied on this machine: " + ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                // Thrown by AttestationClient when the native DLL cannot be found/initialized.
                Assert.Inconclusive("Attestation native lib not available on this runner: " + ex.Message);
            }
            finally
            {
                try { key?.Delete(); } catch { /* best-effort cleanup */ }
            }
        }
    }
}
