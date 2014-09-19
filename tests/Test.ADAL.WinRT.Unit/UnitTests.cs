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
using System.Net;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Test.ADAL.Common;
using Logger = Microsoft.IdentityModel.Clients.ActiveDirectory.Logger;
using Windows.Security.Authentication.Web;

namespace Test.ADAL.WinRT.Unit
{
    [TestClass]
    public partial class UnitTests
    {
        [TestMethod]
        [TestCategory("AdalWinRTUnit")]
        //[Description("Test for CreateSha256Hash method in PlatformSpecificHelper")]
        public void CreateSha256HashTest()
        {
            CommonUnitTests.CreateSha256HashTest();
        }

        [TestMethod]
        [TestCategory("AdalWinRTUnit")]
        //[Description("Test for ADAL Id")]
        public void AdalIdTest()
        {
            CommonUnitTests.AdalIdTest();
        }
        
        [TestMethod]
        [TestCategory("AdalWinRTUnit")]
        //[Description("Test to verify forms auth parameters.")]
        public async Task IncludeFormsAuthParamsTest()
        {
            Verify.IsFalse(await AcquireTokenInteractiveHandler.IncludeFormsAuthParamsAsync(null));
        }


        [TestMethod]
        [TestCategory("AdalWinRTUnit")]
        public void AdalTraceTest()
        {
            Verify.IsTrue(AdalTrace.Level == AdalTraceLevel.None);
        }

        [TestMethod]
        [TestCategory("AdalWinRTUnit")]
        [Ignore]    // TODO: The test is currently failing. 
        public async Task LoggerTest()
        {
            for (int i = 0; i < 2; i++)
            {
                AdalTrace.Level = AdalTraceLevel.Informational;
                string guidValue = Guid.NewGuid().ToString();
                Logger.Information(null, "{0}", guidValue);
                StorageFolder sf = ApplicationData.Current.LocalFolder;
                AdalTrace.Level = AdalTraceLevel.None;
                StorageFile file = await sf.GetFileAsync("AdalTraces.log");
                try
                {
                    string content = await FileIO.ReadTextAsync(file);
                    Log.Comment(content);
                    Verify.IsTrue(content.Contains(guidValue));
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        [TestMethod]
        [TestCategory("AdalWinRTUnit")]
        public async Task MsAppRedirectUriTest()
        {
            Sts sts = new AadSts();
            AuthenticationContext context = new AuthenticationContext(sts.Authority);
            AuthenticationResult result = await context.AcquireTokenAsync(sts.ValidResource, sts.ValidClientId,
                new Uri("ms-app://s-1-15-2-2097830667-3131301884-2920402518-3338703368-1480782779-4157212157-3811015497/"));

            Verify.IsNotNullOrEmptyString(result.Error);
            Verify.AreEqual(result.Error, Sts.AuthenticationUiFailedError);

            Uri uri = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
            result = await context.AcquireTokenAsync(sts.ValidResource, sts.ValidClientId, uri);

            Verify.IsNotNullOrEmptyString(result.Error);
            Verify.AreEqual(result.Error, Sts.AuthenticationUiFailedError);
        }

        [TestMethod]
        [TestCategory("AdalWinRTUnit")]
        [Ignore]    // This test requires TestService to run in a non-WinRT app. The code can be found in tests\Test.ADAL.NET.Unit\UnitTests.cs
        public async Task TimeoutTest()
        {
            const string TestServiceUrl = "http://localhost:8080";
            HttpWebRequestWrapper webRequest = new HttpWebRequestWrapper(TestServiceUrl + "?delay=0&response_code=200") { TimeoutInMilliSeconds = 10000 };
            await webRequest.GetResponseSyncOrAsync(new CallState(Guid.NewGuid(), false));  // Asynchronous

            try
            {
                webRequest = new HttpWebRequestWrapper(TestServiceUrl + "?delay=0&response_code=400") { TimeoutInMilliSeconds = 10000 };
                await webRequest.GetResponseSyncOrAsync(new CallState(Guid.NewGuid(), false));
            }
            catch (WebException ex)
            {
                Verify.AreEqual((int)(ex.Status), 7);   // ProtocolError
            }

            try
            {
                webRequest = new HttpWebRequestWrapper(TestServiceUrl + "?delay=10000&response_code=200") { TimeoutInMilliSeconds = 500 };
                await webRequest.GetResponseSyncOrAsync(new CallState(Guid.NewGuid(), false));  // Asynchronous
            }
            catch (WebException ex)
            {
                Verify.AreEqual((int)(ex.Status), 6);   // RequestCanceled
            }
        }
    }
}
