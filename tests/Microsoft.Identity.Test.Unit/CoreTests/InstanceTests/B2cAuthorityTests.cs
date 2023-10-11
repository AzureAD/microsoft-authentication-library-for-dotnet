// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    [DeploymentItem("Resources\\OpenidConfiguration-B2C.json")]
    [DeploymentItem("Resources\\OpenidConfiguration-B2CLogin.json")]
    public class B2CAuthorityTests : TestBase
    {
        [TestMethod]
        public void NotEnoughPathSegments_ThrowsException_Test()
        {
            var ex = Assert.ThrowsException<ArgumentException>(() =>
                Authority.CreateAuthority("https://login.microsoftonline.in/tfp/"));

            Assert.AreEqual(MsalErrorMessage.B2cAuthorityUriInvalidPath, ex.Message);
        }

        [DataTestMethod]
        [DataRow("https://sometenantid.b2clogin.com/tfp/sometenantid/policy/", "sometenantid")]
        [DataRow("https://catsareamazing.com/tfp/catsareamazing/policy/", "catsareamazing")]
        [DataRow("https://sometenantid.b2clogin.de/tfp/tid/policy/", "tid")]
        public void AuthorityDoesNotUpdateTenant(string authorityUri, string actualTenant)
        {
            Authority authority = Authority.CreateAuthority(authorityUri);
            Assert.AreEqual(actualTenant, authority.TenantId);

            string updatedAuthority = authority.GetTenantedAuthority("other_tenant_id", false);
            Assert.AreEqual(actualTenant, authority.TenantId);
            Assert.AreEqual(updatedAuthority, authorityUri);

            authority = Authority.CreateAuthorityWithTenant(authority.AuthorityInfo, "other_tenant_id_2");

            Assert.AreEqual(authority.AuthorityInfo.CanonicalAuthority.AbsoluteUri, authorityUri);
        }

        [TestMethod]
        public void B2CLoginAuthorityEndpoints_Success()
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
        public void CanonicalAuthority_Success()
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

        [DataTestMethod]
        [DataRow("https://login.microsoftonline.in/tfp/te nant/b2c_1_susi/")]
        [DataRow("http://login.microsoftonline.in/tfp/tenant/b2c_1_susi/")]
        public void MalformedAuthority_ThrowsException(string malformedAuthority)
        {
            Assert.ThrowsException<ArgumentException>(() =>
                ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithB2CAuthority(malformedAuthority)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .Build());

            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .Build();

            Assert.ThrowsException<ArgumentException>(() =>
                app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, "code")
                   .WithB2CAuthority(malformedAuthority));
        }
    }
}
