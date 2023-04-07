// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class ImdsTests : TestBase
    {
        [TestMethod]
        public async Task ImdsBadRequestTestAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())

            {
                SetEnvironmentVariables(ManagedIdentitySourceType.IMDS, "http://169.254.169.254");

                IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder.Create()
                    .WithExperimentalFeatures()
                    .WithHttpManager(httpManager)
                    .Build();

                httpManager.AddManagedIdentityMockHandler(ManagedIdentityTests.ImdsEndpoint, ManagedIdentityTests.Resource, MockHelpers.GetMsiImdsErrorResponse(),
                    ManagedIdentitySourceType.IMDS, statusCode: HttpStatusCode.BadRequest);

                MsalManagedIdentityException ex = await Assert.ThrowsExceptionAsync<MsalManagedIdentityException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySourceType.IMDS, ex.ManagedIdentitySource);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.IsTrue(ex.Message.Contains("The requested identity has not been assigned to this resource."));
            }
        }
    }
}
