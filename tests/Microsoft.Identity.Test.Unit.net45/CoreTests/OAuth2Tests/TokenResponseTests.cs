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

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.OAuth2Tests
{
    [TestClass]
    public class TokenResponseTests
    {
        [TestMethod]
        [TestCategory("TokenResponseTests")]
        public void ExpirationTimeTest()
        {
            // Need to get timestamp here since it needs to be before we create the token.
            // ExpireOn time is calculated from UtcNow when the object is created.
            DateTimeOffset current = DateTimeOffset.UtcNow;
            const long ExpiresInSeconds = 3599;

            var response = MsalTestConstants.CreateMsalTokenResponse();
            
            Assert.IsTrue(response.AccessTokenExpiresOn.Subtract(current) >= TimeSpan.FromSeconds(ExpiresInSeconds));
        }

        [TestMethod]
        [TestCategory("TokenResponseTests")]
        public void JsonDeserializationTest()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.AuthorityCommonTenant);

                OAuth2Client client = new OAuth2Client(harness.ServiceBundle.DefaultLogger, harness.HttpManager, new TelemetryManager(
                    harness.ServiceBundle.Config,
                    harness.ServiceBundle.PlatformProxy,
                    null));

                Task<MsalTokenResponse> task = client.GetTokenAsync(
                    new Uri(MsalTestConstants.AuthorityCommonTenant + "oauth2/v2.0/token"),
                    RequestContext.CreateForTest(harness.ServiceBundle));
                MsalTokenResponse response = task.Result;
                Assert.IsNotNull(response);
            }
        }
    }
}
