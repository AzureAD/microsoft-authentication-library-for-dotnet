// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
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
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))

            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, "http://169.254.169.254");

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddManagedIdentityMockHandler(ManagedIdentityTests.ImdsEndpoint, ManagedIdentityTests.Resource, MockHelpers.GetMsiImdsErrorResponse(),
                    ManagedIdentitySource.Imds, statusCode: HttpStatusCode.BadRequest);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySource.Imds.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.IsTrue(ex.Message.Contains(ImdsManagedIdentitySource.IdentityUnavailableError));
            }
        }
    }
}
