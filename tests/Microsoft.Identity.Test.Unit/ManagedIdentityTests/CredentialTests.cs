// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class CredentialTests : TestBase
    {
        //[TestMethod]
        public async Task CredentialEndpointBadRequestTestAsync()
        {
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithExperimentalFeatures()
                    .WithClientCapabilities(new[] { "cp1" }) //calling this api in MI will trigger the cred flow
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityCredentialMockHandler(ManagedIdentityTests.CredentialEndpoint);

                MsalManagedIdentityException ex = await Assert.ThrowsExceptionAsync<MsalManagedIdentityException>(async () =>
                    await mi.AcquireTokenForManagedIdentity("scope")
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySource.Credential, ex.ManagedIdentitySource);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.ManagedIdentityNoResponseReceived, ex.Message);
            }
        }
    }
}
