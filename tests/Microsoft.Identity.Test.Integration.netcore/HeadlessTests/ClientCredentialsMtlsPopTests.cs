﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    // Tests in this class will run on .NET Core
    [TestClass]
    public class ClientCredentialsMtlsPopTests
    {
        private const string MsiAllowListedAppIdforSNI = "163ffef9-a313-45b4-ab2f-c7e2f5e0e23e";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public async Task Sni_Gets_Pop_Token_Successfully_TestAsync()
        {
            // Arrange: Use the public cloud settings for testing
            IConfidentialAppSettings settings = ConfidentialAppSettings.GetSettings(Cloud.Public);

            // Retrieve the certificate from settings
            X509Certificate2 cert = settings.GetCertificate();

            // Build Confidential Client Application with SNI certificate at App level
            IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder.Create(MsiAllowListedAppIdforSNI)
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithAzureRegion("westus3") //test slice region 
                .WithCertificate(cert, true)  
                .WithExperimentalFeatures()
                .WithTestLogging()
                .Build();

            // Act: Acquire token with MTLS Proof of Possession at Request level
            AuthenticationResult authResult = await confidentialApp
                .AcquireTokenForClient(settings.AppScopes)
                .WithMtlsProofOfPossession()
                .WithExtraQueryParameters("dc=ESTSR-PUB-WUS3-AZ1-TEST1&slice=TestSlice") //Feature in test slice 
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
    }
}
