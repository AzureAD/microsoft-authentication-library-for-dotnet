// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{

    [TestClass]
    public class AppServiceTests
    {
        private const string AppService = "App Service";

        [TestMethod]
        public async Task AppServiceInvalidEndpointAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ManagedIdentityTests.SetEnvironmentVariables(ManagedIdentitySourceType.AppService, "127.0.0.1:41564/msi/token");

                IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder.Create()
                    .WithHttpManager(httpManager)
                    .Build();

                MsalClientException ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.InvalidManagedIdentityEndpoint, ex.ErrorCode);
                Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityEndpointInvalidUriError, "IDENTITY_ENDPOINT", "127.0.0.1:41564/msi/token", AppService), ex.Message);
            }
        }
    }
}
