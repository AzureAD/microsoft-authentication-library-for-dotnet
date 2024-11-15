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
        private static readonly string[] s_scopes = { "User.Read" };
        private const string LabAccessConfidentialClientId = "f62c5ae3-bf3a-4af5-afa8-a68b800396e9";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public async Task SNI_MtlsPopFlow_TestAsync()
        {
            // Arrange: Use the public cloud settings for testing
            IConfidentialAppSettings settings = ConfidentialAppSettings.GetSettings(Cloud.Public);

            // Retrieve the certificate from settings
            X509Certificate2 cert = settings.GetCertificate();

            // Build Confidential Client Application with SNI certificate at App level
            IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder.Create("e3628006-9c26-43b2-b5e7-07691426b65f")
                .WithAuthority("https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c")
                .WithAzureRegion("westus")
                .WithCertificate(cert, true)  // Configure SNI certificate at App level
                .WithExperimentalFeatures()
                .WithTestLogging()
                .Build();

            // Act: Acquire token with MTLS Proof of Possession at Request level
            AuthenticationResult authResult = await confidentialApp
                .AcquireTokenForClient(settings.AppScopes)
                .WithMtlsProofOfPossession()
                .OnBeforeTokenRequest(async (request) =>
                {
                    request.RequestUri = new Uri("https://mtlsauth.microsoft.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c/oauth2/v2.0/token?dc=ESTS-PUB-WUS3-AZ3-FD025-001&certboundtoken=true");

                    // Add custom headers to the request
                    request.Headers["x-client-sku"] = "MSAL.Test";
                    request.Headers["x-client-ver"] = "";
                })
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Assert: Check that the MTLS PoP token acquisition was successful
            Assert.IsNotNull(authResult, "The authentication result should not be null.");
            //Assert.AreEqual(Constants.BearerTokenType, authResult.TokenType, "Token type should be MTLS PoP");
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
