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
using System.Net;
using Microsoft.Identity.Client.Interfaces;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class TokenResponseTests
    {
        [TestMethod]
        [TestCategory("TokenResponseTests")]
        public void GetResultTest()
        {
            TokenResponse response = new TokenResponse();
            response.IdToken =
                "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ik1uQ19WWmNBVGZNNXBPWWlKSE1iYTlnb0VLWSIsImtpZCI6Ik1uQ19WWmNBVGZNNXBPWWlKSE1iYTlnb0VLWSJ9.eyJhdWQiOiI3YzdhMmY3MC1jYWVmLTQ1YzgtOWE2Yy0wOTE2MzM1MDFkZTQiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vODE2OTAyODYtNTA1NC00Zjk3LWI3MDgtNTQxNjU0Y2Q5MjFhL3YyLjAvIiwiaWF0IjoxNDU1NTc2MjM1LCJuYmYiOjE0NTU1NzYyMzUsImV4cCI6MTQ1NTU4MDEzNSwibmFtZSI6IkFEQUwgT2JqLUMgLSBFMkUiLCJvaWQiOiIxZTcwYThlZi1jYjIwLTQxOWMtYjhhNy1hNDJlZDJmYTIyNzciLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJlMmVAYWRhbG9iamMub25taWNyb3NvZnQuY29tIiwic3ViIjoibHJxVDlsQXQzSUlhS3hHanE2UlNReFRqN3diV3Q2RnpaMFU3NkJZMEJINCIsInRpZCI6IjgxNjkwMjg2LTUwNTQtNGY5Ny1iNzA4LTU0MTY1NGNkOTIxYSIsInZlciI6IjIuMCJ9.axS_-N3Z3b1GnZftxb6dKtMeooldoIQ_B7YrVO4CQI9xhHI1_Vl-dXfsFHBPRvIvXBEfBEehaaWq9B9P_CD5TpQXGycsYS08knHf_QpHIJ9WQbBIJ774divakx7kN6x7IxjoD1PrfRfo2QZsLLAz-1n-NHt7FwtkBQpKTDfgc6cVShy9isaJt5WoxfUM1eNo1HK_YjHj7Q5-n-XiZEbe-8m-7nqwBw86QDlLdk7dBhhCzVzXZb_5HCHI-23xZLYR34RoW7ljYEG4P8auEcML1haS4MN83VKRorMyljAIoA4YOgbfnvnlAlxRz_rtAAcjNqaUpIwzadGzd-QVbyoKPQ";
            response.AccessToken = "access-token";
            response.ExpiresIn = 3599;
            response.CorrelationId = "correlation-id";
            response.RefreshToken = "refresh-token";
            response.FamilyId = "1";
            response.Scope = "scope1 scope2";
            response.TokenType = "Bearer";

            AuthenticationResultEx resultEx = response.GetResultEx();
            Assert.IsNotNull(resultEx);
            Assert.AreEqual("access-token", resultEx.Result.Token);
            Assert.AreEqual("scope1 scope2", resultEx.Result.ScopeSet.AsSingleString());
            Assert.AreEqual("Bearer", resultEx.Result.TokenType);
        }
        
        private static TokenResponse CreateTokenResponse()
        {
            return new TokenResponse
            {
                AccessToken = "access_token",
                RefreshToken = "refresh_token",
                CorrelationId = Guid.NewGuid().ToString(),
                Scope = "my-resource",
                TokenType = "Bearer",
                ExpiresIn = 3899
            };
        }
    }
}
