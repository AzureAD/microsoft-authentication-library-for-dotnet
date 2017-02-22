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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Test.ADAL.Common;
using Test.ADAL.Common.Unit;
using Test.ADAL.NET.Unit.Mocks;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    [DeploymentItem("valid_cert.pfx")]
    public class AdalDotNetTests
    {
        private PlatformParameters platformParameters;

        [TestInitialize]
        public void Initialize()
        {
            HttpMessageHandlerFactory.ClearMockHandlers();
            platformParameters = new PlatformParameters(PromptBehavior.Auto);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken")]
        [TestCategory("AdalDotNet")]
        public async Task SmokeTest()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"));
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, true);
            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, platformParameters);
            Assert.IsNotNull(result);
            Assert.IsTrue(context.Authenticator.Authority.EndsWith("/some-tenant-id/"));
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual(result.ExpiresOn, result.ExtendedExpiresOn);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, result.UserInfo.UniqueId);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken with extended expires on support")]
        [TestCategory("AdalDotNet")]
        public async Task SmokeTestWithExtendedExpiresOn()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"));
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(true)
            });

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, true);
            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, platformParameters);
            Assert.IsNotNull(result);
            Assert.IsTrue(context.Authenticator.Authority.EndsWith("/some-tenant-id/"));
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            Assert.IsTrue(result.ExtendedExpiresOn.Subtract(result.ExpiresOn) > TimeSpan.FromSeconds(5));
            Assert.AreEqual(TestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, result.UserInfo.UniqueId);
        }

        [TestMethod]
        [Description("Positive Test for ExtendedLife Feature")]
        [TestCategory("AdalDotNet")]
        public async Task ExtendedLifetimeRetry()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                 TestConstants.DefaultRedirectUri + "?code=some-code"));
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, true);
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateResiliencyMessage(HttpStatusCode.GatewayTimeout),
            });
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
            });
            Assert.AreEqual(HttpMessageHandlerFactory.MockHandlersCount(), 2);
            context.ExtendedLifeTimeEnabled = true;
            AuthenticationResult result =
            await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,TestConstants.DefaultRedirectUri, platformParameters);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(HttpMessageHandlerFactory.MockHandlersCount(),0);
        }

        [TestMethod]
        [Description("Positive Test for ExtendedLife Feature returning back a stale AT")]
        [TestCategory("AdalDotNet")]
        public async Task ExtendedLifetimePositiveTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", "some-access-token", DateTimeOffset.UtcNow , (DateTimeOffset.UtcNow + TimeSpan.FromMinutes(180)))
            };

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateResiliencyMessage(HttpStatusCode.GatewayTimeout),
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
            });
            Assert.AreEqual(HttpMessageHandlerFactory.MockHandlersCount(), 2);
            context.ExtendedLifeTimeEnabled = true;
            AuthenticationResult result =
                    await context.AcquireTokenSilentAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, new UserIdentifier("unique_id", UserIdentifierType.UniqueId));
            Assert.IsNotNull(result);
            Assert.AreEqual(HttpMessageHandlerFactory.MockHandlersCount(), 0);
            Assert.AreEqual(result.AccessToken, "some-access-token");
        }

        [TestMethod]
        [Description("Expiry time test for ExtendedLife Feature not returning back a stale AT")]
        [TestCategory("AdalDotNet")]
        public async Task ExtendedLifetimeExpiredTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", "some-access-token", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
            };

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateResiliencyMessage(HttpStatusCode.GatewayTimeout),
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateResiliencyMessage(HttpStatusCode.GatewayTimeout),
            });

            Assert.AreEqual(HttpMessageHandlerFactory.MockHandlersCount(), 2);
            context.ExtendedLifeTimeEnabled = true;
            AuthenticationResult result =
                 await context.AcquireTokenSilentAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, new UserIdentifier("unique_id", UserIdentifierType.UniqueId));
            Assert.IsNull(result.AccessToken);
            Assert.AreEqual(HttpMessageHandlerFactory.MockHandlersCount(), 0);
        }


        [TestMethod]
        [Description("Test for returning back a stale AT")]
        [TestCategory("AdalDotNet")]
        public async Task ExtendedLifetimeTokenTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", "some-access-token", DateTimeOffset.UtcNow, (DateTimeOffset.UtcNow + TimeSpan.FromMinutes(180)))
            };

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateResiliencyMessage(HttpStatusCode.GatewayTimeout),
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateResiliencyMessage(HttpStatusCode.InternalServerError),
            });

            //Assert.AreEqual(HttpMessageHandlerFactory.MockHandlersCount(), 2);
            context.ExtendedLifeTimeEnabled = true;
            AuthenticationResult result =
                await
                    context.AcquireTokenSilentAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        new UserIdentifier("unique_id", UserIdentifierType.UniqueId));

            Assert.IsNotNull(result);
            Assert.IsFalse(result.ExpiresOn <=
                           DateTime.UtcNow);
            Assert.AreEqual(result.AccessToken, "some-access-token");
        }

        [TestMethod]
        [Description("Test for returning back a stale AT in case of Network failure")]
        [TestCategory("AdalDotNet")]
        public async Task ExtendedLifetimeRequestTimeoutTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", "some-access-token", DateTimeOffset.UtcNow, (DateTimeOffset.UtcNow + TimeSpan.FromMinutes(180)))
            };

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ExceptionToThrow = new TaskCanceledException("request timed out")
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ExceptionToThrow = new TaskCanceledException("request timed out")
            });
            context.ExtendedLifeTimeEnabled = true;
            AuthenticationResult result =
                await
                    context.AcquireTokenSilentAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        new UserIdentifier("unique_id", UserIdentifierType.UniqueId));

            Assert.IsNotNull(result);
            Assert.IsFalse(result.ExpiresOn <=
                           DateTime.UtcNow);
            Assert.AreEqual(result.AccessToken, "some-access-token");

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        [Description("Test for ExtendedLifetime feature flag being set in normal(non-outage) for Client Credentials")]
        public async Task ClientCredentialExtendedExpiryFlagSet()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            var credential = new ClientCredential(TestConstants.DefaultClientId, TestConstants.DefaultClientSecret);

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"client_secret", TestConstants.DefaultClientSecret},
                    {"grant_type", "client_credentials"}
                }
            });

            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            Assert.IsNotNull(result.AccessToken);

            // cache look up
            var result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            Assert.AreEqual(result.AccessToken, result2.AccessToken);

            // Null resource -> error
            var exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(null, credential));
            Assert.AreEqual(exc.ParamName, "resource");

            // Null client credential -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientCredential)null));
            Assert.AreEqual(exc.ParamName, "clientCredential");
        }

        [TestMethod]
        [Description("Test for ExtendedLifetime feature flag being not set in normal(non-outage) for Client Credentials")]
        public async Task ClientCredentialExtendedExpiryFlagNotSet()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            var credential = new ClientCredential(TestConstants.DefaultClientId, TestConstants.DefaultClientSecret);

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"client_secret", TestConstants.DefaultClientSecret},
                    {"grant_type", "client_credentials"}
                }
            });

            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            Assert.IsNotNull(result.AccessToken);

            // cache look up
            var result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            Assert.AreEqual(result.AccessToken, result2.AccessToken);

            // Null resource -> error
            var exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(null, credential));
            Assert.AreEqual(exc.ParamName, "resource");

            // Null client credential -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientCredential)null));
            Assert.AreEqual(exc.ParamName, "clientCredential");
        }

        [TestMethod]
        [Description("Test for getting back access token when the extendedExpiresOn flag is set")]
        public async Task ClientCredentialExtendedExpiryPositiveTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, new TokenCache());
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.Client,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", "some-access-token", DateTimeOffset.UtcNow, (DateTimeOffset.UtcNow + TimeSpan.FromMinutes(180)))
            };

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"client_secret", TestConstants.DefaultClientSecret},
                    {"grant_type", "client_credentials"}
                }
            });
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"client_secret", TestConstants.DefaultClientSecret},
                    {"grant_type", "client_credentials"}
                }
            });

            var credential = new ClientCredential(TestConstants.DefaultClientId, TestConstants.DefaultClientSecret);

            context.ExtendedLifeTimeEnabled = true;
            // cache look up
            var result = await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            Assert.IsNotNull(result.AccessToken);

            // Null resource -> error
            var exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(null, credential));
            Assert.AreEqual(exc.ParamName, "resource");

            // Null client credential -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientCredential)null));
            Assert.AreEqual(exc.ParamName, "clientCredential");
        }

        [TestMethod]
        [Description("Test for ExtendedLifetime feature with the extendedExpiresOn being expired not returning back stale AT")]
        public void ClientCredentialExtendedExpiryNegativeTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityCommonTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", "some-access-token", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
            };

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"client_secret", TestConstants.DefaultClientSecret},
                    {"grant_type", "client_credentials"}
                }
            });
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"client_secret", TestConstants.DefaultClientSecret},
                    {"grant_type", "client_credentials"}
                }
            });

            Assert.AreEqual(HttpMessageHandlerFactory.MockHandlersCount(), 2);

            var credential = new ClientCredential(TestConstants.DefaultClientId, TestConstants.DefaultClientSecret);


            // cache look up
            var ex = AssertException.TaskThrows<AdalServiceException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, credential));
            Assert.AreEqual(ex.InnerException.Message, " Response status code does not indicate success: 504 (GatewayTimeout).");

            // Null resource -> error
            var exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(null, credential));
            Assert.AreEqual(exc.ParamName, "resource");

            // Null client credential -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientCredential)null));
            Assert.AreEqual(exc.ParamName, "clientCredential");
        }

        [TestMethod]
        [Description("Test for ExtendedLifetime feature with the extendedExpiresOn being expired not returning back stale AT")]
        public void ClientCredentialNegativeRequestTimeoutTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityCommonTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", "some-access-token", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
            };

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"client_secret", TestConstants.DefaultClientSecret},
                    {"grant_type", "client_credentials"}
                }
            });
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"client_secret", TestConstants.DefaultClientSecret},
                    {"grant_type", "client_credentials"}
                }
            });

            Assert.AreEqual(HttpMessageHandlerFactory.MockHandlersCount(), 2);

            var credential = new ClientCredential(TestConstants.DefaultClientId, TestConstants.DefaultClientSecret);

            context.ExtendedLifeTimeEnabled = true;
            // cache look up
            var ex = AssertException.TaskThrows<AdalServiceException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, credential));
            Assert.AreEqual(ex.InnerException.Message, " Response status code does not indicate success: 408 (RequestTimeout).");


            // Null resource -> error
            ArgumentNullException exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(null, credential));
            Assert.AreEqual(exc.ParamName, "resource");

            // Null client credential -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientCredential)null));
            Assert.AreEqual(exc.ParamName, "clientCredential");
        }

        [TestMethod]
        [Description("Test for being in outage mode and extendedExpires flag not set")]
        public void ClientCredentialExtendedExpiryNoFlagSetTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityCommonTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", "some-access-token", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
            };

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"client_secret", TestConstants.DefaultClientSecret},
                    {"grant_type", "client_credentials"}
                }
            });
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"client_secret", TestConstants.DefaultClientSecret},
                    {"grant_type", "client_credentials"}
                }
            });

            Assert.AreEqual(HttpMessageHandlerFactory.MockHandlersCount(), 2);

            var credential = new ClientCredential(TestConstants.DefaultClientId, TestConstants.DefaultClientSecret);

            context.ExtendedLifeTimeEnabled = false;
            // cache look up
            var ex = AssertException.TaskThrows<AdalServiceException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, credential));
            Assert.AreEqual(ex.InnerException.Message, " Response status code does not indicate success: 504 (GatewayTimeout).");

            // Null resource -> error
            var exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(null, credential));
            Assert.AreEqual(exc.ParamName, "resource");

            // Null client credential -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientCredential)null));
            Assert.AreEqual(exc.ParamName, "clientCredential");
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken with missing redirectUri and/or userId")]
        public async Task AcquireTokenPositiveWithoutUserIdAsync()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"));
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, platformParameters);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.AccessToken, "some-access-token");

            var exc = AssertException.TaskThrows<ArgumentException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                    TestConstants.DefaultRedirectUri, platformParameters, null));
            Assert.IsTrue(exc.Message.StartsWith(AdalErrorMessage.SpecifyAnyUser));


            // this should hit the cache
            result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, platformParameters, UserIdentifier.AnyUser);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.AccessToken, "some-access-token");
        }

        [TestMethod]
        [Description("Test for autority validation to AuthenticationContext")]
        public async Task AuthenticationContextAuthorityValidationTestAsync()
        {
            AuthenticationContext context = null;
            AuthenticationResult result = null;

            var ex = AssertException.Throws<ArgumentException>(() => new AuthenticationContext("https://login.contoso.com/adfs"));
            Assert.AreEqual(ex.ParamName, "validateAuthority");


            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"));
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            //whitelisted authority
            context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, true);
            result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, platformParameters,
                        new UserIdentifier(TestConstants.DefaultDisplayableId, UserIdentifierType.RequiredDisplayableId));
            Assert.IsNotNull(result);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            //add handler to return failed discovery response
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage =
                    MockHelpers.CreateFailureResponseMessage(
                        "{\"error\":\"invalid_instance\",\"error_description\":\"AADSTS70002: Error in validating authority.\"}")
            });

            context = new AuthenticationContext("https://login.microsoft0nline.com/common");
            var adalEx = AssertException.TaskThrows<AdalException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                    TestConstants.DefaultRedirectUri, platformParameters));

            Assert.AreEqual(adalEx.ErrorCode, AdalError.AuthorityNotInValidList);
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with invalid resource")]
        public void AcquireTokenWithInvalidResourceTestAsync()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", "some-access-token", DateTimeOffset.UtcNow)
            };

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateInvalidRequestTokenResponseMessage()
            });

            var exc = AssertException.TaskThrows<AdalServiceException>(() =>
                context.AcquireTokenSilentAsync("random-resource", TestConstants.DefaultClientId));
            Assert.AreEqual(AdalError.FailedToRefreshToken, exc.ErrorCode);
        }

        [TestMethod]
        [Description("Negative Test for AcquireToken with user canceling authentication")]
        public void AcquireTokenWithAuthenticationCanceledTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.UserCancel,
                TestConstants.DefaultRedirectUri + "?error=user_cancelled"));

            var exc = AssertException.TaskThrows<AdalServiceException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, platformParameters));

            Assert.AreEqual(exc.ErrorCode, AdalError.AuthenticationCanceled);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken testing default token cache")]
        public async Task AcquireTokenPositiveWithNullCacheTest()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"));
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, null);
            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, platformParameters);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
        }


        [TestMethod]
        [Description("Test for simple refresh token")]
        public void SimpleRefreshTokenTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAdfsAuthorityTenant, false, new TokenCache());
            //add simple RT to cache
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAdfsAuthorityTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User, null, null);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "some-rt",
                Result = new AuthenticationResult("Bearer", "some-access-token", DateTimeOffset.UtcNow)
            };

            //token request for some other resource should fail.
            var exc = AssertException.TaskThrows<AdalSilentTokenAcquisitionException>(() =>
                context.AcquireTokenSilentAsync("random-resource", TestConstants.DefaultClientId));
            Assert.AreEqual(AdalError.FailedToAcquireTokenSilently, exc.ErrorCode);
        }

        [TestMethod]
        [Description("Test for acquring token using tenant specific endpoint")]
        public async Task TenantSpecificAuthorityTest()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"));
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, true);
            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, platformParameters);
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, result.UserInfo.UniqueId);
        }

        [TestMethod]
        [Description("Test for Force Prompt")]
        public async Task ForcePromptTestAsync()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"));
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"grant_type", "authorization_code"}
                }
            });

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, true);
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", "existing-access-token", DateTimeOffset.UtcNow)
            };

            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, new PlatformParameters(PromptBehavior.Always));
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, result.UserInfo.UniqueId);
        }

        [TestMethod]
        [Description("Positive Test for AcquireToken non-interactive")]
        public async Task AcquireTokenNonInteractivePositiveTestAsync()
        {
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content =
                        new StringContent("{\"ver\":\"1.0\",\"account_type\":\"Managed\",\"domain_name\":\"id.com\"}")
                },
                QueryParams = new Dictionary<string, string>()
                {
                    {"api-version", "1.0"}
                }
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"grant_type", "password"},
                    {"username", TestConstants.DefaultDisplayableId},
                    {"password", TestConstants.DefaultPassword}
                }
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content =
            new StringContent("{\"ver\":\"1.0\",\"account_type\":\"Managed\",\"domain_name\":\"id.com\"}")
                },
                QueryParams = new Dictionary<string, string>()
                {
                    {"api-version", "1.0"}
                }
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.DefaultUniqueId, "user2@id.com", TestConstants.DefaultResource),
                PostData = new Dictionary<string, string>()
                {
                    {"grant_type", "password"},
                    {"username", "user2@id.com"},
                    {"password", TestConstants.DefaultPassword}
                }
            });

            TokenCache cache = new TokenCache();

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, true, cache);
            var result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        new UserPasswordCredential(TestConstants.DefaultDisplayableId, TestConstants.DefaultPassword));
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, result.UserInfo.UniqueId);

            context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, true, cache);
            result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        new UserPasswordCredential("user2@id.com", TestConstants.DefaultPassword));
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
            Assert.AreEqual(result.AccessToken, "some-access-token");
            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual("user2@id.com", result.UserInfo.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, result.UserInfo.UniqueId);
        }

        [TestMethod]
        [Description("Positive Test for Confidential Client")]
        public async Task ConfidentialClientWithX509Test()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            var certificate = new ClientAssertionCertificate(TestConstants.DefaultClientId,
                new X509Certificate2("valid_cert.pfx", TestConstants.DefaultPassword));

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"}
                }
            });

            AuthenticationResult result =
                await
                    context.AcquireTokenByAuthorizationCodeAsync("some-code", TestConstants.DefaultRedirectUri,
                        certificate, TestConstants.DefaultResource);
            Assert.IsNotNull(result.AccessToken);


            // Null auth code -> error
            var exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenByAuthorizationCodeAsync(null, TestConstants.DefaultRedirectUri, certificate,
                        TestConstants.DefaultResource));

            Assert.AreEqual(exc.ParamName, "authorizationCode");

            // Empty auth code -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenByAuthorizationCodeAsync(string.Empty, TestConstants.DefaultRedirectUri,
                certificate,
                TestConstants.DefaultResource));

            Assert.AreEqual(exc.ParamName, "authorizationCode");


            // Null for redirect -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenByAuthorizationCodeAsync("some-code", null, certificate,
                        TestConstants.DefaultResource));
            Assert.AreEqual(exc.ParamName, "redirectUri");

            // Null client certificate -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenByAuthorizationCodeAsync("some-code", TestConstants.DefaultRedirectUri,
                        (ClientAssertionCertificate)null, TestConstants.DefaultResource));
            Assert.AreEqual(exc.ParamName, "clientCertificate");

        }

        [TestMethod]
        [Description("Test for Client credential")]
        public async Task ClientCredentialNoCrossTenantTestAsync()
        {
            TokenCache cache = new TokenCache();
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, cache);
            var credential = new ClientCredential(TestConstants.DefaultClientId, TestConstants.DefaultClientSecret);

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"client_secret", TestConstants.DefaultClientSecret},
                    {"grant_type", "client_credentials"}
                }
            });

            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            Assert.IsNotNull(result.AccessToken);

            context = new AuthenticationContext(TestConstants.DefaultAuthorityGuestTenant, cache);


            AssertException.TaskThrows<AdalException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, credential));
            Assert.AreEqual(1, cache.tokenCacheDictionary.Count);
        }

        [TestMethod]
        [Description("Test for Client credential")]
        public async Task ClientCredentialTestAsync()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            var credential = new ClientCredential(TestConstants.DefaultClientId, TestConstants.DefaultClientSecret);

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"client_secret", TestConstants.DefaultClientSecret},
                    {"grant_type", "client_credentials"}
                }
            });

            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            Assert.IsNotNull(result.AccessToken);

            // cache look up
            var result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, credential);
            Assert.AreEqual(result.AccessToken, result2.AccessToken);

            // Null resource -> error
            var exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(null, credential));
            Assert.AreEqual(exc.ParamName, "resource");

            // Null client credential -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientCredential)null));
            Assert.AreEqual(exc.ParamName, "clientCredential");
        }

        [TestMethod]
        [Description("Test for Client assertion with X509")]
        public async Task ClientAssertionWithX509Test()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            var certificate = new ClientAssertionCertificate(TestConstants.DefaultClientId, new X509Certificate2("valid_cert.pfx", TestConstants.DefaultPassword));

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"grant_type", "client_credentials"}
                }
            });

            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, certificate);
            Assert.IsNotNull(result.AccessToken);


            // Null resource -> error
            var exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(null, certificate));
            Assert.AreEqual(exc.ParamName, "resource");

            // Null client credential -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientCredential)null));

            Assert.AreEqual(exc.ParamName, "clientCredential");
        }


        [TestMethod]
        [Description("Test for Confidential Client with self signed jwt")]
        public async Task ConfidentialClientWithJwtTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"}
                }
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"}
                }
            });

            ClientAssertion assertion = new ClientAssertion(TestConstants.DefaultClientId, "some-assertion");
            AuthenticationResult result = await context.AcquireTokenByAuthorizationCodeAsync("some-code", TestConstants.DefaultRedirectUri, assertion, TestConstants.DefaultResource);
            Assert.IsNotNull(result.AccessToken);

            result = await context.AcquireTokenByAuthorizationCodeAsync("some-code", TestConstants.DefaultRedirectUri, assertion, null);
            Assert.IsNotNull(result.AccessToken);

            // Empty authorization code -> error
            var exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenByAuthorizationCodeAsync(string.Empty, TestConstants.DefaultRedirectUri, assertion, TestConstants.DefaultResource));
            Assert.AreEqual(exc.ParamName, "authorizationCode");


            // Null authorization code -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenByAuthorizationCodeAsync(null, TestConstants.DefaultRedirectUri, assertion, TestConstants.DefaultResource));
            Assert.AreEqual(exc.ParamName, "authorizationCode");


            // Null redirectUri -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenByAuthorizationCodeAsync("some-code", null, assertion, TestConstants.DefaultResource));
            Assert.AreEqual(exc.ParamName, "redirectUri");


            // Null client assertion -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenByAuthorizationCodeAsync("some-code", TestConstants.DefaultRedirectUri, (ClientAssertion)null, TestConstants.DefaultResource));
            Assert.AreEqual(exc.ParamName, "clientAssertion");
        }

        [TestMethod]
        [Description("Positive Test for AcquireTokenOnBehalf with client credential")]
        public async Task AcquireTokenOnBehalfAndClientCredentialTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            string accessToken = "some-access-token";
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", accessToken, DateTimeOffset.UtcNow)
            };

            ClientCredential clientCredential = new ClientCredential(TestConstants.DefaultClientId, TestConstants.DefaultClientSecret);

            // Null resource -> error
            var exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(null, clientCredential, new UserAssertion(accessToken)));
            Assert.AreEqual(exc.ParamName, "resource");


            // Null user assertion -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential, null));
            Assert.AreEqual(exc.ParamName, "userAssertion");


            // Null client credential -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientCredential)null, new UserAssertion(accessToken)));
            Assert.AreEqual(exc.ParamName, "clientCredential");


            // Valid input -> no error
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-other-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
            });

            var result = await context.AcquireTokenAsync(TestConstants.AnotherResource, clientCredential, new UserAssertion(accessToken));
            Assert.IsNotNull(result.AccessToken);
        }

        [TestMethod]
        [Description("Positive Test for AcquireTokenOnBehalf with client credential")]
        public async Task AcquireTokenOnBehalfAndClientCertificateCredentialTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            string accessToken = "some-access-token";
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", accessToken, DateTimeOffset.UtcNow)
            };

            ClientAssertionCertificate clientCredential = new ClientAssertionCertificate(TestConstants.DefaultClientId, new X509Certificate2("valid_cert.pfx", TestConstants.DefaultPassword));

            // Null resource -> error
            var exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(null, clientCredential, new UserAssertion(accessToken)));
            Assert.AreEqual(exc.ParamName, "resource");


            // Null user assertion -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, clientCredential, null));
            Assert.AreEqual(exc.ParamName, "userAssertion");


            // Null client cert -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientAssertionCertificate)null, new UserAssertion(accessToken)));
            Assert.AreEqual(exc.ParamName, "clientCertificate");


            // Valid input -> no error
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-other-token\"}")
                },
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"}
                }
            });

            var result = await context.AcquireTokenAsync(TestConstants.AnotherResource, clientCredential, new UserAssertion(accessToken));
            Assert.IsNotNull(result.AccessToken);
        }

        [TestMethod]
        [Description("Test for GetAuthorizationRequestURL")]
        public async Task GetAuthorizationRequestUrlTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant);
            Uri uri = null;

            var ex = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.GetAuthorizationRequestUrlAsync(null, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, new UserIdentifier(TestConstants.DefaultDisplayableId, UserIdentifierType.RequiredDisplayableId), "extra=123"));
            Assert.AreEqual(ex.ParamName, "resource");

            uri = await context.GetAuthorizationRequestUrlAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, new UserIdentifier(TestConstants.DefaultDisplayableId, UserIdentifierType.RequiredDisplayableId), "extra=123");
            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.AbsoluteUri.Contains("login_hint"));
            Assert.IsTrue(uri.AbsoluteUri.Contains("extra=123"));
            uri = await context.GetAuthorizationRequestUrlAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, UserIdentifier.AnyUser, null);
            Assert.IsNotNull(uri);
            Assert.IsFalse(uri.AbsoluteUri.Contains("login_hint"));
            Assert.IsFalse(uri.AbsoluteUri.Contains("client-request-id="));
            context.CorrelationId = Guid.NewGuid();
            uri = await context.GetAuthorizationRequestUrlAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, new UserIdentifier(TestConstants.DefaultDisplayableId, UserIdentifierType.RequiredDisplayableId), "extra");
            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.AbsoluteUri.Contains("client-request-id="));
        }
        

        [TestMethod]
        [Description("Positive Test for AcquireTokenOnBehalf with client credential")]
        public void UserAssertionValidationTest()
        {
            TokenCache cache = new TokenCache();
            AuthenticationResultEx resultEx = TokenCacheTests.CreateCacheValue("id", "user1");
            resultEx.UserAssertionHash = "hash1";
            cache.tokenCacheDictionary.Add(
            new TokenCacheKey("https://localhost/MockSts/", "resource1", "client1",
                TokenSubjectType.Client, "id", "user1"), resultEx);
            RequestData data = new RequestData
            {
                Authenticator = new Authenticator("https://localhost/MockSts/", false),
                TokenCache = cache,
                Resource = "resource1",
                ClientKey = new ClientKey(new ClientCredential("client1", "something")),
                SubjectType = TokenSubjectType.Client,
                ExtendedLifeTimeEnabled = false
            };


            AssertException.TaskThrows<HttpRequestException>(() =>
                    new AcquireTokenOnBehalfHandler(data, new UserAssertion("non-existant")).RunAsync()
                );
        }

        [TestMethod]
        [Description("Test for returning entire HttpResponse as inner exception")]
        public void HttpErrorResponseAsInnerException()
        {
            TokenCache cache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityCommonTenant, TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User, "unique_id", "displayable@id.com");
            cache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "something-invalid",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", "some-access-token", DateTimeOffset.UtcNow)
            };

            AuthenticationContext context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, cache);

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateHttpErrorResponse()
            });

            var ex = AssertException.TaskThrows<AdalSilentTokenAcquisitionException>(() =>
                context.AcquireTokenSilentAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, new UserIdentifier("unique_id", UserIdentifierType.UniqueId)));
            Assert.IsTrue((ex.InnerException.InnerException.InnerException).Message.Contains(TestConstants.ErrorSubCode));
        }
    }
}
