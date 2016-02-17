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
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.Common;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    public class AuthenticationParametersTests
    {
        [TestMethod]
        [Description("Test for discovery via 401 challenge response")]
        [TestCategory("AdalDotNetUnit")]
        public void AuthenticationParametersTest()
        {
            Sts sts = new Sts();
            string authority = sts.Authority + "/oauth2/authorize";
            const string Resource = "test_resource";

            AuthenticationParameters authParams = AuthenticationParameters.CreateFromResponseAuthenticateHeader(string.Format(@"Bearer authorization_uri=""{0}"",resource_id=""{1}""", authority, Resource));
            Assert.AreEqual(authority, authParams.Authority);
            Assert.AreEqual(Resource, authParams.Resource);

            authParams = AuthenticationParameters.CreateFromResponseAuthenticateHeader(string.Format(@"bearer Authorization_uri=""{0}"",Resource_ID=""{1}""", authority, Resource));
            Assert.AreEqual(authority, authParams.Authority);
            Assert.AreEqual(Resource, authParams.Resource);

            authParams = AuthenticationParameters.CreateFromResponseAuthenticateHeader(string.Format(@"Bearer authorization_uri=""{0}""", authority));
            Assert.AreEqual(authority, authParams.Authority);
            Assert.IsNull(authParams.Resource);

            authParams = AuthenticationParameters.CreateFromResponseAuthenticateHeader(string.Format(@"Bearer resource_id=""{0}""", Resource));
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
                AuthenticationParameters.CreateFromResponseAuthenticateHeader(string.Format(@"authorization_uri=""{0}"",Resource_id=""{1}""", authority, Resource));
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual(ex.ParamName, "authenticateHeader");
                Assert.IsTrue(ex.Message.Contains("format"));
            }
        }
    }
}
