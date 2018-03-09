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
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Http;
using Microsoft.Identity.Client.Internal.Instance;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Guid = System.Guid;

namespace Test.MSAL.NET.Unit.InstanceTests
{
    [TestClass]
    [DeploymentItem("Resources\\OpenidConfiguration-B2C.json")]
    [DeploymentItem("Resources\\OpenidConfiguration-MissingFields-B2C.json")]
    public class B2CAuthorityTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Authority.ValidatedAuthorities.Clear();
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }


        [TestMethod]
        [TestCategory("B2CAuthorityTests")]
        public void NotEnoughPathSegmentsTest()
        {
            try
            {
                Authority instance = Authority.CreateAuthority("https://login.microsoftonline.in/tfp/", false);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityType, AuthorityType.B2C);
                Task
                    .Run(
                        async () => { await instance.ResolveEndpointsAsync(null, new RequestContext(Guid.NewGuid(), null)).ConfigureAwait(false); })
                    .GetAwaiter()
                    .GetResult();
                Assert.Fail("test should have failed");
            }
            catch (Exception exc)
            {
                Assert.IsInstanceOfType(exc, typeof(ArgumentException));
                Assert.AreEqual(MsalErrorMessage.B2cAuthorityUriInvalidPath, exc.Message);
            }
        }

        [TestMethod]
        [TestCategory("B2CAuthorityTests")]
        public void ValidationEnabledNotSupportedTest()
        {
            Authority instance = Authority.CreateAuthority("https://login.microsoftonline.in/tfp/tenant/policy", true);
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityType, AuthorityType.B2C);
            try
            {
                Task
                    .Run(
                        async () => { await instance.ResolveEndpointsAsync(null, new RequestContext(Guid.NewGuid(), null)).ConfigureAwait(false); })
                    .GetAwaiter()
                    .GetResult();
                Assert.Fail("test should have failed");
            }
            catch (Exception exc)
            {
                Assert.IsInstanceOfType(exc, typeof(ArgumentException));
                Assert.AreEqual(MsalErrorMessage.UnsupportedAuthorityValidation, exc.Message);
            }
        }

        [TestMethod]
        [TestCategory("B2CAuthorityTests")]
        public void CanonicalAuthorityInitTest()
        {
            const string uriNoPort = "https://login.microsoftonline.in/tfp/tenant/policy";
            const string uriNoPortTailSlash = "https://login.microsoftonline.in/tfp/tenant/policy/";

            const string uriDefaultPort = "https://login.microsoftonline.in:443/tfp/tenant/policy";

            const string uriCustomPort = "https://login.microsoftonline.in:444/tfp/tenant/policy";
            const string uriCustomPortTailSlash = "https://login.microsoftonline.in:444/tfp/tenant/policy/";

            var authority = new B2CAuthority(uriNoPort, false);
            Assert.AreEqual(uriNoPortTailSlash, authority.CanonicalAuthority);

            authority = new B2CAuthority(uriDefaultPort, false);
            Assert.AreEqual(uriNoPortTailSlash, authority.CanonicalAuthority);

            authority = new B2CAuthority(uriCustomPort, false);
            Assert.AreEqual(uriCustomPortTailSlash, authority.CanonicalAuthority);
        }
    }
}