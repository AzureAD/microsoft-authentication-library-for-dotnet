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
    public class ServiceFabricTests
    {
        private const string Resource = "https://management.azure.com";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public async Task ServiceFabricInvalidEndpointAsync()
        {
            using(new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ManagedIdentityTests.SetEnvironmentVariables(ManagedIdentitySourceType.ServiceFabric, "localhost/token");

                IManagedIdentityApplication mi = ManagedIdentityApplicationBuilder.Create()
                    .WithHttpManager(httpManager)
                    .Build();

                MsalClientException ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.InvalidManagedIdentityEndpoint, ex.ErrorCode);
                Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityEndpointInvalidUriError, "IDENTITY_ENDPOINT", "localhost/token", "Service Fabric"), ex.Message);
            }
        }
    }
}
