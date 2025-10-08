// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using Microsoft.Identity.Client.ManagedIdentity.KeyProviders;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.E2E
{
    [TestClass]
    public class ManagedIdentityKeyAcquisitionTests
    {
        private const string SoftwareKspName = "Microsoft Software Key Storage Provider";

        // Runs on the AzureArc agent: must obtain a VBS/KeyGuard key.
        [TestMethod]
        [TestCategory("MI_E2E_KeyAcquisition_KeyGuard")]
        [RunOnAzureDevOps]
        public void KeyAcquisition_Fetches_KeyGuard_Key()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("This test runs on Windows agents only.");
            }

            bool ok = WindowsCngKeyOperations.TryGetOrCreateKeyGuard(logger: null, out RSA rsa);
            Assert.IsTrue(ok, "Expected KeyGuard key on AzureArc agent.");

            using (rsa)
            {
                var rsacng = rsa as RSACng;
                Assert.IsNotNull(rsacng, "Expected RSACng for KeyGuard.");
                Assert.IsTrue(
                    WindowsCngKeyOperations.IsKeyGuardProtected(rsacng.Key),
                    "Expected KeyGuard (VBS) protected key on AzureArc agent.");
            }
        }

        // Runs on the IMDS agent: must obtain a TPM/PCP hardware key (user scope).
        [TestMethod]
        [TestCategory("MI_E2E_KeyAcquisition_Hardware")]
        [RunOnAzureDevOps]
        public void KeyAcquisition_Fetches_Hardware_Key()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("This test runs on Windows agents only.");
            }

            bool ok = WindowsCngKeyOperations.TryGetOrCreateHardwareRsa(logger: null, out RSA rsa);
            Assert.IsTrue(ok, "Expected TPM hardware key on IMDS agent.");

            using (rsa)
            {
                var rsacng = rsa as RSACng;
                Assert.IsNotNull(rsacng, "Expected RSACng for hardware key.");

                Assert.AreEqual(
                    SoftwareKspName,
                    rsacng.Key.Provider.Provider,
                    "Expected TPM-backed key via Microsoft Software Key Storage Provider.");

                // TPM keys created with ExportPolicy=None should not allow private export.
                bool privateExportable = true;
                try
                { _ = rsacng.ExportParameters(true); }
                catch (CryptographicException) { privateExportable = false; }
                catch (NotSupportedException) { privateExportable = false; }

                Assert.IsFalse(privateExportable, "Hardware (TPM) key should be non-exportable.");
            }
        }
    }
}
