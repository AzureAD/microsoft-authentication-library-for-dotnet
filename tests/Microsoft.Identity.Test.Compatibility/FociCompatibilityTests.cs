// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Compatibility
{
    [TestClass]
    public class FociCompatibilityTests
    {
        private const string TenantId = "22222222-2222-2222-2222-222222222222";
        private const string Environment = "login.microsoftonline.com";
        private const string HomeAccountId = "uid." + TenantId;
        private const string Claims = "{\"access_token\":{\"acrs\":{\"essential\":true,\"value\":\"c1\"}}}";

        [TestMethod]
        [DataRow("basic_action", MsalError.InvalidGrantError)]
        [DataRow(null, MsalError.InvalidGrantError)]
        [DataRow("client_mismatch", MsalError.NoTokensFoundError)]
        public async Task FamilyRefreshPreservesServiceErrorsExceptClientMismatchAsync(
            string subError, string expectedError)
        {
            // Arrange
            using var handler = new FamilyRefreshHandler(subError);
            using var client = new HttpClient(handler);
            // Isolate each case's process-wide UI-required throttling entry.
            PublicClientApplication app = PublicClientApplicationBuilder
                .Create(Guid.NewGuid().ToString("D"))
                .WithAuthority($"https://{Environment}/{TenantId}")
                .WithInstanceDiscovery(false)
                .WithHttpClientFactory(new TestHttpClientFactory(client))
                .BuildConcrete();
            app.UserTokenCacheInternal.Accessor.SaveAccount(new MsalAccountCacheItem(
                Environment, TenantId, HomeAccountId, accountSource: null, "user@example.test"));
            app.UserTokenCacheInternal.Accessor.SaveRefreshToken(new MsalRefreshTokenCacheItem(
                Environment, "44444444-4444-4444-4444-444444444444", "family-refresh-token",
                rawClientInfo: null, familyId: "1", HomeAccountId));
            IAccount account = (await app.GetAccountsAsync().ConfigureAwait(false)).Single();

            // Act
            MsalUiRequiredException exception = await Assert.ThrowsAsync<MsalUiRequiredException>(
                () => app.AcquireTokenSilent(new[] { "https://example.test/.default" }, account)
                    .ExecuteAsync()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(expectedError, exception.ErrorCode);
            Assert.AreEqual(1, handler.TokenRequests);
            Assert.AreEqual("MSAL.NetCore", handler.ClientSku);
            if (expectedError == MsalError.InvalidGrantError)
            {
                Assert.IsInstanceOfType<MsalClaimsChallengeException>(exception);
                Assert.AreEqual(Claims, exception.Claims);
            }
        }

        private sealed class TestHttpClientFactory : IMsalHttpClientFactory
        {
            private readonly HttpClient _client;

            public TestHttpClientFactory(HttpClient client)
            {
                _client = client;
            }

            public HttpClient GetHttpClient() => _client;
        }

        private sealed class FamilyRefreshHandler : HttpMessageHandler
        {
            private readonly string _subError;

            public FamilyRefreshHandler(string subError)
            {
                _subError = subError;
            }

            public int TokenRequests { get; private set; }
            public string ClientSku { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Assert.AreEqual(Environment, request.RequestUri.Host);
                if (request.Method == HttpMethod.Get)
                {
                    Assert.EndsWith("/.well-known/openid-configuration", request.RequestUri.AbsolutePath);
                    return Task.FromResult(JsonResponse(HttpStatusCode.OK, new
                    {
                        authorization_endpoint = $"https://{Environment}/{TenantId}/oauth2/v2.0/authorize",
                        token_endpoint = $"https://{Environment}/{TenantId}/oauth2/v2.0/token",
                        issuer = $"https://{Environment}/{TenantId}/v2.0"
                    }));
                }

                Assert.AreEqual(HttpMethod.Post, request.Method);
                Assert.EndsWith("/oauth2/v2.0/token", request.RequestUri.AbsolutePath);
                TokenRequests++;
                ClientSku = request.Headers.GetValues("x-client-SKU").Single();
                return Task.FromResult(JsonResponse(HttpStatusCode.BadRequest, new
                {
                    error = "invalid_grant",
                    error_description = "Additional authentication is required.",
                    suberror = _subError,
                    claims = Claims
                }));
            }

            private static HttpResponseMessage JsonResponse(HttpStatusCode status, object payload)
            {
                return new HttpResponseMessage(status)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
