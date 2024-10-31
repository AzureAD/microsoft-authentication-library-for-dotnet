// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Region;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    [DeploymentItem(@"Resources\CustomInstanceMetadata.json")]
    public class MtlsPopTests : TestBase
    {
        public const string EastUsRegion = "eastus";

        [TestCleanup]
        public override void TestCleanup()
        {
            Environment.SetEnvironmentVariable("REGION_NAME", null);
        }

        [TestMethod]
        public async Task MtlsPopWithoutCertificateAsync()
        {
            var app = ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithClientSecret(TestConstants.ClientSecret)
                            .WithExperimentalFeatures()
                            .Build();

            // Set UseMtlsPop on the request without a certificate
            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForClient(TestConstants.s_scope)
                   .WithMtlsProofOfPossession() // Enables MTLS PoP
                   .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsCertificateNotProvided, ex.ErrorCode);
        }

        [TestMethod]
        public async Task MtlsPopWithoutRegionAsync()
        {
            Environment.SetEnvironmentVariable("REGION_NAME", null); // Ensure no region is set

            var app = ConfidentialClientApplicationBuilder
                            .Create(TestConstants.ClientId)
                            .WithCertificate(CertHelper.GetOrCreateTestCert())
                            .WithExperimentalFeatures()
                            .Build();

            // Set UseMtlsPop on the request without specifying a region
            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForClient(TestConstants.s_scope)
                   .WithMtlsProofOfPossession() // Enables MTLS PoP
                   .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsPopWithoutRegion, ex.ErrorCode);
        }
    }
}
