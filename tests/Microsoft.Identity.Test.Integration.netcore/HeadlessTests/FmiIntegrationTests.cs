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

namespace Microsoft.Identity.Test.Integration.NetCore.HeadlessTests
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
        private const string _testslice1 = "dc=ESTS-PUB-SCUS-LZ1-FD000-TEST1"; //Enables MSAL to target ESTS Test slice
        private const string _testslice2 = "dc=ESTSR-PUB-WUS-LZ1-TEST"; //Updated slice for regional tests

        [TestMethod]
        //RMA getting FMI cred for a leaf entity or sub-RMA
        [Ignore("Requires Coorp net to run")]
        public async Task Flow1_RmaCredential_From_CertTestAsync()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            string expectedExternalCacheKey = null;

            Action<TokenCacheNotificationArgs> extCacheKeyEvaluator = (args) =>
            {
                if (expectedExternalCacheKey != null)
                {
                    Assert.IsTrue(args.SuggestedCacheKey.Contains(expectedExternalCacheKey));
                }
            };

            //Fmi app/scenario parameters
            var clientId = "4df2cbbb-8612-49c1-87c8-f334d6d065ad";
            var scope = "api://AzureFMITokenExchange/.default";

            //Act
            //Create application
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority("https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca", true)
                        .WithExtraQueryParameters(_testslice1) //Enables MSAL to target ESTS Test slice
                        .WithCertificate(cert, sendX5C: true) //sendX5c enables SN+I auth which is required for FMI flows
                        .BuildConcrete();

            //Configure token cache serialization
            confidentialApp.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
            confidentialApp.AppTokenCache.SetAfterAccess(AfterCacheAccess);

            //Recording test data for Asserts
            var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(extCacheKeyEvaluator);
            expectedExternalCacheKey = "zm2n0E62zwTsnNsozptLsoOoB_C7i-GfpxHYQQINJUw";

            //Acquire Fmi Cred
            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            //Assert
            AssertResults(authResult,
                          confidentialApp,
                          "-login.windows.net-atext-4df2cbbb-8612-49c1-87c8-f334d6d065ad-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-api://azurefmitokenexchange/.default-zm2n0e62zwtsnnsozptlsooob_c7i-gfpxhyqqinjuw",
                          "a9dd8a2a-df54-4ae0-84f9-38c8d57e5265",
                          "SomeFmiPath/FmiCredentialPath");
        }

        [TestMethod]
        //RMA getting FMI token for a leaf entity
        [Ignore("Requires Coorp net to run")]
        public async Task Flow2_RmaToken_From_CertTestAsync()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            string expectedExternalCacheKey = null;

            Action<TokenCacheNotificationArgs> extCacheKeyEvaluator = (args) =>
            {
                if (expectedExternalCacheKey != null)
                {
                    Assert.IsTrue(args.SuggestedCacheKey.Contains(expectedExternalCacheKey));
                }
            };

            //Fmi app/scenario parameters
            var clientId = "4df2cbbb-8612-49c1-87c8-f334d6d065ad";
            var scope = "3091264c-7afb-45d4-b527-39737ee86187/.default"; //Guid for api://AzureFMITokenExchange

            //Act
            //Create application
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority("https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca", true)
                        .WithExtraQueryParameters(_testslice1) //Enables MSAL to target ESTS Test slice
                        .WithCertificate(cert, sendX5C: true) //sendX5c enables SN+I auth which is required for FMI flows
                        .BuildConcrete();

            //Configure token cache serialization
            confidentialApp.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
            confidentialApp.AppTokenCache.SetAfterAccess(AfterCacheAccess);

            //Recording test data for Asserts
            var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(extCacheKeyEvaluator);
            expectedExternalCacheKey = "zm2n0E62zwTsnNsozptLsoOoB_C7i-GfpxHYQQINJUw";

            //Acquire Token
            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            //Assert
            AssertResults(authResult,
                          confidentialApp,
                          "-login.windows.net-atext-4df2cbbb-8612-49c1-87c8-f334d6d065ad-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-3091264c-7afb-45d4-b527-39737ee86187/.default-zm2n0e62zwtsnnsozptlsooob_c7i-gfpxhyqqinjuw",
                          "3091264c-7afb-45d4-b527-39737ee86187",
                          "SomeFmiPath/FmiCredentialPath");
        }

        [TestMethod]
        //Sub-RMA getting FMI cred for a child sub-RMA
        [Ignore("Requires Coorp net to run")]
        public async Task Flow3_FmiCredential_From_RmaCredential()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            string expectedExternalCacheKey = null;

            Action<TokenCacheNotificationArgs> extCacheKeyEvaluator = (args) =>
            {
                if (expectedExternalCacheKey != null)
                {
                    Assert.IsTrue(args.SuggestedCacheKey.Contains(expectedExternalCacheKey));
                }
            };

            //Fmi app/scenario parameters
            var clientId = "urn:microsoft:identity:fmi";
            var scope = "api://AzureFMITokenExchange/.default";

            //Act
            //Create application
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority("https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca", true)
                        .WithExtraQueryParameters(_testslice1) //Enables MSAL to target ESTS Test slice
                        .WithClientAssertion((options) => GetParentCredential(options, _testslice1)) //This api acquires the FMI credential needed to authenticate
                        .BuildConcrete();

            //FOR TESTING ONLY: Configure token cache serialization
            confidentialApp.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
            confidentialApp.AppTokenCache.SetAfterAccess(AfterCacheAccess);

            //Recording test data for Asserts
            var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(extCacheKeyEvaluator);
            expectedExternalCacheKey = "7CX57Q63os7benQ6ER0sxgJPtNQSv7TGb5zexcidFoI";

            //Acquire Fmi Cred
            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath("SomeFmiPath/Path") //Sets fmi path in client credential request.
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            //Assert
            AssertResults(authResult,
                          confidentialApp,
                          "-login.windows.net-atext-urn:microsoft:identity:fmi-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-api://azurefmitokenexchange/.default-7cx57q63os7benq6er0sxgjptnqsv7tgb5zexcidfoi",
                          "a9dd8a2a-df54-4ae0-84f9-38c8d57e5265",
                          "SomeFmiPath/Path");
        }

        [TestMethod]
        //Sub-RMA getting FIC for leaf entity.
        [Ignore("Requires Coorp net to run")]
        public async Task Flow4_SubRma_FmiCredential_For_leaf()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            string expectedExternalCacheKey = null;

            Action<TokenCacheNotificationArgs> extCacheKeyEvaluator = (args) =>
            {
                if (expectedExternalCacheKey != null)
                {
                    Assert.IsTrue(args.SuggestedCacheKey.Contains(expectedExternalCacheKey));
                }
            };

            //Fmi app/scenario parameters
            var clientId = "urn:microsoft:identity:fmi";
            var scope = "3091264c-7afb-45d4-b527-39737ee86187/.default"; //Guid for api://AzureFMITokenExchange

            //Act
            //Create application
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority("https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca", true)
                        .WithExtraQueryParameters(_testslice1) //Enables MSAL to target ESTS Test slice
                        .WithClientAssertion((options) => GetParentCredential(options, _testslice1)) //This api acquires the FMI credential needed to authenticate
                        .BuildConcrete();

            //Configure token cache serialization
            confidentialApp.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
            confidentialApp.AppTokenCache.SetAfterAccess(AfterCacheAccess);

            //Recording test data for Asserts
            var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(extCacheKeyEvaluator);
            expectedExternalCacheKey = "";

            //Acquire Fmi Cred
            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath("SomeFmiPath/Path") //Sets fmi path in client credential request.
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            //Assert
            AssertResults(authResult,
                          confidentialApp,
                          "-login.windows.net-atext-urn:microsoft:identity:fmi-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-3091264c-7afb-45d4-b527-39737ee86187/.default-7cx57q63os7benq6er0sxgjptnqsv7tgb5zexcidfoi",
                          "3091264c-7afb-45d4-b527-39737ee86187",
                          "SomeFmiPath/Path");
        }

        [TestMethod]
        //Sub-RMA getting FMI token for leaf entity
        [Ignore("Requires Coorp net to run")]
        public async Task Flow5_SubRma_FmiToken_From_FmiCred_For_leafTestAsync()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            string expectedExternalCacheKey = null;

            Action<TokenCacheNotificationArgs> extCacheKeyEvaluator = (args) =>
            {
                if (expectedExternalCacheKey != null)
                {
                    Assert.IsTrue(args.SuggestedCacheKey.Contains(expectedExternalCacheKey));
                }
            };

            //Fmi app/scenario parameters
            var clientId = "urn:microsoft:identity:fmi";
            var scope = "3091264c-7afb-45d4-b527-39737ee86187/.default"; //Guid for api://AzureFMITokenExchange

            //Act
            //Create application
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority("https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca", true)
                        .WithExtraQueryParameters(_testslice1) //Enables MSAL to target ESTS Test slice
                        .WithClientAssertion((options) => GetParentCredential(options, _testslice1)) //This api acquires the FMI credential needed to authenticate
                        .BuildConcrete();

            //Configure token cache serialization
            confidentialApp.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
            confidentialApp.AppTokenCache.SetAfterAccess(AfterCacheAccess);

            //Recording test data for Asserts
            var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(extCacheKeyEvaluator);
            expectedExternalCacheKey = "7CX57Q63os7benQ6ER0sxgJPtNQSv7TGb5zexcidFoI";

            //Acquire Fmi Cred
            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath("SomeFmiPath/Path") //Sets fmi path in client credential request.
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            //Assert
            AssertResults(authResult,
                          confidentialApp,
                          "-login.windows.net-atext-urn:microsoft:identity:fmi-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-3091264c-7afb-45d4-b527-39737ee86187/.default-7cx57q63os7benq6er0sxgjptnqsv7tgb5zexcidfoi",
                          "3091264c-7afb-45d4-b527-39737ee86187",
                          "SomeFmiPath/Path");
        }

        private static async Task<string> GetParentCredential(AssertionRequestOptions options, string eqParameters)
        {
            //Fmi app/scenario parameters
            var clientId = "4df2cbbb-8612-49c1-87c8-f334d6d065ad";
            var scope = "api://AzureFMITokenExchange/.default";

            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            //Create application
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority("https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca", true)
                        .WithExtraQueryParameters(eqParameters) //Enables MSAL to target ESTS Test slice
                        .WithCertificate(cert, sendX5C: true) //sendX5c enables SN+I auth which is required for FMI flows
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
            Assert.AreEqual(expectedAudience, audience);
            Assert.IsTrue(subject.Contains(expectedFmiPath));
            Assert.AreEqual(expectedInternalCacheKey, token.CacheKey);
        }

        #region Regional Tests
        [TestMethod]
        //RMA getting FMI cred for a leaf entity or sub-RMA
        [Ignore("Requires Coorp net to run")]
        public async Task Flow1_RmaCredential_From_CertTestRegionalAsync()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            string expectedExternalCacheKey = null;

            Action<TokenCacheNotificationArgs> extCacheKeyEvaluator = (args) =>
            {
                if (expectedExternalCacheKey != null)
                {
                    Assert.IsTrue(args.SuggestedCacheKey.Contains(expectedExternalCacheKey));
                }
            };

            //Fmi app/scenario parameters
            var clientId = "4df2cbbb-8612-49c1-87c8-f334d6d065ad";
            var scope = "api://AzureFMITokenExchange/.default";

            //Act
            //Create application
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority("https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca", true)
                        .WithExtraQueryParameters(_testslice2) //Enables MSAL to target ESTS Test slice
                        .WithCertificate(cert, sendX5C: true) //sendX5c enables SN+I auth which is required for FMI flows
                        .WithAzureRegion("westus3")
                        .BuildConcrete();

            //Configure token cache serialization
            confidentialApp.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
            confidentialApp.AppTokenCache.SetAfterAccess(AfterCacheAccess);

            //Recording test data for Asserts
            var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(extCacheKeyEvaluator);
            expectedExternalCacheKey = "zm2n0E62zwTsnNsozptLsoOoB_C7i-GfpxHYQQINJUw";

            //Acquire Fmi Cred
            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            //Assert
            AssertResults(authResult,
                          confidentialApp,
                          "-login.microsoftonline.com-atext-4df2cbbb-8612-49c1-87c8-f334d6d065ad-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-api://azurefmitokenexchange/.default-zm2n0e62zwtsnnsozptlsooob_c7i-gfpxhyqqinjuw",
                          "a9dd8a2a-df54-4ae0-84f9-38c8d57e5265",
                          "SomeFmiPath/FmiCredentialPath");
        }

        [TestMethod]
        //RMA getting FMI token for a leaf entity
        [Ignore("Requires Coorp net to run")]
        public async Task Flow2_RmaToken_From_CertTestRegionalAsync()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            string expectedExternalCacheKey = null;

            Action<TokenCacheNotificationArgs> extCacheKeyEvaluator = (args) =>
            {
                if (expectedExternalCacheKey != null)
                {
                    Assert.IsTrue(args.SuggestedCacheKey.Contains(expectedExternalCacheKey));
                }
            };

            //Fmi app/scenario parameters
            var clientId = "4df2cbbb-8612-49c1-87c8-f334d6d065ad";
            var scope = "3091264c-7afb-45d4-b527-39737ee86187/.default"; //Guid for api://AzureFMITokenExchange

            //Act
            //Create application
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority("https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca", true)
                        .WithExtraQueryParameters(_testslice2) //Enables MSAL to target ESTS Test slice
                        .WithCertificate(cert, sendX5C: true) //sendX5c enables SN+I auth which is required for FMI flows
                        .WithAzureRegion("westus3")
                        .BuildConcrete();

            //Configure token cache serialization
            confidentialApp.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
            confidentialApp.AppTokenCache.SetAfterAccess(AfterCacheAccess);

            //Recording test data for Asserts
            var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(extCacheKeyEvaluator);
            expectedExternalCacheKey = "zm2n0E62zwTsnNsozptLsoOoB_C7i-GfpxHYQQINJUw";

            //Acquire Token
            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath("SomeFmiPath/FmiCredentialPath") //Sets fmi path in client credential request.
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            //Assert
            AssertResults(authResult,
                          confidentialApp,
                          "-login.microsoftonline.com-atext-4df2cbbb-8612-49c1-87c8-f334d6d065ad-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-3091264c-7afb-45d4-b527-39737ee86187/.default-zm2n0e62zwtsnnsozptlsooob_c7i-gfpxhyqqinjuw",
                          "3091264c-7afb-45d4-b527-39737ee86187",
                          "SomeFmiPath/FmiCredentialPath");
        }

        [TestMethod]
        //Sub-RMA getting FMI cred for a child sub-RMA
        [Ignore("Requires Coorp net to run")]
        public async Task Flow3_FmiCredential_From_RmaCredentialRegional()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            string expectedExternalCacheKey = null;

            Action<TokenCacheNotificationArgs> extCacheKeyEvaluator = (args) =>
            {
                if (expectedExternalCacheKey != null)
                {
                    Assert.IsTrue(args.SuggestedCacheKey.Contains(expectedExternalCacheKey));
                }
            };

            //Fmi app/scenario parameters
            var clientId = "urn:microsoft:identity:fmi";
            var scope = "api://AzureFMITokenExchange/.default";

            //Act
            //Create application
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority("https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca", true)
                        .WithExtraQueryParameters(_testslice2) //Enables MSAL to target ESTS Test slice
                        .WithClientAssertion((options) => GetParentCredential(options, _testslice2)) //This api acquires the FMI credential needed to authenticate
                        .WithAzureRegion("westus3")
                        .BuildConcrete();

            //FOR TESTING ONLY: Configure token cache serialization
            confidentialApp.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
            confidentialApp.AppTokenCache.SetAfterAccess(AfterCacheAccess);

            //Recording test data for Asserts
            var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(extCacheKeyEvaluator);
            expectedExternalCacheKey = "7CX57Q63os7benQ6ER0sxgJPtNQSv7TGb5zexcidFoI";

            //Acquire Fmi Cred
            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath("SomeFmiPath/Path") //Sets fmi path in client credential request.
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            //Assert
            AssertResults(authResult,
                          confidentialApp,
                          "-login.microsoftonline.com-atext-urn:microsoft:identity:fmi-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-api://azurefmitokenexchange/.default-7cx57q63os7benq6er0sxgjptnqsv7tgb5zexcidfoi",
                          "a9dd8a2a-df54-4ae0-84f9-38c8d57e5265",
                          "SomeFmiPath/Path");
        }

        [TestMethod]
        //Sub-RMA getting FIC for leaf entity.
        [Ignore("Requires Coorp net to run")]
        public async Task Flow4_SubRma_FmiCredential_For_leafRegional()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            string expectedExternalCacheKey = null;

            Action<TokenCacheNotificationArgs> extCacheKeyEvaluator = (args) =>
            {
                if (expectedExternalCacheKey != null)
                {
                    Assert.IsTrue(args.SuggestedCacheKey.Contains(expectedExternalCacheKey));
                }
            };

            //Fmi app/scenario parameters
            var clientId = "urn:microsoft:identity:fmi";
            var scope = "3091264c-7afb-45d4-b527-39737ee86187/.default"; //Guid for api://AzureFMITokenExchange

            //Act
            //Create application
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority("https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca", true)
                        .WithExtraQueryParameters(_testslice2) //Enables MSAL to target ESTS Test slice
                        .WithClientAssertion((options) => GetParentCredential(options, _testslice2)) //This api acquires the FMI credential needed to authenticate
                        .WithAzureRegion("westus3")
                        .BuildConcrete();

            //Configure token cache serialization
            confidentialApp.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
            confidentialApp.AppTokenCache.SetAfterAccess(AfterCacheAccess);

            //Recording test data for Asserts
            var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(extCacheKeyEvaluator);
            expectedExternalCacheKey = "";

            //Acquire Fmi Cred
            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath("SomeFmiPath/Path") //Sets fmi path in client credential request.
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            //Assert
            AssertResults(authResult,
                          confidentialApp,
                          "-login.microsoftonline.com-atext-urn:microsoft:identity:fmi-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-3091264c-7afb-45d4-b527-39737ee86187/.default-7cx57q63os7benq6er0sxgjptnqsv7tgb5zexcidfoi",
                          "3091264c-7afb-45d4-b527-39737ee86187",
                          "SomeFmiPath/Path");
        }

        [TestMethod]
        //Sub-RMA getting FMI token for leaf entity
        [Ignore("Requires Coorp net to run")]
        public async Task Flow5_SubRma_FmiToken_From_FmiCred_For_leafTestRegionalAsync()
        {
            //Arrange
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            string expectedExternalCacheKey = null;

            Action<TokenCacheNotificationArgs> extCacheKeyEvaluator = (args) =>
            {
                if (expectedExternalCacheKey != null)
                {
                    Assert.IsTrue(args.SuggestedCacheKey.Contains(expectedExternalCacheKey));
                }
            };

            //Fmi app/scenario parameters
            var clientId = "urn:microsoft:identity:fmi";
            var scope = "3091264c-7afb-45d4-b527-39737ee86187/.default"; //Guid for api://AzureFMITokenExchange

            //Act
            //Create application
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority("https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca", true)
                        .WithExtraQueryParameters(_testslice2) //Enables MSAL to target ESTS Test slice
                        .WithClientAssertion((options) => GetParentCredential(options, _testslice2)) //This api acquires the FMI credential needed to authenticate
                        .WithAzureRegion("westus3")
                        .BuildConcrete();

            //Configure token cache serialization
            confidentialApp.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
            confidentialApp.AppTokenCache.SetAfterAccess(AfterCacheAccess);

            //Recording test data for Asserts
            var appCacheAccess = confidentialApp.AppTokenCache.RecordAccess(extCacheKeyEvaluator);
            expectedExternalCacheKey = "7CX57Q63os7benQ6ER0sxgJPtNQSv7TGb5zexcidFoI";

            //Acquire Fmi Cred
            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath("SomeFmiPath/Path") //Sets fmi path in client credential request.
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            //Assert
            AssertResults(authResult,
                          confidentialApp,
                          "-login.microsoftonline.com-atext-urn:microsoft:identity:fmi-f645ad92-e38d-4d1a-b510-d1b09a74a8ca-3091264c-7afb-45d4-b527-39737ee86187/.default-7cx57q63os7benq6er0sxgjptnqsv7tgb5zexcidfoi",
                          "3091264c-7afb-45d4-b527-39737ee86187",
                          "SomeFmiPath/Path");
        }
        #endregion
    }
}
