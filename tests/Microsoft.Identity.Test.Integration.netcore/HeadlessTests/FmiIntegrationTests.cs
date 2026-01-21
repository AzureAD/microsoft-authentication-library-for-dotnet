// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common.Core.Helpers;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using OpenTelemetry.Logs;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    /// <summary>
    /// The tests in this file are demonstrations of the various authentication flows outlined in the "FMI protocol spec v1.0" Section 3.2
    /// https://microsoft.sharepoint.com/:w:/t/aad/protocols/EThMH6es0UNKhsFlVgBiBegByuZQ6CnaCzzAdAV0excHVA?e=m5xXtV
    /// Test apps are located in MSID Lab 4
    /// Client app: 4df2cbbb-8612-49c1-87c8-f334d6d065ad
    /// Resource app: 3091264c-7afb-45d4-b527-39737ee86187
    /// </summary>
    [TestClass]
    public class FmiIntegrationTests
    {
        private byte[] _serializedCache;
        private const string AzureRegion = "westus3";
        private const string TenantId = ConfidentialAppSettings.ID4SLab1TenantId;
        private const string RmaClientId = "3bf56293-fbb5-42bd-a407-248ba7431a8c";
        private const string WebApiScope = "api://aa464f73-2868-4f67-b0e7-fc2f749e757f/.default";
        private const string ExpectedResourceAudience = "api://aa464f73-2868-4f67-b0e7-fc2f749e757f";
        private const string ExpectedFmiCredentialAudience = "a9dd8a2a-df54-4ae0-84f9-38c8d57e5265";

        [TestMethod]
        //RMA getting FMI cred for a leaf entity or sub-RMA
        public async Task Flow1_Credential_From_Cert()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            //Fmi app/scenario parameters            
            var scope = "api://AzureFMITokenExchange/.default";

            //Act
            //Create application
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(RmaClientId)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithCertificate(cert, sendX5C: true) //sendX5c enables SN+I auth which is required for FMI flows
                        .WithAzureRegion(AzureRegion)
                        .BuildConcrete();

            //Configure token cache serialization
            confidentialApp.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
            confidentialApp.AppTokenCache.SetAfterAccess(AfterCacheAccess);

            //Recording test data for Asserts
            string expectedFmiPathHash = "zm2n0E62zwTsnNsozptLsoOoB_C7i-GfpxHYQQINJUw";
            var expectedExternalCacheKey = $"{RmaClientId}_{TenantId}_{expectedFmiPathHash}_AppTokenCache"; // last part is the SHA256 of the fmi path
            var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(
                (args) => Assert.AreEqual(args.SuggestedCacheKey, expectedExternalCacheKey));

            //Acquire Fmi Cred
            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            //Assert
            var expectedInternalCacheKey = $"-login.microsoftonline.com-atext-{RmaClientId}-{TenantId}-{scope}-{expectedFmiPathHash}".ToLowerInvariant();
            AssertResults(authResult,
                          confidentialApp,
                          expectedInternalCacheKey,
                          ExpectedFmiCredentialAudience,
                          "SomeFmiPath/FmiCredentialPath");
        }

        [TestMethod]
        //RMA getting FMI token for a leaf entity
        public async Task Flow2_Token_From_CertTest()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            //Act
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(RmaClientId)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithCertificate(cert, sendX5C: true)
                        .WithAzureRegion(AzureRegion)
                        .BuildConcrete();

            confidentialApp.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
            confidentialApp.AppTokenCache.SetAfterAccess(AfterCacheAccess);

            //Recording test data for Asserts
            string expectedFmiPathHash = "zm2n0E62zwTsnNsozptLsoOoB_C7i-GfpxHYQQINJUw";
            var expectedExternalCacheKey = $"{RmaClientId}_{TenantId}_{expectedFmiPathHash}_AppTokenCache";
            var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(
                (args) => Assert.AreEqual(args.SuggestedCacheKey, expectedExternalCacheKey));

            var authResult = await confidentialApp.AcquireTokenForClient(new[] { WebApiScope })
                                                    .WithFmiPath("SomeFmiPath/FmiCredentialPath")
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            var expectedInternalCacheKey = $"-login.microsoftonline.com-atext-{RmaClientId}-{TenantId}-{WebApiScope}-{expectedFmiPathHash}".ToLowerInvariant();
            AssertResults(authResult,
                          confidentialApp,
                          expectedInternalCacheKey,
                          ExpectedResourceAudience,
                          "SomeFmiPath/FmiCredentialPath");
        }

        [TestMethod]
        //Sub-RMA getting FMI cred for a child sub-RMA
        public async Task Flow3_FmiCredential_From_AnotherFmiCredential()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var clientId = "urn:microsoft:identity:fmi";
            var scope = "api://AzureFMITokenExchange/.default";

            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create("urn:microsoft:identity:fmi")
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithClientAssertion((options) => GetFmiCredentialFromRma(options))
                        .WithAzureRegion(AzureRegion)
                        .BuildConcrete();

            confidentialApp.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
            confidentialApp.AppTokenCache.SetAfterAccess(AfterCacheAccess);

            //Recording test data for Asserts
            string expectedFmiPathHash = "7CX57Q63os7benQ6ER0sxgJPtNQSv7TGb5zexcidFoI";
            var expectedExternalCacheKey = $"{clientId}_{TenantId}_{expectedFmiPathHash}_AppTokenCache";
            var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(
                (args) => Assert.AreEqual(args.SuggestedCacheKey, expectedExternalCacheKey));

            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath("SomeFmiPath/Path")
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            var expectedInternalCacheKey = $"-login.microsoftonline.com-atext-{clientId}-{TenantId}-{scope}-{expectedFmiPathHash}".ToLowerInvariant();
            AssertResults(authResult,
                          confidentialApp,
                          expectedInternalCacheKey,
                          ExpectedFmiCredentialAudience,
                          "SomeFmiPath/Path");
        }

        [TestMethod]
        [Ignore] // Flow 4 is not currently enabled
        // Sub-RMA getting a Federated Identity Credential (FIC), using FMI credential
        public async Task Flow4_SubRma_FIC_From_FmiCredential()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var clientId = "urn:microsoft:identity:fmi";
            var scope = "api://AzureADTokenExchange/.default";

            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithClientAssertion((options) => GetFmiCredentialFromRma(options))
                        .WithAzureRegion(AzureRegion)
                        .BuildConcrete();

            confidentialApp.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
            confidentialApp.AppTokenCache.SetAfterAccess(AfterCacheAccess);

            //Recording test data for Asserts
            string expectedFmiPathHash = ""; // If you have a value, replace accordingly
            var expectedExternalCacheKey = $"{clientId}_{TenantId}_{expectedFmiPathHash}_AppTokenCache";
            var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(
                (args) => Assert.AreEqual(args.SuggestedCacheKey, expectedExternalCacheKey));

            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath("SomeFmiPath/Path")
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            var expectedInternalCacheKey = $"-login.microsoftonline.com-atext-{clientId}-{TenantId}-{scope}-{expectedFmiPathHash}".ToLowerInvariant();
            AssertResults(authResult,
                          confidentialApp,
                          expectedInternalCacheKey,
                          ExpectedResourceAudience,
                          "SomeFmiPath/Path");
        }

        [TestMethod]
        //Sub-RMA getting FMI token for leaf entity
        public async Task Flow5_FmiToken_From_FmiCred()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var clientId = "urn:microsoft:identity:fmi";            

            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithClientAssertion((options) => GetFmiCredentialFromRma(options))
                        .WithAzureRegion(AzureRegion)
                        .BuildConcrete();

            confidentialApp.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
            confidentialApp.AppTokenCache.SetAfterAccess(AfterCacheAccess);

            //Recording test data for Asserts
            string expectedFmiPathHash = "7CX57Q63os7benQ6ER0sxgJPtNQSv7TGb5zexcidFoI";
            var expectedExternalCacheKey = $"{clientId}_{TenantId}_{expectedFmiPathHash}_AppTokenCache";
            var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(
                (args) => Assert.AreEqual(args.SuggestedCacheKey, expectedExternalCacheKey));

            var authResult = await confidentialApp.AcquireTokenForClient(new[] { WebApiScope })
                                                    .WithFmiPath("SomeFmiPath/Path")
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(authResult.AccessToken) as JwtSecurityToken;
            Assert.IsFalse(jsonToken.Payload.ContainsKey("xms_attr"), "xms_attr claim should not exist in the token");

            var expectedInternalCacheKey = $"-login.microsoftonline.com-atext-{clientId}-{TenantId}-{WebApiScope}-{expectedFmiPathHash}".ToLowerInvariant();
            AssertResults(authResult,
                          confidentialApp,
                          expectedInternalCacheKey,
                          ExpectedResourceAudience,
                          "SomeFmiPath/Path");
        }

        private static async Task<string> GetFmiCredentialFromRma(AssertionRequestOptions options)
        {
            //Fmi app/scenario parameters
            var clientId = "4df2cbbb-8612-49c1-87c8-f334d6d065ad";
            var scope = "api://AzureFMITokenExchange/.default";

            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            //Create application
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority("https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca", true)
                        .WithCertificate(cert, sendX5C: true) //sendX5c enables SN+I auth which is required for FMI flows
                        .WithAzureRegion(AzureRegion)
                        .BuildConcrete();

            //Acquire Token
            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            return authResult.AccessToken;
        }

        private void BeforeCacheAccess(TokenCacheNotificationArgs args)
        {
            args.TokenCache.DeserializeMsalV3(_serializedCache);
        }

        private void AfterCacheAccess(TokenCacheNotificationArgs args)
        {
            _serializedCache = args.TokenCache.SerializeMsalV3();
        }

        private void AssertResults(
            AuthenticationResult authResult,
            ConfidentialClientApplication confidentialApp,
            string expectedInternalCacheKey,
            string expectedAudience,
            string expectedFmiPath)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(authResult.AccessToken) as JwtSecurityToken;
            var subject = jsonToken.Payload["sub"].ToString();
            var audience = jsonToken.Payload["aud"].ToString();
            var token = confidentialApp.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First();

            Assert.IsNotNull(authResult);
            if (audience.EndsWith("/.default"))
            {
                audience = audience.Substring(0, audience.Length - "/.default".Length);
            }
            Assert.AreEqual(expectedAudience, audience);
            Assert.IsTrue(subject.Contains(expectedFmiPath));
            Assert.AreEqual(expectedInternalCacheKey, token.CacheKey);
        }

        [TestMethod]
        //RMA getting FMI token with attributes for a leaf entity.
        public async Task Flow6_Token_withAttributeTest()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            //Act
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(RmaClientId)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithCertificate(cert, sendX5C: true)
                        .WithAzureRegion(AzureRegion)
                        .BuildConcrete();

            confidentialApp.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
            confidentialApp.AppTokenCache.SetAfterAccess(AfterCacheAccess);

            //Recording test data for Asserts
            string expectedFmiPathHash = "NlReJx-vneHBYB53A1WmQWCgwPobHn98udAu2hCDkJ8";
            var expectedExternalCacheKey = $"{RmaClientId}_{TenantId}_{expectedFmiPathHash}_AppTokenCache";
            var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(
                (args) => Assert.AreEqual(args.SuggestedCacheKey, expectedExternalCacheKey));
            var attributesString = "{\"FavoriteColor\": \"Blue\", \"file:/c/users/foobar/documents/info.txt\": \"{\\\"permissions\\\":[\\\"read\\\",\\\"write\\\"]}\"}";
            var authResult = await confidentialApp.AcquireTokenForClient(new[] { "api://AzureFMITokenExchange/.default" })
                                .WithAttributes(attributesString)
                                                    .WithFmiPath("SomeFmiPath/FmiCredentialPath")
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(authResult.AccessToken) as JwtSecurityToken;
            Assert.IsTrue(jsonToken.Payload.ContainsKey("xms_attr"), "xms_attr claim should exist in the token");
            var xmsAttr = jsonToken.Payload["xms_attr"].ToString();
            Assert.IsTrue(xmsAttr.Contains(attributesString), "xms_attr claim should contain attribute string");

            var expectedInternalCacheKey = $"-login.microsoftonline.com-atext-{RmaClientId}-{TenantId}-{"api://AzureFMITokenExchange/.default"}-{expectedFmiPathHash}".ToLowerInvariant();
            AssertResults(authResult,
                          confidentialApp,
                          expectedInternalCacheKey,
                          "a9dd8a2a-df54-4ae0-84f9-38c8d57e5265", // this is app id for the audience for api://AzureFMITokenExchange/.defaul
                          "SomeFmiPath/FmiCredentialPath");
        }
    }
}
