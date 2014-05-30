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
using Windows.Storage;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Test.ADAL.Common;
using Logger = Microsoft.IdentityModel.Clients.ActiveDirectory.Logger;

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
        //[Description("Test for ADAL Id")]
        public async Task IncludeFormsAuthParamsTest()
        {
            await OAuth2Request.IncludeFormsAuthParams();
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
    }
}
