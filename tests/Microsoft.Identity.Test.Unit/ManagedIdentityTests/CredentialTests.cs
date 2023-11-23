// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    /// <summary>
    /// Unit tests for the Credential component in the Managed Identity scenario.
    /// </summary>
    [TestClass]
    public class CredentialTests : TestBase
    {
        internal const string CredentialEndpoint = "http://169.254.169.254/metadata/identity/credential";
        internal const string MtlsEndpoint = "https://centraluseuap.mtlsauth.microsoft.com/" +
            "72f988bf-86f1-41af-91ab-2d7cd011db47/oauth2/v2.0/token";
        internal const string AzureResource = "http://vault.azure.net";

        /// <summary>
        /// Tests the happy path for acquiring credentials with various CryptoKeyTypes.
        /// </summary>
        /// <param name="cryptoKeyType">The type of cryptographic key used.</param>
        [DataTestMethod]
        [DataRow(CryptoKeyType.Machine)]
        [DataRow(CryptoKeyType.User)]
        [DataRow(CryptoKeyType.InMemory)]
        [DataRow(CryptoKeyType.Ephemeral)]
        [DataRow(CryptoKeyType.KeyGuard)]
        public async Task CredentialHappyPathAsync(CryptoKeyType cryptoKeyType)
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), cryptoKeyType);

                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    sendHeaders: true,
                    MockHelpers.GetSuccessfulCredentialResponse());          

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    sendHeaders: false,
                    MockHelpers.GetSuccessfulMtlsResponse());

                // Act
                // We should get the auth result from the token provider
                AuthenticationResult result = await mi.AcquireTokenForManagedIdentity(AzureResource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                
                // Act
                // We should get the auth result from the cache
                result = await mi.AcquireTokenForManagedIdentity(AzureResource)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }
    }
}
