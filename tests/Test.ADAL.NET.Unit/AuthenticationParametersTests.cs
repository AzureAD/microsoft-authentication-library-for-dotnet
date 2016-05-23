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
using System.Globalization;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.Common;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    public class AuthenticationParametersTests
    {
        [TestMethod]
        [Description("Test for discovery via 401 challenge response")]
        public void AuthenticationParametersTest()
        {
            string authority = TestConstants.DefaultAuthorityCommonTenant + "/oauth2/authorize";
            const string Resource = "test_resource";

            AuthenticationParameters authParams = AuthenticationParameters.CreateFromResponseAuthenticateHeader(string.Format(CultureInfo.InvariantCulture, @"Bearer authorization_uri=""{0}"",resource_id=""{1}""", authority, Resource));
            Assert.AreEqual(authority, authParams.Authority);
            Assert.AreEqual(Resource, authParams.Resource);

            authParams = AuthenticationParameters.CreateFromResponseAuthenticateHeader(string.Format(CultureInfo.InvariantCulture, @"bearer Authorization_uri=""{0}"",Resource_ID=""{1}""", authority, Resource));
            Assert.AreEqual(authority, authParams.Authority);
            Assert.AreEqual(Resource, authParams.Resource);

            authParams = AuthenticationParameters.CreateFromResponseAuthenticateHeader(string.Format(CultureInfo.InvariantCulture, @"Bearer authorization_uri=""{0}""", authority));
            Assert.AreEqual(authority, authParams.Authority);
            Assert.IsNull(authParams.Resource);

            authParams = AuthenticationParameters.CreateFromResponseAuthenticateHeader(string.Format(CultureInfo.InvariantCulture, @"Bearer resource_id=""{0}""", Resource));
            Assert.AreEqual(Resource, authParams.Resource);
            Assert.IsNull(authParams.Authority);

            try
            {
                AuthenticationParameters.CreateFromResponseAuthenticateHeader(null);
            }
            catch(ArgumentNullException ex)
            {
                Assert.AreEqual(ex.ParamName, "authenticateHeader");
            }

            try
            {
                AuthenticationParameters.CreateFromResponseAuthenticateHeader(string.Format(CultureInfo.InvariantCulture, @"authorization_uri=""{0}"",Resource_id=""{1}""", authority, Resource));
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual(ex.ParamName, "authenticateHeader");
                Assert.IsTrue(ex.Message.Contains("format"));
            }
        }
    }
}
