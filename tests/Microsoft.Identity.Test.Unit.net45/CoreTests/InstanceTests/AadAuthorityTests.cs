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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.Telemetry;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Core.Mocks.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    [DeploymentItem("Resources\\OpenidConfiguration.json")]
    [DeploymentItem("Resources\\OpenidConfiguration-MissingFields.json")]
    public class AadAuthorityTests
    {
        private IValidatedAuthoritiesCache _validatedAuthoritiesCache;

        [TestInitialize]
        public void TestInitialize()
        {
            _validatedAuthoritiesCache = new ValidatedAuthoritiesCache();
            CoreExceptionFactory.Instance = new TestExceptionFactory();
        }

        [TestCleanup]
        public void TestCleanup()
        {

        }

        [TestMethod]
        [TestCategory("AadAuthorityTests")]
        public void SuccessfulValidationTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);

                //add mock response for instance validation
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url = "https://login.microsoftonline.com/common/discovery/instance",
                        QueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.1"},
                            {
                                "authorization_endpoint",
                                "https%3A%2F%2Flogin.microsoftonline.in%2Fmytenant.com%2Foauth2%2Fv2.0%2Fauthorize"
                            },
                        },
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                            "{\"tenant_discovery_endpoint\":\"https://login.microsoftonline.in/mytenant.com/.well-known/openid-configuration\"}")
                    });

                //add mock response for tenant endpoint discovery
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url = "https://login.microsoftonline.in/mytenant.com/.well-known/openid-configuration",
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                           File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("OpenidConfiguration.json")))
                    });

                Authority instance = Authority.CreateAuthority(serviceBundle, "https://login.microsoftonline.in/mytenant.com", true);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityType, AuthorityType.Aad);
                Task.Run(
                    async () =>
                    {
                        await instance.ResolveEndpointsAsync(
                            null, 
                            new RequestContext(null, new TestLogger(Guid.NewGuid(), null))).ConfigureAwait(false);
                    }).GetAwaiter().GetResult();

                Assert.AreEqual(
                    "https://login.microsoftonline.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/oauth2/v2.0/authorize",
                    instance.AuthorizationEndpoint);
                Assert.AreEqual(
                    "https://login.microsoftonline.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/oauth2/v2.0/token",
                    instance.TokenEndpoint);
                Assert.AreEqual("https://sts.windows.net/6babcaad-604b-40ac-a9d7-9fd97c0b779f/", instance.SelfSignedJwtAudience);
                Assert.AreEqual("https://login.microsoftonline.in/common/userrealm/", instance.UserRealmUriPrefix);
            }
        }

        [TestMethod]
        [TestCategory("AadAuthorityTests")]
        public void ValidationOffSuccessTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);

                //add mock response for tenant endpoint discovery
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url = "https://login.microsoftonline.in/mytenant.com/v2.0/.well-known/openid-configuration",
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                           File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("OpenidConfiguration.json")))
                    });

                Authority instance = Authority.CreateAuthority(serviceBundle, "https://login.microsoftonline.in/mytenant.com", false);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityType, AuthorityType.Aad);
                Task.Run(
                    async () =>
                    {
                        await instance.ResolveEndpointsAsync(
                            null, 
                            new RequestContext(null, new TestLogger(Guid.NewGuid(), null))).ConfigureAwait(false);
                    }).GetAwaiter().GetResult();

                Assert.AreEqual(
                    "https://login.microsoftonline.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/oauth2/v2.0/authorize",
                    instance.AuthorizationEndpoint);
                Assert.AreEqual(
                    "https://login.microsoftonline.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/oauth2/v2.0/token",
                    instance.TokenEndpoint);
                Assert.AreEqual("https://sts.windows.net/6babcaad-604b-40ac-a9d7-9fd97c0b779f/", instance.SelfSignedJwtAudience);
            }
        }

        [TestMethod]
        [TestCategory("AadAuthorityTests")]
        public void FailedValidationTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);

                //add mock response for instance validation
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url = "https://login.microsoftonline.com/common/discovery/instance",
                        QueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.1"},
                            {
                                "authorization_endpoint",
                                "https%3A%2F%2Flogin.microsoft0nline.com%2Fmytenant.com%2Foauth2%2Fv2.0%2Fauthorize"
                            },
                        },
                        ResponseMessage = MockHelpers.CreateFailureMessage(
                            HttpStatusCode.BadRequest,
                            "{\"error\":\"invalid_instance\"," + "\"error_description\":\"AADSTS50049: " +
                            "Unknown or invalid instance. Trace " + "ID: b9d0894d-a9a4-4dba-b38e-8fb6a009bc00 " +
                            "Correlation ID: 34f7b4cf-4fa2-4f35-a59b" + "-54b6f91a9c94 Timestamp: 2016-08-23 " +
                            "20:45:49Z\",\"error_codes\":[50049]," + "\"timestamp\":\"2016-08-23 20:45:49Z\"," +
                            "\"trace_id\":\"b9d0894d-a9a4-4dba-b38e-8f" + "b6a009bc00\",\"correlation_id\":\"34f7b4cf-" +
                            "4fa2-4f35-a59b-54b6f91a9c94\"}")
                    });

                Authority instance = Authority.CreateAuthority(serviceBundle, "https://login.microsoft0nline.com/mytenant.com", true);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityType, AuthorityType.Aad);
                try
                {
                    Task.Run(
                        async () =>
                        {
                            await instance.ResolveEndpointsAsync(
                                null,
                                new RequestContext(null, new TestLogger(Guid.NewGuid(), null))).ConfigureAwait(false);
                        }).GetAwaiter().GetResult();
                    Assert.Fail("validation should have failed here");
                }
                catch (Exception exc)
                {
                    Assert.IsTrue(exc is TestServiceException);
                    Assert.AreEqual(((TestServiceException)exc).ErrorCode, "invalid_instance");
                }
            }
        }

        [TestMethod]
        [TestCategory("AadAuthorityTests")]
        public void FailedValidationMissingFieldsTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);

                //add mock response for instance validation
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url = "https://login.windows.net/common/discovery/instance",
                        QueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.0"},
                            {"authorization_endpoint", "https://login.microsoft0nline.com/mytenant.com/oauth2/v2.0/authorize"},
                        },
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage("{}")
                    });

                var aadInstanceDiscovery = new AadInstanceDiscovery(httpManager, new TelemetryManager());
                Authority instance = Authority.CreateAuthority(serviceBundle, "https://login.microsoft0nline.com/mytenant.com", true);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityType, AuthorityType.Aad);
                try
                {
                    Task.Run(
                        async () =>
                        {
                            await instance.ResolveEndpointsAsync(
                                null,
                                new RequestContext(null, new TestLogger(Guid.NewGuid(), null))).ConfigureAwait(false);
                        }).GetAwaiter().GetResult();
                    Assert.Fail("validation should have failed here");
                }
                catch (Exception exc)
                {
                    Assert.IsNotNull(exc);
                }
            }
        }

        [TestMethod]
        [TestCategory("AadAuthorityTests")]
        public void FailedTenantDiscoveryMissingEndpointsTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);

                //add mock response for tenant endpoint discovery
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url = "https://login.microsoftonline.in/mytenant.com/v2.0/.well-known/openid-configuration",
                        ResponseMessage =
                            MockHelpers.CreateSuccessResponseMessage(
                                File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("OpenidConfiguration-MissingFields.json")))
                    });

                var aadInstanceDiscovery = new AadInstanceDiscovery(httpManager, new TelemetryManager());
                Authority instance = Authority.CreateAuthority(serviceBundle, "https://login.microsoftonline.in/mytenant.com", false);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityType, AuthorityType.Aad);
                try
                {
                    Task.Run(
                        async () =>
                        {
                            await instance.ResolveEndpointsAsync(
                                null,
                                new RequestContext(null, new TestLogger(Guid.NewGuid(), null))).ConfigureAwait(false);
                        }).GetAwaiter().GetResult();
                    Assert.Fail("validation should have failed here");
                }
                catch (TestClientException exc)
                {
                    Assert.AreEqual(CoreErrorCodes.TenantDiscoveryFailedError, exc.ErrorCode);
                }
            }
        }

        [TestMethod]
        [TestCategory("AadAuthorityTests")]
        public void CanonicalAuthorityInitTest()
        {
            var serviceBundle = ServiceBundle.CreateDefault();

            const string UriNoPort = "https://login.microsoftonline.in/mytenant.com";
            const string UriNoPortTailSlash = "https://login.microsoftonline.in/mytenant.com/";

            const string UriDefaultPort = "https://login.microsoftonline.in:443/mytenant.com";

            const string UriCustomPort = "https://login.microsoftonline.in:444/mytenant.com";
            const string UriCustomPortTailSlash = "https://login.microsoftonline.in:444/mytenant.com/";

            var authority = Authority.CreateAuthority(serviceBundle, UriNoPort, false);
            Assert.AreEqual(UriNoPortTailSlash, authority.CanonicalAuthority);

            authority = Authority.CreateAuthority(serviceBundle, UriDefaultPort, false);
            Assert.AreEqual(UriNoPortTailSlash, authority.CanonicalAuthority);

            authority = Authority.CreateAuthority(serviceBundle, UriCustomPort, false);
            Assert.AreEqual(UriCustomPortTailSlash, authority.CanonicalAuthority);
        }

        /*
        [TestMethod]
        [TestCategory("AadAuthorityTests")]
        public void DeprecatedAuthorityTest()
        {
            const string uriNoPort = "https://login.windows.net/mytenant.com";
            const string uriCustomPort = "https://login.windows.net:444/mytenant.com/";
            var authority = Authority.CreateAuthority(uriNoPort, false);
            Assert.AreEqual("https://login.microsoftonline.com/mytenant.com/", authority.CanonicalAuthority);
            authority = Authority.CreateAuthority(uriCustomPort, false);
            Assert.AreEqual("https://login.microsoftonline.com:444/mytenant.com/", authority.CanonicalAuthority);
            authority = Authority.CreateAuthority("https://login.windows.net/tfp/tenant/policy", false);
            Assert.AreEqual("https://login.microsoftonline.com/tfp/tenant/policy/", authority.CanonicalAuthority);
            authority = Authority.CreateAuthority("https://login.windows.net:444/tfp/tenant/policy", false);
            Assert.AreEqual("https://login.microsoftonline.com:444/tfp/tenant/policy/", authority.CanonicalAuthority);
        }
        */
    }
}