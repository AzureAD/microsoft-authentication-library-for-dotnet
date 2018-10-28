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
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Microsoft.Identity.Core.Unit;
using Test.Microsoft.Identity.Core.Unit.Mocks;

namespace Test.Microsoft.Identity.Unit.OAuth2Tests
{
    [TestClass]
    public class TokenResponseTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            CoreTelemetryService.InitializeCoreTelemetryService(new TestTelemetry());
        }

        [TestMethod]
        [TestCategory("TokenResponseTests")]
        public void ExpirationTimeTest()
        {
            // Need to get timestamp here since it needs to be before we create the token.
            // ExpireOn time is calculated from UtcNow when the object is created.
            DateTimeOffset current = DateTimeOffset.UtcNow;
            const long ExpiresInSeconds = 3599;

            MsalTokenResponse response = new MsalTokenResponse
            {
                IdToken =
                    "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ik1uQ19WWmNBVGZNNXBPWWlKSE1iYTlnb0VLWSIsImtpZCI6Ik1uQ19WWmNBVGZNNXBPWWlKSE1iYTlnb0VLWSJ9.eyJhdWQiOiI3YzdhMmY3MC1jYWVmLTQ1YzgtOWE2Yy0wOTE2MzM1MDFkZTQiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vODE2OTAyODYtNTA1NC00Zjk3LWI3MDgtNTQxNjU0Y2Q5MjFhL3YyLjAvIiwiaWF0IjoxNDU1NTc2MjM1LCJuYmYiOjE0NTU1NzYyMzUsImV4cCI6MTQ1NTU4MDEzNSwibmFtZSI6IkFEQUwgT2JqLUMgLSBFMkUiLCJvaWQiOiIxZTcwYThlZi1jYjIwLTQxOWMtYjhhNy1hNDJlZDJmYTIyNzciLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJlMmVAYWRhbG9iamMub25taWNyb3NvZnQuY29tIiwic3ViIjoibHJxVDlsQXQzSUlhS3hHanE2UlNReFRqN3diV3Q2RnpaMFU3NkJZMEJINCIsInRpZCI6IjgxNjkwMjg2LTUwNTQtNGY5Ny1iNzA4LTU0MTY1NGNkOTIxYSIsInZlciI6IjIuMCJ9.axS_-N3Z3b1GnZftxb6dKtMeooldoIQ_B7YrVO4CQI9xhHI1_Vl-dXfsFHBPRvIvXBEfBEehaaWq9B9P_CD5TpQXGycsYS08knHf_QpHIJ9WQbBIJ774divakx7kN6x7IxjoD1PrfRfo2QZsLLAz-1n-NHt7FwtkBQpKTDfgc6cVShy9isaJt5WoxfUM1eNo1HK_YjHj7Q5-n-XiZEbe-8m-7nqwBw86QDlLdk7dBhhCzVzXZb_5HCHI-23xZLYR34RoW7ljYEG4P8auEcML1haS4MN83VKRorMyljAIoA4YOgbfnvnlAlxRz_rtAAcjNqaUpIwzadGzd-QVbyoKPQ",
                AccessToken = "access-token",
                ExpiresIn = ExpiresInSeconds,
                CorrelationId = "correlation-id",
                RefreshToken = "refresh-token",
                Scope = "scope1 scope2",
                TokenType = "Bearer"
            };
            Assert.IsTrue(response.AccessTokenExpiresOn.Subtract(current) >= TimeSpan.FromSeconds(ExpiresInSeconds));
        }

        [TestMethod]
        [TestCategory("TokenResponseTests")]
        public void JsonDeserializationTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                OAuth2Client client = new OAuth2Client(httpManager);
                Task<MsalTokenResponse> task = client.GetTokenAsync(
                    new Uri(CoreTestConstants.AuthorityCommonTenant),
                    new RequestContext(new TestLogger(Guid.NewGuid(), null)));
                MsalTokenResponse response = task.Result;
                Assert.IsNotNull(response);
            }
        }
    }
}
