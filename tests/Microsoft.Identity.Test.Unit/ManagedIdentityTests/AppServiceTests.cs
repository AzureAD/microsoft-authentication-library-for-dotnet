﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
    public class AppServiceTests : TestBase
    {
        private const string AppService = "App Service";
        internal const string AppServiceEndpoint = "http://127.0.0.1:41564/msi/token";
        internal const string MachineLearningEndpoint = "http://localhost:7071/msi/token";

        [TestMethod]
        public async Task AppServiceInvalidEndpointAsync()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                SetEnvironmentVariables(ManagedIdentitySource.AppService, "127.0.0.1:41564/msi/token");

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.InvalidManagedIdentityEndpoint, ex.ErrorCode);
                Assert.AreEqual(ManagedIdentitySource.AppService.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityEndpointInvalidUriError, "IDENTITY_ENDPOINT", "127.0.0.1:41564/msi/token", AppService), ex.Message);
            }
        }

        // Regression test for Bug ID #5077 - ManagedIdentityCredential authentication failed 
        [DataTestMethod]
        [DataRow("http://127.0.0.1:41564/msi/token/", ManagedIdentitySource.AppService, ManagedIdentitySource.AppService)]
        [DataRow(AppServiceEndpoint, ManagedIdentitySource.AppService, ManagedIdentitySource.AppService)]
        [DataRow(MachineLearningEndpoint, ManagedIdentitySource.MachineLearning, ManagedIdentitySource.MachineLearning)]
        public void TestAppServiceUpgradeScenario(
            string endpoint,
            ManagedIdentitySource managedIdentitySource,
            ManagedIdentitySource expectedManagedIdentitySource)
        {
            using (new EnvVariableContext())
            {
                SetUpgradeScenarioEnvironmentVariables(managedIdentitySource, endpoint);

                Assert.AreEqual(expectedManagedIdentitySource, ManagedIdentityApplication.GetManagedIdentitySource());
            }
        }
    }
}
