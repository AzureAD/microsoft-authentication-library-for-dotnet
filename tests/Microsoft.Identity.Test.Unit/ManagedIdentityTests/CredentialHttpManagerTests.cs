// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.Extensions;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    /// <summary>
    /// Unit tests for the Credential component in the Managed Identity scenario specific to Http Clients.
    /// </summary>
    [TestClass]
    public class CredentialHttpManagerTests : TestBase
    {
        internal const string CredentialEndpoint = "http://169.254.169.254/metadata/identity/credential";
        internal const string MtlsEndpoint = "https://centraluseuap.mtlsauth.microsoft.com/" +
            "72f988bf-86f1-41af-91ab-2d7cd011db47/oauth2/v2.0/token";
        internal const string Resource = "https://management.azure.com";
        internal const string ImdsEndpoint = "http://169.254.169.254/metadata/identity/oauth2/token";

        /// <summary>
        /// Test the Public APIs for Credential endpoint support for http client customizations.
        /// </summary>
        [DataTestMethod]
        [DataRow(CryptoKeyType.Machine)]
        [DataRow(CryptoKeyType.User)]
        [DataRow(CryptoKeyType.InMemory)]
        [DataRow(CryptoKeyType.Ephemeral)]
        [DataRow(CryptoKeyType.KeyGuard)]
        public void CredentialPublicApi(int keyType)
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                CryptoKeyType cryptoKeyType = (CryptoKeyType)keyType;

                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), cryptoKeyType);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                Assert.IsTrue(mi.IsClaimsSupportedByClient() == true);
                
                // Check IsProofOfPossessionSupportedByClient only if CryptoKeyType is KeyGuard
                if (cryptoKeyType == CryptoKeyType.KeyGuard)
                {
                    Assert.IsTrue(mi.IsProofOfPossessionSupportedByClient() == true);
                }
                else
                {
                    Assert.IsTrue(mi.IsProofOfPossessionSupportedByClient() == false);
                }
            }
        }

        [TestMethod]
        public void TestConstructor_WithHttpClientFactory()
        {
            var httpClientFactory = Substitute.For<IMsalHttpClientFactory>();
            var mia = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                                                                .WithHttpClientFactory(httpClientFactory);
                                                                
            Assert.AreEqual(httpClientFactory, mia.Config.HttpClientFactory);
        }

        [TestMethod]
        public void TestConstructor_WithMtlsHttpClientFactory()
        {
            var httpClientFactory = Substitute.For<IMsalMtlsHttpClientFactory>();
            var mia = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                                                                 .WithHttpClientFactory(httpClientFactory);

            Assert.AreEqual(httpClientFactory, mia.Config.HttpClientFactory);
        }

        /// <summary>
        /// Tests MI customized with IMsalHttpClientFactory returns Mtls token.
        /// </summary>
        [TestMethod]
        public async Task WithHttpClientCustomizationReturnsMtlsTokenAsync()
        {
            // Arrange
            using MockHttpClientFactoryForTest mockHttpClient = new MockHttpClientFactoryForTest();
            using (mockHttpClient.AddMockHandler(MockHttpCreator.CreateMtlsCredentialHandler(CertHelper.GetOrCreateTestCert())))
            using (mockHttpClient.AddMockHandler(MockHttpCreator.CreateMtlsTokenHandler()))
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpClientFactory(mockHttpClient);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.User);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Act
                var result = await mi.AcquireTokenForManagedIdentity(Resource).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        /// <summary>
        /// Tests MI customized with IMsalHttpClientFactory returns IMDS token.
        /// </summary>
        [TestMethod]
        public async Task WithMtlsHttpClientCustomizationReturnsMtlsTokenAsync()
        {
            // Arrange
            using MockMtlsHttpClientFactory mockHttpClient = new MockMtlsHttpClientFactory();
            using (mockHttpClient.AddMockHandler(MockHttpCreator.CreateMtlsCredentialHandler(CertHelper.GetOrCreateTestCert())))
            using (mockHttpClient.AddMockHandler(MockHttpCreator.CreateMtlsCredentialHandler(CertHelper.GetOrCreateTestCert())))
            using (mockHttpClient.AddMockHandler(MockHttpCreator.CreateMtlsTokenHandler()))
            using (mockHttpClient.AddMockHandler(MockHttpCreator.CreateMtlsTokenHandler()))
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpClientFactory(mockHttpClient as IMsalMtlsHttpClientFactory);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.User);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                // Act
                var result = await mi.AcquireTokenForManagedIdentity(Resource).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }
    }
}
