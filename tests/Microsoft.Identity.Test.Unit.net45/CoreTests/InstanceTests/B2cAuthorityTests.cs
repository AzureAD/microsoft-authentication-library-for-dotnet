// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Config;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    [DeploymentItem("Resources\\OpenidConfiguration-B2C.json")]
    [DeploymentItem("Resources\\OpenidConfiguration-B2CLogin.json")]
    public class B2CAuthorityTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        [TestCategory("B2CAuthorityTests")]
        public async Task NotEnoughPathSegmentsTestAsync()
        {
            try
            {
                var serviceBundle = TestCommon.CreateDefaultServiceBundle();
                var instance = Authority.CreateAuthority(serviceBundle, "https://login.microsoftonline.in/tfp/", false);

                var endpointManager = new AuthorityEndpointResolutionManager(serviceBundle);

                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityType, AuthorityType.B2C);
                await endpointManager.ResolveEndpointsAsync(
                    instance.AuthorityInfo,
                    null,
                    RequestContext.CreateForTest()).ConfigureAwait(false);
                Assert.Fail("test should have failed");
            }
            catch (Exception exc)
            {
                Assert.IsInstanceOfType(exc, typeof(ArgumentException));
                Assert.AreEqual(CoreErrorMessages.B2cAuthorityUriInvalidPath, exc.Message);
            }
        }

        [TestMethod]
        [TestCategory("B2CAuthorityTests")]
        public async Task B2CLoginAuthorityCreateAuthorityAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(httpManager);
                var endpointManager = new AuthorityEndpointResolutionManager(serviceBundle);

                // Add mock response for tenant endpoint discovery
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url =
                            "https://mytenant.com.b2clogin.com/tfp/mytenant.com/my-policy/v2.0/.well-known/openid-configuration",
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                            File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("OpenidConfiguration-B2CLogin.json")))
                    });

                var instance = Authority.CreateAuthority(
                    serviceBundle,
                    "https://mytenant.com.b2clogin.com/tfp/mytenant.com/my-policy/",
                    true);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityType, AuthorityType.B2C);
                var endpoints = await endpointManager.ResolveEndpointsAsync(
                                                         instance.AuthorityInfo,
                                                         null,
                                                         RequestContext.CreateForTest())
                                                     .ConfigureAwait(false);

                Assert.AreEqual(
                    "https://mytenant.com.b2clogin.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/my-policy/oauth2/v2.0/authorize",
                    endpoints.AuthorizationEndpoint);
                Assert.AreEqual(
                    "https://mytenant.com.b2clogin.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/my-policy/oauth2/v2.0/token",
                    endpoints.TokenEndpoint);
                Assert.AreEqual(
                    "https://mytenant.com.b2clogin.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/v2.0/",
                    endpoints.SelfSignedJwtAudience);
            }
        }

        [TestMethod]
        [TestCategory("B2CAuthorityTests")]
        public async Task B2CMicrosoftOnlineCreateAuthorityAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(httpManager);
                var endpointManager = new AuthorityEndpointResolutionManager(serviceBundle);

                // Add mock response for tenant endpoint discovery
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url =
                            "https://login.microsoftonline.com/tfp/mytenant.com/my-policy/v2.0/.well-known/openid-configuration",
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                            File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("OpenidConfiguration-B2C.json")))
                    });

                var instance = Authority.CreateAuthority(
                    serviceBundle,
                    "https://login.microsoftonline.com/tfp/mytenant.com/my-policy/",
                    true);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityType, AuthorityType.B2C);
                var endpoints = await endpointManager.ResolveEndpointsAsync(
                                                         instance.AuthorityInfo,
                                                         null,
                                                         RequestContext.CreateForTest())
                                                     .ConfigureAwait(false);

                Assert.AreEqual(
                    "https://login.microsoftonline.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/my-policy/oauth2/v2.0/authorize",
                    endpoints.AuthorizationEndpoint);
                Assert.AreEqual(
                    "https://login.microsoftonline.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/my-policy/oauth2/v2.0/token",
                    endpoints.TokenEndpoint);
                Assert.AreEqual("https://sts.windows.net/6babcaad-604b-40ac-a9d7-9fd97c0b779f/", endpoints.SelfSignedJwtAudience);
            }
        }

        [TestMethod]
        [TestCategory("B2CAuthorityTests")]
        public void CanonicalAuthorityInitTest()
        {
            var serviceBundle = TestCommon.CreateDefaultServiceBundle();

            const string UriNoPort = CoreTestConstants.B2CAuthority;
            const string UriNoPortTailSlash = CoreTestConstants.B2CAuthority;

            const string UriDefaultPort = "https://login.microsoftonline.in:443/tfp/tenant/policy";

            const string UriCustomPort = "https://login.microsoftonline.in:444/tfp/tenant/policy";
            const string UriCustomPortTailSlash = "https://login.microsoftonline.in:444/tfp/tenant/policy/";
            const string UriVanityPort = CoreTestConstants.B2CLoginAuthority;

            var authority = new B2CAuthority(serviceBundle, AuthorityInfo.FromAuthorityUri(UriNoPort, false, false));
            Assert.AreEqual(UriNoPortTailSlash, authority.CanonicalAuthority);

            authority = new B2CAuthority(serviceBundle, AuthorityInfo.FromAuthorityUri(UriDefaultPort, false, false));
            Assert.AreEqual(UriNoPortTailSlash, authority.CanonicalAuthority);

            authority = new B2CAuthority(serviceBundle, AuthorityInfo.FromAuthorityUri(UriCustomPort, false, false));
            Assert.AreEqual(UriCustomPortTailSlash, authority.CanonicalAuthority);

            authority = new B2CAuthority(serviceBundle, AuthorityInfo.FromAuthorityUri(UriVanityPort, false, false));
            Assert.AreEqual(UriVanityPort, authority.CanonicalAuthority);
        }
    }
}