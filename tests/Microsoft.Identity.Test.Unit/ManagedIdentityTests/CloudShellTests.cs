// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class CloudShellTests : TestBase
    {
        private const string CloudShell = "Cloud Shell";

        [DataTestMethod]
        [DataRow(TestConstants.ClientId, UserAssignedIdentityId.ClientId)]
        [DataRow("resourceId", UserAssignedIdentityId.ResourceId)]
        public async Task CloudShellUserAssignedManagedIdentityNotSupportedAsync(string userAssignedClientId, UserAssignedIdentityId userAssignedIdentityId)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySourceType.CloudShell, ManagedIdentityTests.CloudShellEndpoint);

                IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder.Create(userAssignedClientId)
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .Build();

                MsalManagedIdentityException ex = await Assert.ThrowsExceptionAsync<MsalManagedIdentityException>(async () =>
                    await mi.AcquireTokenForManagedIdentity("scope")
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySourceType.CloudShell, ex.ManagedIdentitySource);
                Assert.AreEqual(MsalError.UserAssignedManagedIdentityNotSupported, ex.ErrorCode);
                Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityUserAssignedNotSupported, CloudShell), ex.Message);
            }
        }

        [TestMethod]
        public async Task CloudShellInvalidEndpointAsync()
        {
            using(new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySourceType.CloudShell, "localhost/token");

                IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder.Create()
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .Build();

                MsalManagedIdentityException ex = await Assert.ThrowsExceptionAsync<MsalManagedIdentityException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySourceType.CloudShell, ex.ManagedIdentitySource);
                Assert.AreEqual(MsalError.InvalidManagedIdentityEndpoint, ex.ErrorCode);
            }
        }
    }
}
