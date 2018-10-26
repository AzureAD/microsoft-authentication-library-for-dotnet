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
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Core.Instance;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Test.Microsoft.Identity.Core.Unit;

namespace Test.MSAL.NET.Unit.RequestsTests
{
#if !WINDOWS_APP && !ANDROID && !iOS // Confidential Client not available

    [TestClass]
    public class RequestValidationHelperTests
    {
        public const uint JwtToAadLifetimeInSeconds = 60 * 10; // Ten minutes

        [TestMethod]
        [Description("Test for client assertion with mismatched parameters in Request Validator.")]
        public void ClientAssertionRequestValidatorMismatchParameterTest()
        {
            string Audience1 = "Audience1";
            string Audience2 = "Audience2";

            var credential = new ClientCredential(MsalTestConstants.ClientSecret);
            credential.Audience = Audience1;
            credential.ContainsX5C = false;
            credential.Assertion = MsalTestConstants.DefaultClientAssertion;
            credential.ValidTo = ConvertToTimeT(DateTime.UtcNow + TimeSpan.FromSeconds(JwtToAadLifetimeInSeconds));

            var parameters = new AuthenticationRequestParameters();
            parameters.ClientCredential = credential;
            parameters.SendCertificate = false;
            parameters.Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityCommonTenant, false);
            parameters.Authority.SelfSignedJwtAudience = Audience1;

            //Validate cached client assertion with parameters
            Assert.IsTrue(RequestValidationHelper.ValidateClientAssertion(parameters));

            //Different audience
            credential.Audience = Audience2;

            //cached assertion should be invalid
            Assert.IsFalse(RequestValidationHelper.ValidateClientAssertion(parameters));

            //Different x5c, same audience
            credential.Audience = Audience1;
            credential.ContainsX5C = true;

            //cached assertion should be invalid
            Assert.IsFalse(RequestValidationHelper.ValidateClientAssertion(parameters));

            //Different audience and x5c
            credential.Audience = Audience2;

            //cached assertion should be invalid
            Assert.IsFalse(RequestValidationHelper.ValidateClientAssertion(parameters));

            //No cached Assertion
            credential.Assertion = "";

            //should return false
            Assert.IsFalse(RequestValidationHelper.ValidateClientAssertion(parameters));
        }

        [TestMethod]
        [Description("Test for expired client assertion in Request Validator.")]
        public void ClientAssertionRequestValidatorExpirationTimeTest()
        {
            var credential = new ClientCredential(MsalTestConstants.ClientSecret);
            credential.Audience = "Audience1";
            credential.ContainsX5C = false;
            credential.Assertion = MsalTestConstants.DefaultClientAssertion;
            credential.ValidTo = ConvertToTimeT(DateTime.UtcNow + TimeSpan.FromSeconds(JwtToAadLifetimeInSeconds));

            var parameters = new AuthenticationRequestParameters();
            parameters.ClientCredential = credential;
            parameters.SendCertificate = false;
            parameters.Authority = Authority.CreateAuthority(MsalTestConstants.AuthorityCommonTenant, false);
            parameters.Authority.SelfSignedJwtAudience = "Audience1";

            //Validate cached client assertion with expiration time
            //Cached assertion should be valid
            Assert.IsTrue(RequestValidationHelper.ValidateClientAssertion(parameters));

            //Setting expiration time to now
            credential.ValidTo = ConvertToTimeT(DateTime.UtcNow);

            //cached assertion should have expired
            Assert.IsFalse(RequestValidationHelper.ValidateClientAssertion(parameters));
        }

        internal static long ConvertToTimeT(DateTime time)
        {
            var startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = time - startTime;
            return (long)(diff.TotalSeconds);
        }
    }
#endif
}
