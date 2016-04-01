//----------------------------------------------------------------------
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

using System;
using System.Diagnostics.Tracing;
using System.Net;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Test.ADAL.Common;
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
        public void AdalTraceTest()
        {
            Verify.IsFalse(AdalOption.AdalEventSource.IsEnabled());
            var eventListener = new SampleEventListener();
            eventListener.EnableEvents(AdalOption.AdalEventSource, EventLevel.Verbose);
            Verify.IsNullOrEmptyString(eventListener.TraceBuffer);
            Verify.IsTrue(AdalOption.AdalEventSource.IsEnabled());
            AuthenticationContext context = new AuthenticationContext("https://login.windows.net/commmon");
            Verify.IsNotNullOrEmptyString(eventListener.TraceBuffer);
            eventListener.DisableEvents(AdalOption.AdalEventSource);
            Verify.IsFalse(AdalOption.AdalEventSource.IsEnabled());
        }

        [TestMethod]
        [TestCategory("AdalWinRTUnit")]
        public async Task MsAppRedirectUriTest()
        {
            Sts sts = new AadSts();
            AuthenticationContextProxy context = new AuthenticationContextProxy(sts.Authority);
            AuthenticationResultProxy result = await context.AcquireTokenAsync(sts.ValidResource, sts.ValidClientId,
                new Uri("ms-app://s-1-15-2-2097830667-3131301884-2920402518-3338703368-1480782779-4157212157-3811015497/"), 
                new PlatformParameters(PromptBehavior.Auto, false));

            Verify.IsNotNullOrEmptyString(result.Error);
            Verify.AreEqual(result.Error, Sts.AuthenticationUiFailedError);

            Uri uri = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
            result = await context.AcquireTokenAsync(sts.ValidResource, sts.ValidClientId, uri, new PlatformParameters(PromptBehavior.Auto, false));

            Verify.IsNotNullOrEmptyString(result.Error);
            Verify.AreEqual(result.Error, Sts.AuthenticationUiFailedError);
        }

        [TestMethod]
        [TestCategory("AdalWinRTUnit")]
        [Ignore]    // This test requires TestService to run in a non-WinRT app. The code can be found in tests\Test.ADAL.NET.Unit\UnitTests.cs
        public async Task TimeoutTest()
        {
            const string TestServiceUrl = "http://localhost:8080";
            HttpClientWrapper webRequest = new HttpClientWrapper(TestServiceUrl + "?delay=0&response_code=200", null) { TimeoutInMilliSeconds = 10000 };
            await webRequest.GetResponseAsync();

            try
            {
                webRequest = new HttpClientWrapper(TestServiceUrl + "?delay=0&response_code=400", null) { TimeoutInMilliSeconds = 10000 };
                await webRequest.GetResponseAsync();
            }
            catch (WebException ex)
            {
                Verify.AreEqual((int)(ex.Status), 7);   // ProtocolError
            }

            try
            {
                webRequest = new HttpClientWrapper(TestServiceUrl + "?delay=10000&response_code=200", null) { TimeoutInMilliSeconds = 500 };
                await webRequest.GetResponseAsync();
            }
            catch (WebException ex)
            {
                Verify.AreEqual((int)(ex.Status), 6);   // RequestCanceled
            }
        }

        class SampleEventListener : EventListener
        {
            public string TraceBuffer { get; set; }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                TraceBuffer += (eventData.Payload[0] + "\n");
            }
        }
    }
}
