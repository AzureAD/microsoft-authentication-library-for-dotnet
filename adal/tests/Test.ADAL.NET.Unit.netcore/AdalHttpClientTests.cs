using Microsoft.Identity.Core;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
using Test.Microsoft.Identity.Core.Unit;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    public class AdalHttpClientTests
    {
        [TestMethod]
        public void QueryParamsFromEnvVariable()
        {
            try
            {
                // Arrange
                string uriWithVars = "http://contoso.com?existingVar=var&foo=bar";
                string uriWithoutVars = "http://contoso.com";

                RequestContext requestContext = new RequestContext(
                    "id",
                    new TestLogger(Guid.NewGuid(), null));

                const string extraQueryParams = "n1=v1&n2=v2";
                Environment.SetEnvironmentVariable(
                    AdalHttpClient.ExtraQueryParamEnvVariable,
                    extraQueryParams);

                // Act
                string actualUriWithVars = new AdalHttpClient(
                     uriWithVars, requestContext).RequestUri;

                string actualUriWithoutVars = new AdalHttpClient(
                  uriWithoutVars, requestContext).RequestUri;
                
                // Assert
                Assert.AreEqual(uriWithoutVars + "?" + extraQueryParams, actualUriWithoutVars);
                Assert.AreEqual(uriWithVars + "&" + extraQueryParams, actualUriWithVars);
            }
            finally
            {
                Environment.SetEnvironmentVariable(AdalHttpClient.ExtraQueryParamEnvVariable, null);
            }
        }

        [TestMethod]
        public void QueryParamsNoEnvVariable()
        {
            // Arrange
            Environment.SetEnvironmentVariable(AdalHttpClient.ExtraQueryParamEnvVariable, "");
            string initialUri = "http://contoso.com?existingVar=var&foo=bar";

            RequestContext requestContext = new RequestContext(
                "id",
                new TestLogger(Guid.NewGuid(), null));

            // Act
            string requestUri = new AdalHttpClient(
                     initialUri, requestContext).RequestUri;

            // Assert
            Assert.AreEqual(initialUri, requestUri);
        }
    }
}
