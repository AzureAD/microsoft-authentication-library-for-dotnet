// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class AppTokenProviderTests : TestBase
    {
        [TestMethod]
        public async Task ValidateAppTokenProviderAsync()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                bool usingClaims = false;
                string differentScopesForAt = string.Empty;
                int callbackInvoked = 0;
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAppTokenProvider((AppTokenProviderParameters parameters) =>
                                                              {
                                                                  Assert.IsNotNull(parameters.Scopes);
                                                                  Assert.IsNotNull(parameters.CorrelationId);
                                                                  Assert.IsNotNull(parameters.TenantId);
                                                                  Assert.IsNotNull(parameters.CancellationToken);

                                                                  if (usingClaims)
                                                                  {
                                                                      Assert.IsNotNull(parameters.Claims);
                                                                  }

                                                                  Interlocked.Increment(ref callbackInvoked);

                                                                  return Task.FromResult(GetAppTokenProviderResult(differentScopesForAt));
                                                              })
                                                              .WithHttpManager(harness.HttpManager)
                                                              .BuildConcrete();

                // AcquireToken from app provider
                AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                                                        .ExecuteAsync(new CancellationToken()).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.DefaultAccessToken, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, callbackInvoked);

                var tokens = app.AppTokenCacheInternal.Accessor.GetAllAccessTokens();

                Assert.AreEqual(1, tokens.Count);

                var token = tokens.FirstOrDefault();
                Assert.IsNotNull(token);
                Assert.AreEqual(TestConstants.DefaultAccessToken, token.Secret);

                // AcquireToken from cache
                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                                                        .ExecuteAsync(new CancellationToken()).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.DefaultAccessToken, result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, callbackInvoked);

                // Expire token
                TokenCacheHelper.ExpireAllAccessTokens(app.AppTokenCacheInternal);

                // Acquire token from app provider with expired token
                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                                                        .ExecuteAsync(new CancellationToken()).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.DefaultAccessToken, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(2, callbackInvoked);

                differentScopesForAt = "new scope";

                // Acquire token from app provider with new scopes
                result = await app.AcquireTokenForClient(new[] { differentScopesForAt })
                                                        .ExecuteAsync(new CancellationToken()).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.DefaultAccessToken + differentScopesForAt, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count, 2);
                Assert.AreEqual(3, callbackInvoked);

                // Acquire token from app provider with claims. Should not use cache
                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                                                        .WithClaims(TestConstants.Claims)
                                                        .ExecuteAsync(new CancellationToken()).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.DefaultAccessToken + differentScopesForAt, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(4, callbackInvoked);
            }
        }

        [DataTestMethod]
        [DataRow(3600, 0, 0)]
        [DataRow(3600, 500, 500)]
        [DataRow(7200, 0, 3600)]
        [DataRow(7200, 500, 500)]
        public async Task CheckRefreshInAsync(long expiresInResponse, long refreshInResponse, long expectedRefreshIn)
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                string differentScopesForAt = string.Empty;
                var app1 = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAppTokenProvider((AppTokenProviderParameters _) =>
                                                              {
                                                                  AppTokenProviderResult result = new AppTokenProviderResult
                                                                  {
                                                                      AccessToken = TestConstants.DefaultAccessToken,
                                                                      ExpiresInSeconds = expiresInResponse,
                                                                      RefreshInSeconds = refreshInResponse == 0 ? null : refreshInResponse,
                                                                  };

                                                                  return Task.FromResult(result);
                                                              })
                                                              .WithHttpManager(harness.HttpManager)
                                                              .Build();

                // AcquireToken from app provider
                AuthenticationResult result = await app1.AcquireTokenForClient(TestConstants.s_scope)
                                                        .ExecuteAsync().ConfigureAwait(false);

                if (expectedRefreshIn == 0)
                {
                    Assert.IsFalse(result.AuthenticationResultMetadata.RefreshOn.HasValue);
                }
                else
                {
                    DateTimeOffset expectedRefreshOn = DateTimeOffset.UtcNow;
                    if (expectedRefreshIn != 0)
                        expectedRefreshOn += TimeSpan.FromSeconds(expectedRefreshIn);

                    CoreAssert.IsWithinRange(
                        expectedRefreshOn,
                        result.AuthenticationResultMetadata.RefreshOn.Value,
                        TimeSpan.FromSeconds(2));
                }
            }
        }

        private AppTokenProviderResult GetAppTokenProviderResult(string differentScopesForAt = "", long? refreshIn = 1000)
        {
            var token = new AppTokenProviderResult();
            token.AccessToken = TestConstants.DefaultAccessToken + differentScopesForAt; //Used to indicate that there is a new access token for a different set of scopes
            token.ExpiresInSeconds = 3600;
            token.RefreshInSeconds = refreshIn;

            return token;
        }

        [TestMethod]
        public async Task ParallelRequests_CallTokenEndpointOnceAsync()
        {
            using (var harness = CreateTestHarness())
            {

                int numOfTasks = 10;
                int identityProviderHits = 0;
                int cacheHits = 0;

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithAuthority("https://login.microsoftonline.com/tid")
                    .WithHttpManager(harness.HttpManager)
                    .WithAppTokenProvider((AppTokenProviderParameters _) =>
                    {
                        return Task.FromResult(GetAppTokenProviderResult());
                    })
                    .BuildConcrete();

                Task[] tasks = new Task[numOfTasks];
                for (int i = 0; i < numOfTasks; i++)
                {
                    harness.HttpManager.AddInstanceDiscoveryMockHandler();

                    tasks[i] = Task.Run(async () =>
                    {
                        AuthenticationResult authResult = await app.AcquireTokenForClient(TestConstants.s_scope)
                                                    .ExecuteAsync(new CancellationToken()).ConfigureAwait(false);

                        if (authResult.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider)
                        {
                            // Increment identity hits count
                            Interlocked.Increment(ref identityProviderHits);
                            Assert.IsTrue(identityProviderHits == 1);
                        }
                        else
                        {
                            // Increment cache hits count
                            Interlocked.Increment(ref cacheHits);
                        }
                    });
                }


                await Task.WhenAll(tasks).ConfigureAwait(false);

                Debug.WriteLine($"Total Identity Hits: {identityProviderHits}");
                Debug.WriteLine($"Total Cache Hits: {cacheHits}");
                Assert.IsTrue(cacheHits == 9);

                harness.HttpManager.ClearQueue();

            }
        }

        [TestMethod]
        // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4472
        // Should throw TaskCanceledException instead of trying to take a semaphore
        public async Task CanceledRequest_ThrowsTaskCanceledExceptionAsync()
        {
            var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithInstanceDiscovery(false)
                .WithAppTokenProvider((AppTokenProviderParameters _) =>
                {
                    return Task.FromResult(GetAppTokenProviderResult());
                })
                .BuildConcrete();

            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            await AssertException.TaskThrowsAsync<TaskCanceledException>(
                () => app.AcquireTokenForClient(TestConstants.s_scope)
                        .WithForceRefresh(true)
                        .ExecuteAsync(tokenSource.Token)).ConfigureAwait(false);
        }
    }
}
