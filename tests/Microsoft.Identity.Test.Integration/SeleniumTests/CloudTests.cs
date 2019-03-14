//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.LabInfrastructure.CloudInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Integration.SeleniumTests
{
    [TestClass]
    public class CloudTests
    {
        #region MSTest Hooks
        /// <summary>
        /// Initialized by MSTest (do not make private or readonly)
        /// </summary>
        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();
            LabUserHelper.UseCache = false;
        }

        #endregion

        #region Interactive Tests
        [DataTestMethod]
        [DataRow("FairFaxCloud")]
        [DataRow("GermanyCloud")]
        [DataRow("ChinaCloud")]
        [TestCategory("Cloud")]
        public async Task Cloud_Interactive_AADAsync(string value)
        {
            // Arrange
            CloudConfigurationProvider.LoadConfiguration(value);
            LabResponse labResponse = LabUserHelper.GetDefaultUser();
            await SeleniumTestHelper.RunInteractiveTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow("FairFaxCloud")]
        [DataRow("GermanyCloud")]
        [DataRow("ChinaCloud")]
        [TestCategory("Cloud")]
        public async Task Cloud_Interactive_AdfsV3_NotFederatedAsync(string value)
        {
            // Arrange
            CloudConfigurationProvider.LoadConfiguration(value);
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV3,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = false
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await SeleniumTestHelper.RunInteractiveTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow("GermanyCloud")]
        [DataRow("ChinaCloud")]
        [TestCategory("Cloud")]
        public async Task Cloud_Interactive_AdfsV3_FederatedAsync(string value)
        {
            // Arrange
            CloudConfigurationProvider.LoadConfiguration(value);
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV3,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await SeleniumTestHelper.RunInteractiveTestForUserAsync(labResponse).ConfigureAwait(false);
        }
        #endregion Interactive Tests

        #region Authority Tests
        [DataTestMethod]
        [DataRow("FairFaxCloud")]
        [TestCategory("Cloud")]
        public async Task Cloud_AuthorityMigrationAsync(string value)
        {
            CloudConfigurationProvider.LoadConfiguration(value);
            await SeleniumTestHelper.AuthorityMigrationTestAsync().ConfigureAwait(false);
        }
        #endregion Authority Tests

#if !WINDOWS_APP && !ANDROID && !iOS // U/P not available on UWP, Android and iOS
        #region Username Password Integration Tests
        [DataTestMethod]
        [DataRow("FairFaxCloud")]
        [TestCategory("Cloud")]
        public async Task Cloud_ROPC_AAD_Async(string value)
        {
            CloudConfigurationProvider.LoadConfiguration(value);
            var labResponse = LabUserHelper.GetDefaultUser();
            await SeleniumTestHelper.RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow("FairFaxCloud")]
        [TestCategory("Cloud")]
        public async Task Cloud_ROPC_ADFSv3Federated_Async(string value)
        {
            CloudConfigurationProvider.LoadConfiguration(value);
            var labResponse = LabUserHelper.GetAdfsUser(FederationProvider.AdfsV3, true);
            await SeleniumTestHelper.RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow("FairFaxCloud")]
        [TestCategory("Cloud")]
        public async Task Cloud_ROPC_ADFSv3Managed_Async(string value)
        {
            CloudConfigurationProvider.LoadConfiguration(value);
            var labResponse = LabUserHelper.GetAdfsUser(FederationProvider.AdfsV3, false);
            await SeleniumTestHelper.RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow("FairFaxCloud")]
        [TestCategory("Cloud")]
        public async Task Cloud_AcquireTokenWithManagedUsernameIncorrectPasswordAsync(string value)
        {
            CloudConfigurationProvider.LoadConfiguration(value);
            await SeleniumTestHelper.AcquireTokenWithManagedUsernameIncorrectPasswordTestAsync().ConfigureAwait(false);
        }
        #endregion Username Password Integration Tests
#endif

        #region Silent Tests
        [DataTestMethod]
        [DataRow("FairFaxCloud")]
        [TestCategory("Cloud")]
        public async Task Cloud_SilentAuth_ForceRefresh_Async(string value)
        {
            CloudConfigurationProvider.LoadConfiguration(value);
            await SeleniumTestHelper.SilentAuth_ForceRefresh_Async().ConfigureAwait(false);
        }
        #endregion Silent Tests
    }
}
