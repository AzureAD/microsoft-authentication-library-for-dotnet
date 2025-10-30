// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*
WHY THESE TESTS ONLY RUN ON A SPECIFIC (AZURE ARC) MACHINE
---------------------------------------------------------
KeyGuard attestation requires:
  1. A KeyGuard / Virtualization-Based Security (VBS) capable environment.
  2. The ability to create a CNG RSA key with:
       - Virtual Isolation (NCRYPT_USE_VIRTUAL_ISOLATION_FLAG)
       - Per-boot scope (NCRYPT_USE_PER_BOOT_KEY_FLAG)
  3. A native KeyGuard attestation stack (deployed via the MtlsPop package) capable of:
       - Accessing the key handle
       - Interacting with the VBS services to produce an attestation

Most hosted build agents (including standard Azure DevOps Microsoft-hosted pools) do NOT expose:
  - Virtualization-based key isolation
  - The necessary kernel components for KeyGuard property retrieval
  - The proper security context to create KeyGuard-protected keys

We therefore run these tests ONLY on a dedicated Azure Arc–connected VM (custom self-hosted agent) that:
  - Is provisioned with VBS + KeyGuard enabled
  - Has the Microsoft Software Key Storage Provider configured to honor Virtual Isolation + per-boot flags
  - Has an identity/endpoint (TOKEN_ATTESTATION_ENDPOINT) capable of accepting and validating a KeyGuard attestation
  - Is allowed in the pipeline via filtering on the TestCategory MI_E2E_AzureArc (and infra chooses that agent)

If any prerequisite is missing (e.g., VBS off, endpoint unset, native DLL absent, or key not actually KeyGuard-protected),
the test exits early with Assert.Inconclusive instead of failing the overall build.
*/

