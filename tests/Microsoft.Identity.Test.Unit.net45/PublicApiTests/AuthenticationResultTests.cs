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
                "SomeTokenType");

            Assert.AreEqual("SomeTokenType at", ar.CreateAuthorizationHeader());
        }
    }
}
