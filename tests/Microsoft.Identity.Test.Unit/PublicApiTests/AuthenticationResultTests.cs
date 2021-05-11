// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class AuthenticationResultTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public void PublicTestConstructorCoversAllProperties()
        {
            var ctorParameters = typeof(AuthenticationResult)
                .GetConstructors()
                .First(ctor => ctor.GetParameters().Length > 3)
                .GetParameters();

            var classProperties = typeof(AuthenticationResult)
                .GetProperties()
                .Where(p => p.GetCustomAttribute(typeof(ObsoleteAttribute)) == null);

            Assert.AreEqual(ctorParameters.Length, classProperties.Count() + 1, "The <for test> constructor should include all properties of AuthenticationObject except AuthenticationScheme"); ;
        }

        [TestMethod]
        public void GetAuthorizationHeader()
        {
            var ar = new AuthenticationResult(
                "at",
                false,
                "uid",
                DateTime.UtcNow,
                DateTime.UtcNow,
                "tid",
                new Account("aid", "user", "env"),
                "idt", new[] { "scope" }, Guid.NewGuid(),
                "SomeTokenType",
                new AuthenticationResultMetadata(TokenSource.Cache));

            Assert.AreEqual("SomeTokenType at", ar.CreateAuthorizationHeader());
        }

        [TestMethod]
        public void GetIdTokenClaimsTest()
        {
            var ar = new AuthenticationResult(
                "at",
                false,
                "uid",
                DateTime.UtcNow,
                DateTime.UtcNow,
                "tid",
                new Account("aid", "user", "env"),
                MockHelpers.CreateIdTokenWithExtraClaim(TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.TenantId), 
                new[] { "scope" }, Guid.NewGuid(),
                "SomeTokenType",
                new AuthenticationResultMetadata(TokenSource.Cache));
            
            ClaimsPrincipal claimsPrincipal = ar.GetIdTokenClaims();
            Assert.IsNotNull(claimsPrincipal);
            Assert.AreEqual(13, claimsPrincipal.Claims.Count());
            var claims = claimsPrincipal.Claims;
            IDictionary<string, string> claimsDictionary = new Dictionary<string, string>();
            foreach (Claim claim in claims)
            {
                claimsDictionary.Add(claim.Type, claim.Value);
            }

            Assert.IsTrue(claimsDictionary.TryGetValue("some_claim", out string value));
            Assert.AreEqual("value", value);
        }

        [TestMethod]
        public void GetIdTokenClaimsWithNullIdTokenTest()
        {
            var ar = new AuthenticationResult(
                "at",
                false,
                "uid",
                DateTime.UtcNow,
                DateTime.UtcNow,
                "tid",
                new Account("aid", "user", "env"),
                null,
                new[] { "scope" }, Guid.NewGuid(),
                "SomeTokenType",
                new AuthenticationResultMetadata(TokenSource.Cache));

            Assert.IsNull(ar.GetIdTokenClaims());
        }

        [TestMethod]
        public void GetIdTokenClaimsWithInvalidIdTokenStringTest()
        {
            var ar = new AuthenticationResult(
                "at",
                false,
                "uid",
                DateTime.UtcNow,
                DateTime.UtcNow,
                "tid",
                new Account("aid", "user", "env"),
                "bad_token",
                new[] { "scope" }, Guid.NewGuid(),
                "SomeTokenType",
                new AuthenticationResultMetadata(TokenSource.Cache));

            try
            {
                ar.GetIdTokenClaims();
                Assert.Fail("Should have failed due to invalid token");
            } 
            catch (MsalClientException e)
            {
                Assert.IsNotNull(e);
                Assert.AreEqual(MsalError.InvalidJwtError, e.ErrorCode);
            }
        }

        [TestMethod]
        public void GetIdTokenClaimsWithInvalidIdTokenTest()
        {
            var ar = new AuthenticationResult(
                "at",
                false,
                "uid",
                DateTime.UtcNow,
                DateTime.UtcNow,
                "tid",
                new Account("aid", "user", "env"),
                MockHelpers.CreateIdTokenWithInvalidJson(),
                new[] { "scope" }, Guid.NewGuid(),
                "SomeTokenType",
                new AuthenticationResultMetadata(TokenSource.Cache));

            try
            {
                ar.GetIdTokenClaims();
                Assert.Fail("Should have failed due to invalid token");
            }
            catch (MsalClientException e)
            {
                Assert.IsNotNull(e);
                Assert.AreEqual(MsalError.JsonParseError, e.ErrorCode);
            }
        }

        [TestMethod]
        [Description(
            "In MSAL 4.17 we made a mistake and added AuthenticationResultMetadata with no default value before tokenType param. " +
            "To fix this breaking change, we added 2 constructors - " +
            "one for backwards compat with 4.17+ and one for 4.16 and below")]
        public void AuthenticationResult_PublicApi()
        {
            // old constructor, before 4.16
            var ar1 = new AuthenticationResult(
                "at",
                false,
                "uid",
                DateTime.UtcNow,
                DateTime.UtcNow,
                "tid",
                new Account("aid", "user", "env"),
                "idt", 
                new[] { "scope" }, 
                Guid.NewGuid());

            Assert.IsNull(ar1.AuthenticationResultMetadata);
            Assert.AreEqual("Bearer", ar1.TokenType);

            // old constructor, before 4.16
            var ar2 = new AuthenticationResult(
              "at",
              false,
              "uid",
              DateTime.UtcNow,
              DateTime.UtcNow,
              "tid",
              new Account("aid", "user", "env"),
              "idt",
              new[] { "scope" },
              Guid.NewGuid(), 
              "ProofOfBear");

            Assert.IsNull(ar2.AuthenticationResultMetadata);
            Assert.AreEqual("ProofOfBear", ar2.TokenType);

            // new ctor, after 4.17
            var ar3 = new AuthenticationResult(
             "at",
             false,
             "uid",
             DateTime.UtcNow,
             DateTime.UtcNow,
             "tid",
             new Account("aid", "user", "env"),
             "idt",
             new[] { "scope" },
             Guid.NewGuid(),
             new AuthenticationResultMetadata(TokenSource.Broker));

            Assert.AreEqual(TokenSource.Broker, ar3.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual("Bearer", ar1.TokenType);

        }
    }
}
