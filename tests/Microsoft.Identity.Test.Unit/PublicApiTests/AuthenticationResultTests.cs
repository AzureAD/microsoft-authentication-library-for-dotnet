// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
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
