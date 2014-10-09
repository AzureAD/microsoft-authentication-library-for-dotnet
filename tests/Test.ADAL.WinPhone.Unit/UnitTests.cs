//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Threading.Tasks;

using Windows.Security.Authentication.Web;
using Windows.Storage;

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

using Test.ADAL.Common;

namespace Test.ADAL.WinPhone.Unit
{
    [TestClass]
    public class UnitTests
    {

        [TestMethod]
        [TestCategory("AdalWinPhoneUnit")]
        public async Task MsAppRedirectUriTest()
        {
            Sts sts = new AadSts();
            AuthenticationContextProxy context = new AuthenticationContextProxy(sts.Authority);

            AuthenticationResultProxy result = null;

            result = await context.AcquireTokenAsync(sts.ValidResource, sts.ValidClientId, new Uri("ms-app://test/"), null);

            Verify.IsNotNullOrEmptyString(result.Error);
            Verify.AreEqual(result.Error, Sts.AuthenticationUiFailedError);            

            try
            {
                WebAuthenticationBroker.GetCurrentApplicationCallbackUri();

                Verify.Fail("Exception expected");
            }
            catch (Exception ex)
            {
                Verify.IsTrue(ex.Message.Contains("hostname"));
            }

            result = await context.AcquireTokenAsync(sts.ValidResource, sts.ValidClientId, null, null);
            Verify.AreEqual(result.Error, "need_to_set_callback_uri_as_local_setting");

            // Incorrect ms-app
            ApplicationData.Current.LocalSettings.Values["CurrentApplicationCallbackUri"] = "ms-app://s-1-15-2-2097830667-3131301884-2920402518-3338703368-1480782779-4157212157-3811015497/";
            result = await context.AcquireTokenAsync(sts.ValidResource, sts.ValidClientId, null, null);
            Verify.AreEqual(result.Error, Sts.AuthenticationUiFailedError);
        }
    }
}
