// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class OboRequestTests
    {
        private SecureString _secureString;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        private MockHttpMessageHandler AddMockHandlerDefaultUserRealmDiscovery(MockHttpManager httpManager)
        {
            var handler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                            "{\"ver\":\"1.0\",\"account_type\":\"federated\",\"domain_name\":\"microsoft.com\"," +
                            "\"federation_protocol\":\"WSTrust\",\"federation_metadata_url\":" +
                            "\"https://msft.sts.microsoft.com/adfs/services/trust/mex\"," +
                            "\"federation_active_auth_url\":\"https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed\"" +
                            ",\"cloud_instance_name\":\"login.microsoftonline.com\"}")
                }
            };

            // user realm discovery
            httpManager.AddMockHandler(handler);
            return handler;
        }

        private void AddMockHandlerDefaultUserRealmDiscovery_ManagedUser(MockHttpManager httpManager)
        {
            // user realm discovery
            httpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Get,
                    ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\"ver\":\"1.0\"," +
                            "\"account_type\":\"Managed\"," +
                            "\"domain_name\":\"some_domain.onmicrosoft.com\"," +
                            "\"cloud_audience_urn\":\"urn:federation:MicrosoftOnline\"," +
                            "\"cloud_instance_name\":\"login.microsoftonline.com\"}")
                    }
                });
        }

        private MockHttpMessageHandler AddMockHandlerAadSuccess(MockHttpManager httpManager, string authority)
        {
            var handler = new MockHttpMessageHandler
            {
                ExpectedUrl = authority + "oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            };
            httpManager.AddMockHandler(handler);

            return handler;
        }
        
        [TestMethod]
        public async Task AcquireTokenByOboHappyPathTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);

                var cca = ConfidentialClientApplicationBuilder
                                                         .Create(TestConstants.ClientId)
                                                         .WithClientSecret(TestConstants.ClientSecret)
                                                         .WithAuthority(TestConstants.AuthorityCommonTenant)
                                                         .WithHttpManager(httpManager)
                                                         .BuildConcrete();

                UserAssertion userAssertion = new UserAssertion(TestConstants.FormattedAccessToken);
                var result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_graphScopes, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);

                //Expire access tokens
                TokenCacheHelper.ExpireAccessTokens(cca.UserTokenCacheInternal);

                MockHttpMessageHandler mockTokenRequestHttpHandlerRefresh = AddMockHandlerAadSuccess(httpManager, TestConstants.AuthorityCommonTenant);
                mockTokenRequestHttpHandlerRefresh.ExpectedPostData = new Dictionary<string, string> { { "grant_type", "refresh_token" } };

                result = await cca.AcquireTokenOnBehalfOf(TestConstants.s_graphScopes, userAssertion).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
            }
        }

    
    }
}