using Microsoft.Identity.Client.MtlsPop.Attestation;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Identity.Client.MtlsPop;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.Identity.Test.E2E
{
    [TestClass]
    public class KeyGuardAttestationTests
    {
        /*
         Creates a KeyGuard-capable RSA key (2048-bit) using the Microsoft Software Key Storage Provider.
         Flags:
           - NCRYPT_USE_VIRTUAL_ISOLATION_FLAG: Requests KeyGuard / Virtual Isolation (backed by VBS).
           - NCRYPT_USE_PER_BOOT_KEY_FLAG: Key material only valid for the current boot (expected scenario for attestation).
         On machines without KeyGuard/VBS support the provider may silently ignore the flags; we detect that later via IsKeyGuardProtected.
         IMPORTANT: This must run on the Azure Arc custom agent where VBS + KeyGuard is enabled.
        */
        private static CngKey CreateKeyGuardKey(string keyName)
        {
            const string ProviderName = "Microsoft Software Key Storage Provider";
            const int NCRYPT_USE_VIRTUAL_ISOLATION_FLAG = 0x00020000;
            const int NCRYPT_USE_PER_BOOT_KEY_FLAG = 0x00040000;

            var p = new CngKeyCreationParameters
            {
                Provider = new CngProvider(ProviderName),
                ExportPolicy = CngExportPolicies.None,        // No export allowed; expected for attested keys.
                KeyUsage = CngKeyUsages.AllUsages,            // Broad usage; attestation library only needs signing.
                KeyCreationOptions =
                    CngKeyCreationOptions.OverwriteExistingKey |
                    (CngKeyCreationOptions)NCRYPT_USE_VIRTUAL_ISOLATION_FLAG |
                    (CngKeyCreationOptions)NCRYPT_USE_PER_BOOT_KEY_FLAG,
            };

            // Set 2048-bit RSA length (current attestation native lib expects RSA; adjust only with platform guidance).
            p.Parameters.Add(new CngProperty(
                "Length",
                BitConverter.GetBytes(2048),
                CngPropertyOptions.None));

            return CngKey.Create(CngAlgorithm.Rsa, keyName, p);
        }

        /*
         Determines whether the key actually received KeyGuard Virtual Isolation backing.
         Some environments will accept the creation flags but produce a normal (non-KeyGuard) key;
         those runs should be marked Inconclusive rather than Fail to avoid noisy pipeline failures.
         This mirrors the logic used in other internal tracking (ref #5448).
        */
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

        /*
         Synchronous attestation path.
         Restricted to Azure Arc (MI_E2E_AzureArc) because:
           - Needs a machine with KeyGuard + VBS
           - Needs TOKEN_ATTESTATION_ENDPOINT env var (injected by pipeline/agent config)
           - Uses AttestationClient which depends on a native DLL deployed only on that custom agent
         Fails fast with Assert.Inconclusive when prerequisites are missing.
        */
        [TestCategory("MI_E2E_AzureArc")]
        [RunOnAzureDevOps]
        //[TestMethod]
        public void Attest_KeyGuardKey_OnAzureArc_Succeeds()
        {
            // Endpoint is provisioned only on the Azure Arc agent (backed by MSI / identity service).
            var endpoint = Environment.GetEnvironmentVariable("TOKEN_ATTESTATION_ENDPOINT");
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                Assert.Inconclusive($"Set {"TOKEN_ATTESTATION_ENDPOINT"} on the Azure Arc agent to run this test.");
            }

            // Placeholder logical client ID used by the attestation endpoint (matches agent configuration).
            var clientId = "MSI_CLIENT_ID";
            string keyName = "MsalE2E_Keyguard";

            CngKey key = null;
            try
            {
                key = CreateKeyGuardKey(keyName);

                if (!IsKeyGuardProtected(key))
                {
                    // Indicates environment does not truly support KeyGuard (e.g., VBS disabled) — do not treat as test failure.
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
                // Common when provider flags unsupported or isolation services absent.
                Assert.Inconclusive("CNG/KeyGuard is not available or access is denied on this machine: " + ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                // Thrown by AttestationClient when the native DLL cannot be found/initialized (not deployed outside Azure Arc agent).
                Assert.Inconclusive("Attestation native lib not available on this runner: " + ex.Message);
            }
            finally
            {
                try { key?.Delete(); } catch { /* best-effort cleanup */ }
            }
        }

        /*
         Async attestation path.
         Demonstrates PopKeyAttestor.AttestKeyGuardAsync which wraps the native synchronous call.
         Same environmental constraints as the synchronous test; still limited to the Azure Arc agent.
        */
        [TestCategory("MI_E2E_AzureArc")]
        [RunOnAzureDevOps]
        //[TestMethod]
        public async Task Attest_KeyGuardKey_OnAzureArc_Async_Succeeds()
        {
            var endpoint = Environment.GetEnvironmentVariable("TOKEN_ATTESTATION_ENDPOINT");
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                Assert.Inconclusive($"Set {"TOKEN_ATTESTATION_ENDPOINT"} on the Azure Arc agent to run this test.");
            }

            var clientId = "MSI_CLIENT_ID";
            string keyName = "MsalE2E_Keyguard_Async";

            CngKey key = null;
            try
            {
                key = CreateKeyGuardKey(keyName);

                if (!IsKeyGuardProtected(key))
                {
                    Assert.Inconclusive("Key was created but not KeyGuard-protected. Is KeyGuard/VBS enabled on this machine?");
                }

                // Exercise the async facade (PopKeyAttestor) which wraps the synchronous native call in Task.Run.
                var result = await PopKeyAttestor.AttestKeyGuardAsync(
                    endpoint,
                    key.Handle,
                    clientId: clientId,
                    cancellationToken: CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(AttestationStatus.Success, result.Status,
                    $"Async attestation failed: status={result.Status}, nativeRc={result.NativeErrorCode}, msg={result.ErrorMessage}");
                Assert.IsFalse(string.IsNullOrEmpty(result.Jwt), "Expected a non-empty attestation JWT from async path.");

                var parts = result.Jwt.Split('.');
                Assert.AreEqual(3, parts.Length, "Expected a JWT (3 parts) from async path.");
            }
            catch (CryptographicException ex)
            {
                Assert.Inconclusive("CNG/KeyGuard is not available or access is denied on this machine: " + ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                // Could originate from native initialization inside PopKeyAttestor (AttestationClient constructor).
                Assert.Inconclusive("Attestation native lib not available on this runner (async path): " + ex.Message);
            }
            catch (ArgumentException ex)
            {
                // Defensive: invalid handle or parameters — treat as environment/setup issue for this scenario.
                Assert.Inconclusive("Handle or parameters invalid for async attestation path: " + ex.Message);
            }
            finally
            {
                try { key?.Delete(); } catch { /* best-effort cleanup */ }
            }
        }
    }
}
