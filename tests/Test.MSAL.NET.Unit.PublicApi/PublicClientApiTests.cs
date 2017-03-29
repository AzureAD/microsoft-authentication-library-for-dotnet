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
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using Test.MSAL.Common;

namespace Test.MSAL.NET.Unit.PublicApi
{

    [TestClass]
    public class PublicClientApiTests
    {
        [TestMethod]
        [Description("Tests the public interfaces can be mocked")]
        public void MockPublicClientApplication()
        {
            // Setup up a public client application that returns a dummy result
            // The caller asks for two scopes, but only one is returned
            var mockResult = Substitute.For<IAuthenticationResult>();
            mockResult.IdToken.Returns("id token");
            mockResult.Scope.Returns(new string[] { "scope1" });

            var mockApp = Substitute.For<IPublicClientApplication>();
            mockApp.AcquireTokenAsync(new string[] { "scope1", "scope2" }).ReturnsForAnyArgs(mockResult);

            // Now call the substitute with the args to get the substitute result
            IAuthenticationResult actualResult = mockApp.AcquireTokenAsync(new string[] { "scope1" }).Result;
            Assert.IsNotNull(actualResult);
            Assert.AreEqual("id token", actualResult.IdToken, "Mock result failed to return the expected id token");

            // Check the users properties returns the dummy users
            IEnumerable<string> scopes = actualResult.Scope;
            Assert.IsNotNull(scopes);
            CollectionAssert.AreEqual(new string[] { "scope1" }, actualResult.Scope.ToArray());
        }

        [TestMethod]
        [Description("Tests the public application interfaces can be mocked to throw MSAL exceptions")]
        public void MockPublicClientApplication_Exception()
        {
            // Setup up a confidential client application that returns throws
            var mockApp = Substitute.For<IPublicClientApplication>();
            mockApp
                .WhenForAnyArgs(x => x.AcquireTokenAsync(Arg.Any<string[]>()))
                .Do(x => { throw new MsalServiceException("my error code", "my message"); });


            // Now call the substitute and check the exception is thrown
            MsalServiceException ex = AssertException.Throws<MsalServiceException>(() => mockApp.AcquireTokenAsync(new string[] { "scope1" }));
            Assert.AreEqual("my error code", ex.ErrorCode);
            Assert.AreEqual("my message", ex.Message);
        }
    }
}
