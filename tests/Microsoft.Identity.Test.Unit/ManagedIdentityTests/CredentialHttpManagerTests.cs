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
    /// Unit tests for the Credential component in the Managed Identity scenario specific to Http Client customizations.
    /// </summary>
    [TestClass]
    public class CredentialHttpManagerTests : TestBase
    {
        internal const string CredentialEndpoint = "http://169.254.169.254/metadata/identity/credential";
        internal const string MtlsEndpoint = "https://centraluseuap.mtlsauth.microsoft.com/" +
            "72f988bf-86f1-41af-91ab-2d7cd011db47/oauth2/v2.0/token";

        /// <summary>
        /// Test the Public APIs for Credential endpoint support for Http Client Customizations.
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

                Assert.IsTrue(mi.GetBindingCertificate().Thumbprint == CertHelper.GetOrCreateTestCert().Thumbprint);
            }
        }

        /// <summary>
        /// Tests the Claims on MI.
        /// </summary>
        [TestMethod]
        public async Task CredentialWithWrongHttpClientCustomizationAsync()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true, invokeNonMtlsHttpManagerFactory: true))
            {
                ManagedIdentityApplicationBuilder miBuilder = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                KeyMaterialManagerMock keyManagerMock = new(CertHelper.GetOrCreateTestCert(), CryptoKeyType.User);
                miBuilder.Config.KeyMaterialManagerForTest = keyManagerMock;

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    sendHeaders: true,
                    MockHelpers.GetSuccessfulCredentialResponse());

                // Act
                // Test for MsalClientException
                MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource) 
                    .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.IsNotNull(ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.CredentialHttpCustomizationError, ex.Message); 
            }
        }
    }
}
