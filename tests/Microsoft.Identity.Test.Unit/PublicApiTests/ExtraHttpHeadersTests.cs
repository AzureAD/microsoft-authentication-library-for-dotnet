// Copyright (c) Microsoft.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Advanced;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class ExtraHttpHeadersTests : TestBase
    {
        private readonly string _clientId = "4df2cbbb-8612-49c1-87c8-f334d6d065ad";
        private readonly string _scope = "api://msaltokenexchange/.default";
        private readonly string _tenantId = "tenantid";

        private static bool TryGetHeader(HttpRequestMessage req, string name, out string value)
        {
            if (req.Headers.TryGetValues(name, out var v) && v != null)
            {
                value = v.Single();
                return true;
            }

            if (req.Content?.Headers != null &&
                req.Content.Headers.TryGetValues(name, out var v2) &&
                v2 != null)
            {
                value = v2.Single();
                return true;
            }

            value = null;
            return false;
        }

        [TestMethod]
        public async Task AcquireTokenForClient_WithExtraHttpHeaders_SendsHeaders_Async()
        {
            using var httpManager = new MockHttpManager();
            {
                // 1) Instance discovery
                httpManager.AddInstanceDiscoveryMockHandler();

                // 2) Token endpoint
                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(),
                    AdditionalRequestValidation = (HttpRequestMessage req) =>
                    {
                        Assert.IsTrue(TryGetHeader(req, "x-ms-test", out var v1), "x-ms-test not present.");
                        Assert.AreEqual("abc", v1);

                        Assert.IsTrue(TryGetHeader(req, "x-correlation-id", out var v2), "x-correlation-id not present.");
                        Assert.AreEqual("123", v2);
                    }
                });

                var app = ConfidentialClientApplicationBuilder
                            .Create(_clientId)
                            .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                            .WithClientSecret("ClientSecret")
                            .WithHttpManager(httpManager)
                            .BuildConcrete();

                var headers = new Dictionary<string, string>
                {
                    ["x-ms-test"] = "abc",
                    ["x-correlation-id"] = "123"
                };

                var result = await app.AcquireTokenForClient(new[] { _scope })
                                      .WithExtraHttpHeaders(headers) // <-- new API under test
                                      .ExecuteAsync()
                                      .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task AcquireTokenForClient_ListAllRequestHeaders_Async()
        {
            using var httpManager = new MockHttpManager();
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(),
                    AdditionalRequestValidation = (HttpRequestMessage req) =>
                    {
                        // 1) Dump everything to the test output (no assumptions)
                        foreach (var kv in EnumerateAllHeaders(req))
                        {
                            TestContext.WriteLine($"{kv.Key}: {string.Join(", ", kv.Value)}");
                        }

                        // 2) (Optional) Assert a few stable MSAL defaults are present.
                        // Keep this list small to avoid flakiness across platforms.
                        AssertHeaderExists(req, "client-request-id");
                        AssertHeaderExists(req, "return-client-request-id");
                        AssertHeaderExists(req, "x-client-sku");
                        AssertHeaderExists(req, "x-client-ver");
                        AssertHeaderExists(req, "x-client-os");
                        AssertHeaderExists(req, "Accept");
                        AssertHeaderExists(req, "Content-Type");
                        AssertHeaderExists(req, "x-ms-test");
                    }
                });

                var app = ConfidentialClientApplicationBuilder
                            .Create(_clientId)
                            .WithAuthority("https://login.microsoftonline.com/", _tenantId)
                            .WithClientSecret("ClientSecret")
                            .WithHttpManager(httpManager)
                            .BuildConcrete();

                // Include one custom header to prove user-provided + defaults both show up
                var custom = new Dictionary<string, string>
                {
                    ["x-ms-test"] = "abc"
                };

                var result = await app.AcquireTokenForClient(new[] { _scope })
                                      .WithExtraHttpHeaders(custom)
                                      .ExecuteAsync()
                                      .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        private static IEnumerable<KeyValuePair<string, IEnumerable<string>>> EnumerateAllHeaders(HttpRequestMessage req)
        {
            foreach (var h in req.Headers)
                yield return new KeyValuePair<string, IEnumerable<string>>(h.Key, h.Value);

            if (req.Content != null)
            {
                foreach (var h in req.Content.Headers)
                    yield return new KeyValuePair<string, IEnumerable<string>>(h.Key, h.Value);
            }
        }

        private static void AssertHeaderExists(HttpRequestMessage req, string name)
        {
            bool found =
                (req.Headers.TryGetValues(name, out var v1) && v1 != null) ||
                (req.Content?.Headers?.TryGetValues(name, out var v2) ?? false);

            Assert.IsTrue(found, $"Expected header '{name}' not found.");
        }
    }
}
