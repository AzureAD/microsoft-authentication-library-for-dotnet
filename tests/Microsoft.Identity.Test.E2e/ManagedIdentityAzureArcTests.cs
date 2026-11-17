// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.E2E
{
    [TestClass]
    public class ManagedIdentityAzureArcTests
    {
        private const string ArmScope = "https://management.azure.com";

        private static IManagedIdentityApplication BuildSami()
        {
            var builder = ManagedIdentityApplicationBuilder
                            .Create(ManagedIdentityId.SystemAssigned);

            builder.Config.AccessorOptions = null;

            return builder.Build();
        }

        [TestCategory("MI_E2E_AzureArc")]
        [RunOnAzureDevOps]
        [TestMethod]
        public async Task AcquireToken_ForSami_OnAzureArc_Succeeds()
        {
            var mi = BuildSami();
            var result = await mi.AcquireTokenForManagedIdentity(ArmScope)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken));
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

            var second = await mi.AcquireTokenForManagedIdentity(ArmScope).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(TokenSource.Cache, second.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(result.AccessToken, second.AccessToken, "Expected identical AT from cache.");
        }

        private static IManagedIdentityApplication BuildUami(string clientId)
        {
            var builder = ManagedIdentityApplicationBuilder
                            .Create(ManagedIdentityId.WithUserAssignedClientId(clientId));

            builder.Config.AccessorOptions = null;

            return builder.Build();
        }

        [TestCategory("MI_E2E_AzureArc")]
        [RunOnAzureDevOps]
        [TestMethod]
        public async Task AcquireToken_ForNonExistentUami_OnAzureArc_Fails()
        {
            // A user-assigned identity that is not assigned to this Arc-enabled machine.
            // A UAMI-capable Arc agent returns HTTP 404 (identity_not_found), which MSAL surfaces as a
            // service error. The agent must never silently return the system-assigned identity.
            var mi = BuildUami("00000000-0000-0000-0000-000000000001");

            MsalServiceException ex = await Assert.ThrowsAsync<MsalServiceException>(async () =>
                await mi.AcquireTokenForManagedIdentity(ArmScope)
                    .ExecuteAsync()
                    .ConfigureAwait(false)).ConfigureAwait(false);

            Assert.IsNotNull(ex);
            Assert.AreEqual(ManagedIdentitySource.AzureArc.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
            Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
            Assert.IsTrue(
                ex.Message.IndexOf("identity_not_found", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                ex.Message.IndexOf("Identity not found", System.StringComparison.OrdinalIgnoreCase) >= 0,
                $"Expected an 'identity not found' error from HIMDS for an unassigned UAMI. Actual: {ex.Message}");
        }
    }
}
