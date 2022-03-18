// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    public class IdTokenParsingTests
    {

        [TestMethod]
        public void IdTokenParsing_AADToken()
        {
            /*
                "aud": "b6c69a37-df96-4db0-9088-2ab96e1d8215",
                "iss": "https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca/v2.0",
                "iat": 1538538422,
                "nbf": 1538538422,
                "exp": 1538542322,
                "name": "Cloud IDLAB Basic User",
                "oid": "9f4880d8-80ba-4c40-97bc-f7a23c703084",
                "preferred_username": "idlab@msidlab4.onmicrosoft.com",
                "sub": "Y6YkBdHNNLHNmTKel9KhRz8wrasxdLRFiP14BRPWrn4",
                "tid": "f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
                "uti": "6nciX02SMki9k73-F1sZAA",
                "ver": "2.0"
             */
            var addIdToken = TestConstants.CreateAadTestTokenResponse().IdToken;
            var parsedToken = IdToken.Parse(addIdToken);

            CoreAssert.AreEqual("Cloud IDLAB Basic User", parsedToken.Name, parsedToken.ClaimsPrincipal.FindFirst("name").Value);
            CoreAssert.AreEqual("9f4880d8-80ba-4c40-97bc-f7a23c703084", parsedToken.ObjectId, parsedToken.ClaimsPrincipal.FindFirst("oid").Value);
            CoreAssert.AreEqual("idlab@msidlab4.onmicrosoft.com", parsedToken.PreferredUsername, parsedToken.ClaimsPrincipal.FindFirst("preferred_username").Value);
            CoreAssert.AreEqual("Y6YkBdHNNLHNmTKel9KhRz8wrasxdLRFiP14BRPWrn4", parsedToken.Subject, parsedToken.ClaimsPrincipal.FindFirst("sub").Value);
            CoreAssert.AreEqual("f645ad92-e38d-4d1a-b510-d1b09a74a8ca", parsedToken.TenantId, parsedToken.ClaimsPrincipal.FindFirst("tid").Value);

            Assert.AreEqual("b6c69a37-df96-4db0-9088-2ab96e1d8215", parsedToken.ClaimsPrincipal.Claims.Single(c => c.Type == "aud").Value);
            Assert.AreEqual("https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca/v2.0", parsedToken.ClaimsPrincipal.Claims.Single(c => c.Type == "iss").Value);
            Assert.AreEqual("1538538422", parsedToken.ClaimsPrincipal.Claims.Single(c => c.Type == "iat").Value);
            Assert.AreEqual("1538538422", parsedToken.ClaimsPrincipal.Claims.Single(c => c.Type == "nbf").Value);
            Assert.AreEqual("1538542322", parsedToken.ClaimsPrincipal.Claims.Single(c => c.Type == "exp").Value);
            Assert.AreEqual("Cloud IDLAB Basic User", parsedToken.ClaimsPrincipal.Claims.Single(c => c.Type == "name").Value);
            Assert.AreEqual("9f4880d8-80ba-4c40-97bc-f7a23c703084", parsedToken.ClaimsPrincipal.Claims.Single(c => c.Type == "oid").Value);
            Assert.AreEqual("idlab@msidlab4.onmicrosoft.com", parsedToken.ClaimsPrincipal.Claims.Single(c => c.Type == "preferred_username").Value);
            Assert.AreEqual("Y6YkBdHNNLHNmTKel9KhRz8wrasxdLRFiP14BRPWrn4", parsedToken.ClaimsPrincipal.Claims.Single(c => c.Type == "sub").Value);
            Assert.AreEqual("f645ad92-e38d-4d1a-b510-d1b09a74a8ca", parsedToken.ClaimsPrincipal.Claims.Single(c => c.Type == "tid").Value);
            Assert.AreEqual("6nciX02SMki9k73-F1sZAA", parsedToken.ClaimsPrincipal.Claims.Single(c => c.Type == "uti").Value);
            Assert.AreEqual("2.0", parsedToken.ClaimsPrincipal.Claims.Single(c => c.Type == "ver").Value);

            Assert.IsTrue(parsedToken.ClaimsPrincipal.Claims.Where(c => (new[] { "nbf", "iat", "exp" }).Contains(c.Type) == true).All(c => c.ValueType == ClaimValueTypes.Integer.ToString()));
            Assert.IsTrue(parsedToken.ClaimsPrincipal.Claims.Where(c => (new[] { "nbf", "iat", "exp" }).Contains(c.Type) == false).All(c => c.ValueType == ClaimValueTypes.String.ToString()));

            Assert.IsNull(parsedToken.Upn);
            Assert.IsNull(parsedToken.FamilyName);
            Assert.IsNull(parsedToken.GivenName);

            Assert.IsTrue(parsedToken.ClaimsPrincipal.Claims.All(c => c.Issuer == "https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca/v2.0"));
            Assert.IsTrue(parsedToken.ClaimsPrincipal.Claims.All(c => c.OriginalIssuer == "https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca/v2.0"));
        }

        [TestMethod]
        public void IdTokenParsing_B2CToken_OneEmail()
        {
            /*
                  "exp": 1538804860,
                  "nbf": 1538801260,
                  "ver": "1.0",
                  "iss": "https://login.microsoftonline.com/ba6c0d94-a8da-45b2-83ae-33871f9c2dd8/v2.0/",
                  "sub": "ad020f8e-b1ba-44b2-bd69-c22be86737f5",
                  "aud": "0a7f52dd-260e-432f-94de-b47828c3f372",
                  "iat": 1538801260,
                  "auth_time": 1538801260,
                  "idp": "live.com",
                  "name": "MSAL SDK Test",
                  "oid": "ad020f8e-b1ba-44b2-bd69-c22be86737f5",
                  "family_name": "SDK Test",
                  "given_name": "MSAL",
                  "emails": [
                    "msalsdktest@outlook.com", 
                  ],
                  "tfp": "B2C_1_Signin",
                  "at_hash": "Q4O3HDClcaLl7y0uU-bJAg"

             */
            var addIdToken = TestConstants.CreateB2CTestTokenResponse().IdToken;
            var parsedToken = IdToken.Parse(addIdToken);

            Assert.AreEqual("msalsdktest@outlook.com", parsedToken.ClaimsPrincipal.Claims.Single(c => c.Type == "emails").Value);
            Assert.AreEqual(ClaimValueTypes.String.ToString(), parsedToken.ClaimsPrincipal.Claims.Single(c => c.Type == "emails").ValueType);
        }

        [TestMethod]
        public void IdTokenParsing_B2CToken_TwoEmails()
        {
            /*
                 {
                  "sub": "1234567890",
                  "emails": ["a@outlook.com", "b@hotmail.com"],
                  "iat": 1516239022
                    }

             */
            var addIdToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwiZW1haWxzIjpbImFAb3V0bG9vay5jb20iLCJiQGhvdG1haWwuY29tIl0sImlhdCI6MTUxNjIzOTAyMn0.";
            var parsedToken = IdToken.Parse(addIdToken);

            Assert.AreEqual(2, parsedToken.ClaimsPrincipal.Claims.Where(c => c.Type == "emails").Count());
            Assert.IsTrue(parsedToken.ClaimsPrincipal.Claims.Where(c => c.Type == "emails").All(c => (new[] { "a@outlook.com", "b@hotmail.com" }).Contains(c.Value)));
            
        }
    }
}
