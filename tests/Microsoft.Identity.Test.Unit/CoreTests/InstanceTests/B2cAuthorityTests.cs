// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    [DeploymentItem("Resources\\OpenidConfiguration-B2C.json")]
    [DeploymentItem("Resources\\OpenidConfiguration-B2CLogin.json")]
    public class B2CAuthorityTests : TestBase
    {
        [TestMethod]
        public void NotEnoughPathSegmentsTest()
        {
            try
            {
                var serviceBundle = TestCommon.CreateDefaultServiceBundle();
                var instance = Authority.CreateAuthority("https://login.microsoftonline.in/tfp/");
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.B2C);

                Assert.Fail("test should have failed");
            }
            catch (Exception exc)
            {
                Assert.IsInstanceOfType(exc, typeof(ArgumentException));
                Assert.AreEqual(MsalErrorMessage.B2cAuthorityUriInvalidPath, exc.Message);
            }
        }

        [TestMethod]
        public void TenantTest()
        {
            AuthorityTestHelper.AuthorityDoesNotUpdateTenant(
                "https://sometenantid.b2clogin.com/tfp/sometenantid/policy/",
                "sometenantid");

            AuthorityTestHelper.AuthorityDoesNotUpdateTenant(
                "https://catsareamazing.com/tfp/catsareamazing/policy/",
                "catsareamazing");

            AuthorityTestHelper.AuthorityDoesNotUpdateTenant(
              "https://sometenantid.b2clogin.de/tfp/tid/policy/",
              "tid");
        }

        [TestMethod]
        public void B2CLoginAuthorityEndpoints()
        {
            Authority instance = Authority.CreateAuthority(
                "https://sometenantid.b2clogin.com/tfp/6babcaad-604b-40ac-a9d7-9fd97c0b779f/b2c_1_susi/");
            var _harness = base.CreateTestHarness();
            var _testRequestContext = new RequestContext(
                _harness.ServiceBundle,
                Guid.NewGuid());

            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.B2C);
            Assert.AreEqual(
                "https://sometenantid.b2clogin.com/tfp/6babcaad-604b-40ac-a9d7-9fd97c0b779f/b2c_1_susi/oauth2/v2.0/authorize",
                instance.GetAuthorizationEndpointAsync(_testRequestContext).Result);
            Assert.AreEqual(
                "https://sometenantid.b2clogin.com/tfp/6babcaad-604b-40ac-a9d7-9fd97c0b779f/b2c_1_susi/oauth2/v2.0/token",
                instance.GetTokenEndpointAsync(_testRequestContext).Result);
        }

        [TestMethod]
        public void CanonicalAuthorityInitTest()
        {
            string UriNoPort = TestConstants.B2CAuthority;
            Uri UriNoPortTailSlash = new Uri(TestConstants.B2CAuthority);

            string UriDefaultPort = $"https://login.microsoftonline.in:443/tfp/tenant/{TestConstants.B2CSignUpSignIn}";

            string UriCustomPort = $"https://login.microsoftonline.in:444/tfp/tenant/{TestConstants.B2CSignUpSignIn}";
            Uri UriCustomPortTailSlash = new Uri($"https://login.microsoftonline.in:444/tfp/tenant/{TestConstants.B2CSignUpSignIn}/");
            string UriVanityPort = TestConstants.B2CLoginAuthority;

            var authority = new B2CAuthority(new AuthorityInfo(AuthorityType.B2C, UriNoPort, true));
            Assert.AreEqual(UriNoPortTailSlash, authority.AuthorityInfo.CanonicalAuthority);

            authority = new B2CAuthority(new AuthorityInfo(AuthorityType.B2C, UriDefaultPort, true));
            Assert.AreEqual(UriNoPortTailSlash, authority.AuthorityInfo.CanonicalAuthority);

            authority = new B2CAuthority(new AuthorityInfo(AuthorityType.B2C, UriCustomPort, true));
            Assert.AreEqual(UriCustomPortTailSlash, authority.AuthorityInfo.CanonicalAuthority);

            authority = new B2CAuthority(new AuthorityInfo(AuthorityType.B2C, UriVanityPort, true));
            Assert.AreEqual(new Uri(UriVanityPort), authority.AuthorityInfo.CanonicalAuthority);
        }
    }
}
