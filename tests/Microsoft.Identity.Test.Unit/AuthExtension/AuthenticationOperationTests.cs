// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.AuthExtension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CDT
{
    [TestClass]
    public class AuthenticationOperationTests : TestBase
    {
        private const string ProtectedUrl = "https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b";

        [TestMethod]
        [DeploymentItem(@"Resources\testCert.crtfile")]
        public async Task AuthenticationOperationTest_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithExperimentalFeatures(true)
                                                              .BuildConcrete();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(ProtectedUrl));

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseWithAdditionalParamsMessage(tokenType: "someAccessTokenType");

                MsalAuthenticationExtension cdtExtension = new MsalAuthenticationExtension()
                {
                    AuthenticationOperation = new MsalTestAuthenticationOperation(),
                    AdditionalCacheParameters = new[] { "additional_param1", "additional_param2" }
                };

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid)
                    .WithAuthenticationExtension(cdtExtension)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.IsTrue(result.AdditionalResponseParameters.Keys.Contains("additional_param1"));
                Assert.IsTrue(result.AdditionalResponseParameters.Keys.Contains("additional_param2"));
                var expectedAt = "header.payload.signature"
                                 + "AccessTokenModifier"
                                 + result.AdditionalResponseParameters["additional_param1"]
                                 + result.AdditionalResponseParameters["additional_param2"];
                Assert.AreEqual(expectedAt, result.AccessToken);

                //Verify that the original AT token is cached and the CDT can be recreated
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid)
                    .WithAuthenticationExtension(cdtExtension)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

                Assert.IsTrue(result.AdditionalResponseParameters.Keys.Contains("additional_param1"));
                Assert.IsTrue(result.AdditionalResponseParameters.Keys.Contains("additional_param2"));
                Assert.AreEqual(expectedAt, result.AccessToken);
            }
        }
    }
}
