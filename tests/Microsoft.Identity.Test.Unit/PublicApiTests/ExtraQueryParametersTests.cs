// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class ExtraQueryParametersTests
    {

        /// <summary>
        /// Tests that the older WithExtraQueryParameters methods do not affect token caching behavior. This is meant to demonstrate an
        /// issue in those methods: the parameters could change the contents of a token however they wer not included in the cache keys,
        /// leading to potentially invalid tokens being returned from cache when a new request contained different extra query parameters.
        /// 
        /// This test will no longer be needed when the older APIs are removed.
        /// </summary>
        [TestMethod]
        public async Task WithExtraQueryParameters_DeprecatedDoNotAffectTokenCaching_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
#pragma warning disable CS0618 // Type or member is obsolete
                // Create a confidential client application with a default extra query parameter
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                            .WithClientSecret(TestConstants.ClientSecret)
                                                            .WithExtraQueryParameters("app_param=app_value")
                                                            .WithHttpManager(httpManager)
                                                            .BuildConcrete();

                // Step 1: Make a token request with a specific extra query parameter
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "token_with_request_param");
                var result1 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                      .WithExtraQueryParameters("request_param=request_value")
                                      .ExecuteAsync()
                                      .ConfigureAwait(false);

                Assert.AreEqual("token_with_request_param", result1.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                // Step 2: Make another token request with the same extra query parameter
                // Should retrieve token from cache without network call
                var result2 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                      .WithExtraQueryParameters("request_param=request_value")
                                      .ExecuteAsync()
                                      .ConfigureAwait(false);

                Assert.AreEqual("token_with_request_param", result2.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);

                // Step 3: Make a token request with different extra query parameters
                // Should find the cached token, as older WithExtraQueryParameters APIs do not affect caching
                var result3 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                      .WithExtraQueryParameters("different_param=different_value")
                                      .ExecuteAsync()
                                      .ConfigureAwait(false);

                Assert.AreEqual("token_with_request_param", result3.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result3.AuthenticationResultMetadata.TokenSource);

                // Step 4: Make a token request with the default app-level extra query parameters
                // Using authorization code flow to populate user token cache for AcquireTokenSilent test
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(MockHelpers.GetDefaultTokenResponse("token_for_silent_flow"))
                    });

                var result4 = await app.AcquireTokenByAuthorizationCode(
                    TestConstants.s_scope,
                    "some-auth-code")
                    .WithExtraQueryParameters("param_for_silent_flow=silent_value")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("token_for_silent_flow", result4.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result4.AuthenticationResultMetadata.TokenSource);

                // Get the account that was cached
                var accounts = await app.GetAccountsAsync().ConfigureAwait(false);
                var account = accounts.FirstOrDefault();
                Assert.IsNotNull(account, "An account should be present in the cache");

                // Step 5: Test AcquireTokenSilent with the cached account
                // Should retrieve token from cache without network call
                var silentResult = await app.AcquireTokenSilent(TestConstants.s_scope, account)
                                          .WithExtraQueryParameters("param_for_silent_flow=silent_value")
                                          .ExecuteAsync()
                                          .ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete

                Assert.AreEqual("token_for_silent_flow", silentResult.AccessToken);
                Assert.AreEqual(TokenSource.Cache, silentResult.AuthenticationResultMetadata.TokenSource);

                // Verify expected cache state
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
            }
        }

        /// <summary>
        /// Tests the new tuple-based WithExtraQueryParameters method that allows control over which parameters are used in cache keys.
        /// </summary>
        [TestMethod]
        public async Task WithExtraQueryParameters_TupleVersion_ControlsCaching_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                // Create a confidential client application with a default extra query parameter
                // Using the new tuple-based API, specifying that app_param should be included in the cache key
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                        .WithClientSecret(TestConstants.ClientSecret)
                                                        .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                                                        {
                                                    { "app_param", ("app_value", true) }
                                                        })
                                                        .WithHttpManager(httpManager)
                                                        .BuildConcrete();

                // Step 1: Make a token request with a specific extra query parameter included in the cache key
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "token_with_cache_param");
                var result1 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                      .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                                      {
                                  { "req_param", ("req_value", true) } // Include in cache key
                                      })
                                      .ExecuteAsync()
                                      .ConfigureAwait(false);

                Assert.AreEqual("token_with_cache_param", result1.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                // Step 2: Same parameter with includeInCacheKey=true, should use cache
                var result2 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                      .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                                      {
                                  { "req_param", ("req_value", true) } // Same as before, should hit cache
                                      })
                                      .ExecuteAsync()
                                      .ConfigureAwait(false);

                Assert.AreEqual("token_with_cache_param", result2.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);

                // Step 3: Using the same parameter but NOT including it in the cache key
                // This will create a different cache key (without this parameter) and won't match previous entries
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "token_without_cache_param");
                var result3 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                      .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                                      {
                                  { "req_param", ("req_value", false) } // NOT included in cache key
                                      })
                                      .ExecuteAsync()
                                      .ConfigureAwait(false);

                // Should get a new token since cache key is different (doesn't include req_param)
                Assert.AreEqual("token_without_cache_param", result3.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result3.AuthenticationResultMetadata.TokenSource);

                // Step 4: Reusing the same configuration as Step 3 (parameter not in cache key)
                // Should now use the cache since we've stored a token with this cache key
                var result4 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                      .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                                      {
                                  { "req_param", ("req_value", false) } // NOT included in cache key, same as Step 3
                                      })
                                      .ExecuteAsync()
                                      .ConfigureAwait(false);

                Assert.AreEqual("token_without_cache_param", result4.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result4.AuthenticationResultMetadata.TokenSource);

                // Step 5: Using a different value but still not including in cache key
                // Should still hit the same cache entry because the cache key is the same
                var result5 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                      .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                                      {
                                  { "req_param", ("different_value", false) } // Different value but not in cache key
                                      })
                                      .ExecuteAsync()
                                      .ConfigureAwait(false);

                Assert.AreEqual("token_without_cache_param", result5.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result5.AuthenticationResultMetadata.TokenSource);

                // Step 6: Multiple parameters with different cache inclusion settings
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "token_with_mixed_params");
                var result6 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                      .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                                      {
                                  { "cache_param", ("cache_value", true) },   // Include in cache key
                                  { "non_cache_param", ("value1", false) }    // Don't include in cache key
                                      })
                                      .ExecuteAsync()
                                      .ConfigureAwait(false);

                Assert.AreEqual("token_with_mixed_params", result6.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result6.AuthenticationResultMetadata.TokenSource);

                // Step 7: Same cache key parameter but different non-cache parameter, should use cache
                var result7 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                      .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                                      {
                                  { "cache_param", ("cache_value", true) },    // Same cached parameter
                                  { "non_cache_param", ("value2", false) }     // Different value, not in cache key
                                      })
                                      .ExecuteAsync()
                                      .ConfigureAwait(false);

                Assert.AreEqual("token_with_mixed_params", result7.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result7.AuthenticationResultMetadata.TokenSource);

                // Step 8: Different cache key parameter, should make a new request
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(token: "token_with_different_cache_param");
                var result8 = await app.AcquireTokenForClient(TestConstants.s_scope)
                                      .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                                      {
                                  { "cache_param", ("different_value", true) },  // Different value, included in cache key
                                  { "non_cache_param", ("value1", false) }        // Same as before, not in cache key
                                      })
                                      .ExecuteAsync()
                                      .ConfigureAwait(false);

                Assert.AreEqual("token_with_different_cache_param", result8.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result8.AuthenticationResultMetadata.TokenSource);

                // Step 9: Use AcquireTokenByAuthorizationCode with tuple-based query parameters
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(MockHelpers.GetDefaultTokenResponse("token_from_auth_code"))
                    });

                var result9 = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, "some-auth-code")
                    .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                    {
                { "auth_code_param", ("auth_code_value", true) },   // Include in cache key
                { "transient_param", ("transient_value", false) }   // Don't include in cache key
                    })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("token_from_auth_code", result9.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result9.AuthenticationResultMetadata.TokenSource);

                // Get the account that was cached
                var accounts = await app.GetAccountsAsync().ConfigureAwait(false);
                var account = accounts.FirstOrDefault();
                Assert.IsNotNull(account, "An account should be present in the cache after auth code flow");

                // Step 10: Test AcquireTokenSilent with the same cache key parameters but different non-cache parameters
                var silentResult1 = await app.AcquireTokenSilent(TestConstants.s_scope, account)
                    .WithExtraQueryParameters(new Dictionary<string, (string value, bool includeInCacheKey)>
                    {
                { "auth_code_param", ("auth_code_value", true) },     // Same as auth code flow
                { "transient_param", ("different_value", false) }     // Different value, not in cache key
                    })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Should get token from cache since cache key parameters match
                Assert.AreEqual("token_from_auth_code", silentResult1.AccessToken);
                Assert.AreEqual(TokenSource.Cache, silentResult1.AuthenticationResultMetadata.TokenSource);

                // Verify final cache state
                Assert.AreEqual(4, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count, "Should have 4 app tokens in cache");
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count, "Should have 2 user tokens in cache");
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count, "Should have 1 refresh token in cache");
            }
        }
    }
}
