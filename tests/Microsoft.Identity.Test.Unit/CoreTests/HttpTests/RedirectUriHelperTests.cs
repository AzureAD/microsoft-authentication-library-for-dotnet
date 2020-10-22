// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Client.Internal;
using System.Globalization;
using Microsoft.Identity.Client.Internal.Logger;

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

        [TestMethod]
        public void iOSBrokerRedirectUri()
        {
            string bundleId = "bundleId";

            RedirectUriHelper.ValidateIosBrokerRedirectUri(new Uri($"msauth.{bundleId}://auth"), bundleId, new NullLogger());
            RedirectUriHelper.ValidateIosBrokerRedirectUri(new Uri($"msauth.{bundleId}://auth/"), bundleId, new NullLogger());
            RedirectUriHelper.ValidateIosBrokerRedirectUri(new Uri($"myscheme://{bundleId}"), bundleId, new NullLogger());
            RedirectUriHelper.ValidateIosBrokerRedirectUri(new Uri($"myscheme://{bundleId}/"), bundleId, new NullLogger());
            RedirectUriHelper.ValidateIosBrokerRedirectUri(new Uri($"myscheme://{bundleId}/suffix"), bundleId, new NullLogger());

            // the comparison MUST be case sensitive 
            Assert.ThrowsException<MsalClientException>(() =>
               RedirectUriHelper.ValidateIosBrokerRedirectUri(
                   new Uri($"msauth.{bundleId.ToUpper(CultureInfo.InvariantCulture)}://auth"),
                   bundleId, new NullLogger()));

            Assert.ThrowsException<MsalClientException>(() =>
              RedirectUriHelper.ValidateIosBrokerRedirectUri(
                  new Uri($"other.{bundleId}://auth"), bundleId, new NullLogger()));

            Assert.ThrowsException<MsalClientException>(() =>
                RedirectUriHelper.ValidateIosBrokerRedirectUri(
                  new Uri($"msauth.{bundleId}://other"), bundleId, new NullLogger()));
        }
    }
}
