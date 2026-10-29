// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if MSAL_TEST_NETSTANDARD20
using System;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class NetStandard20ManagedIdentityTests : TestBase
    {
        [TestMethod]
        public void LoadsNetStandard20Asset()
        {
            // Arrange
            var assembly = typeof(ManagedIdentityApplicationBuilder).Assembly;

            // Act
            string framework = assembly.GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName;

            // Assert
            Assert.AreEqual(".NETStandard,Version=v2.0", framework);
        }

        [RunOnWindows]
        [DataRow(false, false, false)]
        [DataRow(false, false, true)]
        [DataRow(false, true, false)]
        [DataRow(true, false, false)]
        [DataRow(true, false, true)]
        [DataRow(true, true, false)]
        public void MtlsRequest_ThrowsUnsupportedBeforeAcquisition(
            bool userAssigned, bool requestOverMtls, bool explicitOptions)
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                var identity = userAssigned
                    ? ManagedIdentityId.WithUserAssignedClientId(TestConstants.ClientId)
                    : ManagedIdentityId.SystemAssigned;
                var request = ManagedIdentityApplicationBuilder.Create(identity)
                    .WithHttpManager(httpManager)
                    .Build()
                    .AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource);

                // Act
                var exception = Assert.ThrowsExactly<MsalClientException>(() =>
                {
                    if (requestOverMtls)
                    {
                        request.WithRequestOverMtls();
                    }
                    else if (explicitOptions)
                    {
                        request.WithMtlsProofOfPossession(new PoPOptions());
                    }
                    else
                    {
                        request.WithMtlsProofOfPossession();
                    }
                });

                // Assert
                Assert.AreEqual(MsalError.MtlsNotSupportedForManagedIdentity, exception.ErrorCode);
                Assert.Contains(".NET Standard 2.0", exception.Message);
                Assert.IsFalse(request.CommonParameters.IsMtlsPopRequested);
                Assert.IsFalse(request.CommonParameters.PreferMsiV2);
            }
        }

        [TestMethod]
        public async Task Discovery_SkipsImdsV2AndReportsNoMtlsSupportAsync()
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetImdsEnvironmentVariables();
                var application = (ManagedIdentityApplication)ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .Build();
                httpManager.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V1));

                // Act
                var capabilities = await application.GetManagedIdentityCapabilitiesAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual(ManagedIdentitySource.Imds, capabilities.Source);
                Assert.AreEqual(MtlsBindingStrength.None, capabilities.MaxSupportedBindingStrength);
                Assert.IsFalse(capabilities.IsMtlsPopSupportedByHost);
                Assert.AreEqual(0, httpManager.QueueSize);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task BearerRequest_StillAcquiresAndCachesTokenAsync(bool userAssigned)
        {
            // Arrange
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetImdsEnvironmentVariables();
                var identity = userAssigned
                    ? ManagedIdentityId.WithUserAssignedClientId(TestConstants.ClientId)
                    : ManagedIdentityId.SystemAssigned;
                var application = ManagedIdentityApplicationBuilder.Create(identity)
                    .WithHttpManager(httpManager)
                    .Build();
                httpManager.AddManagedIdentityMockHandler(
                    ManagedIdentityTests.ImdsEndpoint,
                    ManagedIdentityTests.Resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.Imds,
                    userAssignedId: userAssigned ? TestConstants.ClientId : null,
                    userAssignedIdentityId: userAssigned ? UserAssignedIdentityId.ClientId : UserAssignedIdentityId.None);

                // Act
                var result = await application.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual("Bearer", result.TokenType);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(0, httpManager.QueueSize);
                var cached = await application.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            }
        }

        private static void SetImdsEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", null);
            Environment.SetEnvironmentVariable("IDENTITY_HEADER", null);
            Environment.SetEnvironmentVariable("IDENTITY_SERVER_THUMBPRINT", null);
            Environment.SetEnvironmentVariable("IMDS_ENDPOINT", null);
            Environment.SetEnvironmentVariable("MSI_ENDPOINT", null);
            Environment.SetEnvironmentVariable("MSI_SECRET", null);
            Environment.SetEnvironmentVariable("DEFAULT_IDENTITY_CLIENT_ID", null);
            SetEnvironmentVariables(ManagedIdentitySource.Imds, ManagedIdentityTests.ImdsEndpoint);
        }
    }
}
#endif
