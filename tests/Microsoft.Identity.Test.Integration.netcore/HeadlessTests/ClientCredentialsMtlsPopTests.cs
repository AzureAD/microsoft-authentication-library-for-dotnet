// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    // Tests in this class will run on .NET Core
    [TestClass]
    public class ClientCredentialsMtlsPopTests
    {
        private const string MsiAllowListedAppIdforSNI = "163ffef9-a313-45b4-ab2f-c7e2f5e0e23e";
        private const string LabApp = "4b0db8c2-9f26-4417-8bde-3f0e3656f8e0";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        //[IgnoreOnClassicPipeline]
        [TestMethod]
        public async Task SNI_MtlsPopFlow_TestAsync()
        {
            // Arrange: Use the public cloud settings for testing
            IConfidentialAppSettings settings = ConfidentialAppSettings.GetSettings(Cloud.Public);

            // Retrieve the certificate from settings
            X509Certificate2 cert = settings.GetCertificate();

            // Build Confidential Client Application with SNI certificate at App level
            IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithAzureRegion("westus3") //test slice region 
                .WithCertificate(cert, true)  // Configure SNI certificate at App level
                .WithExperimentalFeatures()
                .WithTestLogging()
                .Build();

            // Act: Acquire token with MTLS Proof of Possession at Request level
            AuthenticationResult authResult = await confidentialApp
                .AcquireTokenForClient(settings.AppScopes)
                .WithMtlsProofOfPossession()
                .WithExtraQueryParameters("dc=ESTSR-PUB-WUS3-AZ1-TEST1&slice=TestSlice") //Feature in test slice 
                .WithSendX5C(true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert: Check that the MTLS PoP token acquisition was successful
            Assert.IsNotNull(authResult, "The authentication result should not be null.");
            Assert.AreEqual(Constants.MtlsPoPTokenType, authResult.TokenType, "Token type should be MTLS PoP");
            Assert.IsNotNull(authResult.AccessToken, "Access token should not be null");

            // Simulate cache retrieval to verify MTLS configuration is cached properly
            authResult = await confidentialApp
               .AcquireTokenForClient(settings.AppScopes)
               .WithMtlsProofOfPossession()
               .ExecuteAsync()
               .ConfigureAwait(false);

            // Assert: Verify that the token was fetched from cache on the second request
            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource, "Token should be retrieved from cache");
        }

        [TestMethod]
        public async Task SNI_MtlsPopFlow_FailureDueToInvalidConfiguration_TestAsync()
        {
            // Arrange: Use the public cloud settings for testing
            IConfidentialAppSettings settings = ConfidentialAppSettings.GetSettings(Cloud.Public);

            // Retrieve the certificate from settings
            X509Certificate2 cert = settings.GetCertificate();

            // Build Confidential Client Application with SNI certificate at App level
            IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder.Create(LabApp)
                .WithAuthority("https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca")
                .WithAzureRegion("westus3") //test slice region 
                .WithCertificate(cert, true)  // Configure SNI certificate at App level
                .WithExperimentalFeatures()
                .WithTestLogging()
                .Build();

            // Act & Assert
            MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
            {
                await confidentialApp
                    .AcquireTokenForClient(settings.AppScopes)
                    .WithMtlsProofOfPossession()
                    .WithExtraQueryParameters("dc=ESTSR-PUB-WUS3-AZ1-TEST1&slice=TestSlice")
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);

            // Verify the exception details
            Assert.IsNotNull(ex);
            Assert.AreEqual("invalid_request", ex.ErrorCode);
            StringAssert.Contains(ex.Message, "AADSTS100032"); 
            StringAssert.Contains(ex.Message, "AAD Regional does not support Mutual-TLS auth requests using non-subject name issuer certificates");
        }
    }
}
