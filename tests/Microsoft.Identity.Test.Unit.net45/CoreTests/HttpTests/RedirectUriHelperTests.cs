// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common;

namespace Microsoft.Identity.Test.Unit.CoreTests.HttpTests
{
    [TestClass]
    public class RedirectUriHelperTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public void ValidateRedirectUri_Throws()
        {

            Assert.ThrowsException<MsalClientException>(
                () => RedirectUriHelper.Validate(null));

            Assert.ThrowsException<ArgumentException>(
               () => RedirectUriHelper.Validate(new Uri("https://redirectUri/uri#fragment")),
               "Validatation should fail if uri has a fragment, i.e. #foo");
        }

        [TestMethod]
        public void ValidateRedirectUri_DoesNotThrow()
        {
            // Arrange
            Uri inputUri = new Uri("http://redirectUri");

            // Act
            RedirectUriHelper.Validate(inputUri);

            // Assert
            // no exception is thrown
        }

        [TestMethod]
        public void ValidateRedirectUri_NoOAuth2DefaultWhenUsingSystemBrowser()
        {
            Assert.ThrowsException<MsalClientException>(() =>
                RedirectUriHelper.Validate(new Uri(Constants.DefaultRedirectUri), true));

              RedirectUriHelper.Validate(new Uri(Constants.DefaultRedirectUri), false);
        }
    }
}
