// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !ANDROID && !iOS 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.RP;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    [DeploymentItem(@"Resources\testCert.crtfile")]
    [DeploymentItem(@"Resources\RSATestCertDotNet.pfx")]
    public class ConfidentialClientWithCertTests : TestBase
    {
        private byte[] _serializedCache;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            _serializedCache = null;
        }

        private const string _clientAssertionClaims = "{\"custom_claims\":{\"xms_az_tm\":\"azureinfra\",\"xms_az_nwperimid\":[\"00000000-1403-0100-0000-000000000000\",\"00000000-dc4b-4eb1-9fa3-902c8d13b5bd\"]}}";

        private static MockHttpMessageHandler CreateTokenResponseHttpHandler(bool clientCredentialFlow)
        {
            return new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = CreateResponse(clientCredentialFlow)
            };
        }

        private static MockHttpMessageHandler CreateTokenResponseHttpHandlerWithX5CValidation(
            bool clientCredentialFlow,
            string expectedX5C = null)
        {
            return new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = CreateResponse(clientCredentialFlow),
                AdditionalRequestValidation = request =>
                {
                    var requestContent = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var formsData = CoreHelpers.ParseKeyValueList(requestContent, '&', true, null);

                    // Check presence of client_assertion in request
                    Assert.IsTrue(formsData.TryGetValue("client_assertion", out string encodedJwt), "Missing client_assertion from request");

                    // Check presence and value of x5c cert claim.
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadJwtToken(encodedJwt);
                    var x5c = jsonToken.Header.FirstOrDefault(header => header.Key == "x5c");

                    if (expectedX5C != null)
                    {
                        Assert.AreEqual("x5c", x5c.Key, "x5c should be present");
                        Assert.AreEqual(x5c.Value.ToString(), expectedX5C);
                    }
                    else
                    {
                        Assert.IsNull(x5c.Key);
                        Assert.IsNull(x5c.Value);
                    }
                }
            };
        }

        private static HttpResponseMessage CreateResponse(bool clientCredentialFlow)
        {
            return clientCredentialFlow ?
                MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid)) :
                MockHelpers.CreateSuccessTokenResponseMessage(
                          TestConstants.s_scope.AsSingleString(),
                          MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                          MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid));
        }

        private void SetupMocks(MockHttpManager httpManager)
        {
            httpManager.AddInstanceDiscoveryMockHandler();
        }

        private void SetupMocks(MockHttpManager httpManager, string authority)
        {
            httpManager.AddInstanceDiscoveryMockHandler();
        }

        [DataTestMethod]
        [DataRow(true, null, true)]
        [DataRow(false, null, false)]
        [DataRow(null, null, false)] // the default is false
        [DataRow(null, true, true)]
        [DataRow(null, false, false)]
        [DataRow(true, true, true)]
        [DataRow(false, false, false)]
        [DataRow(true, false, false)] // request overrides
        [DataRow(false, true, true)] // request overrides
        public async Task JsonWebTokenWithX509PublicCertSendCertificateTestSendX5cCombinationsAsync(bool? appFlag, bool? requestFlag, bool expectX5c)
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);
                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                var appBuilder = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithHttpManager(harness.HttpManager);

                if (appFlag.HasValue)
                {
                    appBuilder = appBuilder.WithCertificate(certificate, appFlag.Value); // app flag
                }
                else
                {
                    appBuilder = appBuilder.WithCertificate(certificate); // no app flag
                }

                var app = appBuilder.BuildConcrete();

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandlerWithX5CValidation(
                    true, expectX5c ? exportedCertificate : null));

                var builder = app.AcquireTokenForClient(TestConstants.s_scope);

                if (requestFlag != null)
                {
                    builder = builder.WithSendX5C(requestFlag.Value);
                }

                AuthenticationResult result = await builder
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
            }
        }

        [DataTestMethod]
        [DataRow(true, null, true)]
        [DataRow(false, null, false)]
        [DataRow(null, null, false)] // the default is false
        [DataRow(null, true, true)]
        [DataRow(null, false, false)]
        [DataRow(true, true, true)]
        [DataRow(false, false, false)]
        [DataRow(true, false, false)] // request overrides
        [DataRow(false, true, true)] // request overrides
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Internal.Analyzers", "IA5352:DoNotMisuseCryptographicApi", Justification = "Suppressing RoslynAnalyzers: Rule: IA5352 - Do Not Misuse Cryptographic APIs in test only code")]
        public async Task TestX5C(
            bool? appFlag,
            bool? requestFlag,
            bool expectX5c)
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);
                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                IDictionary<string, string> claimsToSign = new Dictionary<string, string>
                {
                    { "Foo", "Bar" }
                };

                var appBuilder = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(harness.HttpManager);

                if (appFlag.HasValue)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    appBuilder = appBuilder.WithClientClaims(
                        certificate,
                        claimsToSign,
                        sendX5C: appFlag.Value); // app flag
                }
                else
                {
                    appBuilder = appBuilder.WithClientClaims(
                        certificate,
                        claimsToSign); // no app flag
                }
#pragma warning restore CS0618 // Type or member is obsolete

                var app = appBuilder.BuildConcrete();

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandlerWithX5CValidation(
                    true, expectX5c ? exportedCertificate : null));

                var builder = app.AcquireTokenForClient(TestConstants.s_scope);

                if (requestFlag != null)
                {
                    builder = builder.WithSendX5C(requestFlag.Value);
                }

                AuthenticationResult result = await builder
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
            }
        }

        [TestMethod]
        public async Task ClientAssertionHasExpiration()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                IDictionary<string, string> extraAssertionContent = new Dictionary<string, string>
                {
                    { "foo", "bar" },
                    
                };

#pragma warning disable CS0618 // Type or member is obsolete
                var cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority("https://login.microsoftonline.com/tid")    
                    .WithHttpManager(harness.HttpManager)                    
                    .WithClientClaims(certificate, extraAssertionContent, mergeWithDefaultClaims: true, sendX5C: true) // x5c can also be enabled on the request
                    .Build();
#pragma warning restore CS0618 // Type or member is obsolete

                // Checks the client assertion for x5c and for expiration
                var handler = harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);
                handler.AdditionalRequestValidation = (r) => ValidateClientAssertion(r, exportedCertificate, validateStandardClaims: true);

                AuthenticationResult result = await cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithSendX5C(true) // x5c can also be enabled here on the request
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
            }
        }

        [TestMethod]
        public async Task ClientAssertionWithClaimOverride()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                IDictionary<string, string> extraAssertionContent = new Dictionary<string, string>
                {
                    { "foo", "bar" },
                    { "iss", "issuer_override" }
                };

#pragma warning disable CS0618 // Type or member is obsolete
                var cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority("https://login.microsoftonline.com/tid")
                    .WithHttpManager(harness.HttpManager)
                    .WithClientClaims(certificate, extraAssertionContent, mergeWithDefaultClaims: true, sendX5C: false)
                    .Build();
#pragma warning restore CS0618 // Type or member is obsolete

                // Checks the client assertion for x5c and for expiration
                var handler = harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);
                JwtSecurityToken assertion = null;
                handler.AdditionalRequestValidation = (r) => assertion = ValidateClientAssertion(r, exportedCertificate, validateStandardClaims: false);

                AuthenticationResult result = await cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithSendX5C(true)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                assertion.Claims.Single(c => c.Type == "iss").Value.Equals("issuer_override");
            }
        }

        private JwtSecurityToken ValidateClientAssertion(HttpRequestMessage request, string expectedX5cValue, bool validateStandardClaims )
        {
            var requestContent = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var formsData = CoreHelpers.ParseKeyValueList(requestContent, '&', true, null);

            // Check presence of client_assertion in request
            Assert.IsTrue(formsData.TryGetValue("client_assertion", out string encodedJwt), "Missing client_assertion from request");

            // Check presence and value of x5c cert claim.
            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken assertionJwt = handler.ReadJwtToken(encodedJwt);
            if (validateStandardClaims)
            {
                Assert.AreEqual("https://login.microsoftonline.com/tid/oauth2/v2.0/token", assertionJwt.Claims.Single(c => c.Type == "aud").Value);
                Assert.AreEqual(TestConstants.ClientId, assertionJwt.Claims.Single(c => c.Type == "iss").Value);
                Assert.AreEqual(TestConstants.ClientId, assertionJwt.Claims.Single(c => c.Type == "sub").Value);
            }

            // Assert extra claims
            Assert.AreEqual("bar", assertionJwt.Claims.Single(c => c.Type == "foo").Value);

            // Assert exp and nbf claims 
            long exp = long.Parse(assertionJwt.Claims.Single(c => c.Type == "exp").Value);

            DateTimeOffset actualExpDate = DateTimeOffset.FromUnixTimeSeconds(exp);
            DateTimeOffset expectedExpDate = DateTimeOffset.Now + TimeSpan.FromSeconds(JsonWebToken.JwtToAadLifetimeInSeconds);
            CoreAssert.IsWithinRange(expectedExpDate, actualExpDate, TimeSpan.FromSeconds(5));

            long nbf = long.Parse(assertionJwt.Claims.Single(c => c.Type == "nbf").Value);
            DateTimeOffset actualNbfDate = DateTimeOffset.FromUnixTimeSeconds(nbf);
            CoreAssert.IsWithinRange(DateTimeOffset.Now, actualNbfDate, TimeSpan.FromSeconds(5));

            var x5c = assertionJwt.Header.FirstOrDefault(header => header.Key == "x5c");

            Assert.AreEqual("x5c", x5c.Key, "x5c should be present");
            Assert.AreEqual(x5c.Value.ToString(), expectedX5cValue);

            return assertionJwt;
        }

        [TestMethod]
        [Description("Test for client assertion with X509 public certificate using sendCertificate")]
        public async Task JsonWebTokenWithX509PublicCertSendCertificateTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);
                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate, true)
                    .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(
                    CreateTokenResponseHttpHandlerWithX5CValidation(true, exportedCertificate));
                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                appCacheAccess.AssertAccessCounts(1, 1);
                userCacheAccess.AssertAccessCounts(0, 0);

                // from the cache
                result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.IsNull(result.ClaimsPrincipal);

                //Check app cache
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                appCacheAccess.AssertAccessCounts(2, 1);
                userCacheAccess.AssertAccessCounts(0, 0);
            }
        }

        [TestMethod]
        [Description("Test for client assertion with X509 public certificate using sendCertificate")]
        public async Task JsonWebTokenWithX509PublicCertSendCertificateOnBehalfOfTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);

                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate, true)
                    .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandlerWithX5CValidation(false, exportedCertificate));
                AuthenticationResult result = await app
                    .AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                Assert.IsNotNull(result.AccessToken);

                appCacheAccess.AssertAccessCounts(0, 0);
                userCacheAccess.AssertAccessCounts(1, 1);

                result = await app
                    .AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);

                //Check user cache
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                appCacheAccess.AssertAccessCounts(0, 0);
                userCacheAccess.AssertAccessCounts(2, 1);
            }
        }

        [TestMethod]
        [Description("Test for client assertion with X509 public certificate using Auth code")]
        public async Task JsonWebTokenWithX509PublicCertSendCertificateByAuthCodeTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);

                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate, true)
                    .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(
                    CreateTokenResponseHttpHandlerWithX5CValidation(false, exportedCertificate));
                AuthenticationResult result = await app
                    .AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                Assert.IsNotNull(result.AccessToken);

                appCacheAccess.AssertAccessCounts(0, 0);
                userCacheAccess.AssertAccessCounts(0, 1);
            }
        }

        [TestMethod]
        [Description("Test for client assertion with X509 public certificate using acquire token by refresh token")]
        public async Task JsonWebTokenWithX509PublicCertSendCertificateByRefreshTokenTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);

                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate, true)
                    .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(
                    CreateTokenResponseHttpHandlerWithX5CValidation(false, exportedCertificate));
                AuthenticationResult result = await ((IByRefreshToken)app)
                    .AcquireTokenByRefreshToken(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                Assert.IsNotNull(result.AccessToken);

                appCacheAccess.AssertAccessCounts(0, 0);
                userCacheAccess.AssertAccessCounts(0, 1);

                Assert.AreEqual(result.Account.HomeAccountId.Identifier,
                    userCacheAccess.LastAfterAccessNotificationArgs.SuggestedCacheKey);
                Assert.AreEqual(result.Account.HomeAccountId.Identifier,
                    userCacheAccess.LastBeforeAccessNotificationArgs.SuggestedCacheKey);
            }
        }

        [TestMethod]
        [Description("Test for acquireTokenSilent with X509 public certificate using sendCertificate")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Internal.Analyzers", "IA5352:DoNotMisuseCryptographicApi", Justification = "Fake password only used for tests.")]
        public async Task JsonWebTokenWithX509PublicCertSendCertificateSilentTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager, "https://login.microsoftonline.com/my-utid/");
                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri("https://login.microsoftonline.com/my-utid"), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate, true)
                    .BuildConcrete();

                TokenCacheHelper.PopulateCacheWithOneAccessToken(app.UserTokenCacheInternal.Accessor);
                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(
                    CreateTokenResponseHttpHandlerWithX5CValidation(false, exportedCertificate));

                var result = await app
                    .AcquireTokenSilent(
                        new[] { "someTestScope" },
                        new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
                    .WithForceRefresh(true)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);

                //Check user cache
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());

                appCacheAccess.AssertAccessCounts(0, 0);
                userCacheAccess.AssertAccessCounts(1, 1);
            }
        }

        [DataTestMethod]
        [DataRow(true, true, true, true)]
        [DataRow(true, true, true, false)]
        [DataRow(true, true, false, true)]
        [DataRow(true, true, false, false)]
        [DataRow(true, false, true, true)]
        [DataRow(true, false, true, false)]
        [DataRow(true, false, false, true)]
        [DataRow(true, false, false, false)]
        [DataRow(false, true, true, true)]
        [DataRow(false, true, true, false)]
        [DataRow(false, true, false, true)]
        [DataRow(false, true, false, false)]
        [DataRow(false, false, true, true)]
        [DataRow(false, false, true, false)]
        [DataRow(false, false, false, true)]
        [DataRow(false, false, false, false)]
        public void ClientAssertionTests(bool sendX5C, bool useSha2AndPss, bool addExtraClaims, bool appendDefaultClaims)
        {
            // for asserting nbf and exp - make it bigger for debugging.
            TimeSpan tolerance = TimeSpan.FromSeconds(3);

            Trace.WriteLine($"sendX5C {sendX5C}, useSha2AndPss {useSha2AndPss}, addExtraClaims {addExtraClaims}, appendDefaultClaims {appendDefaultClaims}");
            var cert = new X509Certificate2(
               ResourceHelper.GetTestResourceRelativePath(
                   "testCert.crtfile"),
               TestConstants.TestCertPassword);

            JsonWebToken msalJwtTokenObj =
                new JsonWebToken(new CommonCryptographyManager(),
                TestConstants.ClientId,
                "aud",
                addExtraClaims ? TestConstants.AdditionalAssertionClaims : null,
                appendDefaultClaims);

            string assertion = msalJwtTokenObj.Sign(cert, sendX5C: sendX5C, useSha2AndPss: useSha2AndPss);

            // Use Wilson to decode the token and check its claims
            JwtSecurityToken decodedToken = new JwtSecurityToken(assertion);
            AssertClientAssertionHeader(cert, decodedToken, sendX5C, useSha2AndPss);

            // special case - this is treated just as adding default claims
            if (appendDefaultClaims == false && addExtraClaims == false)
                appendDefaultClaims = true;

            int expectedPayloadClaimsCount = (appendDefaultClaims ? 6 : 0) + (addExtraClaims ? 3 : 0);
            Assert.AreEqual(expectedPayloadClaimsCount, decodedToken.Payload.Count);
            if (appendDefaultClaims)
            {
                Assert.AreEqual("aud", decodedToken.Payload["aud"]);
                Assert.AreEqual(TestConstants.ClientId, decodedToken.Payload["iss"]);
                Assert.AreEqual(TestConstants.ClientId, decodedToken.Payload["sub"]);
                long nbf = long.Parse(decodedToken.Payload["nbf"].ToString());
                var nbfDate = DateTimeOffset.FromUnixTimeSeconds(nbf);
                CoreAssert.IsWithinRange(
                    DateTimeOffset.Now,
                    nbfDate,
                    tolerance);

                long exp = long.Parse(decodedToken.Payload["exp"].ToString());
                var expDate = DateTimeOffset.FromUnixTimeSeconds(exp);
                CoreAssert.IsWithinRange(
                    DateTimeOffset.Now + TimeSpan.FromSeconds(JsonWebToken.JwtToAadLifetimeInSeconds),
                    expDate,
                    tolerance);
            }

            if (addExtraClaims)
            {
                Assert.AreEqual("Val1", decodedToken.Payload["Key1"]);
                Assert.AreEqual("Val2", decodedToken.Payload["Key2"]);
                //Ensure JSON formatting is preserved
                Assert.AreEqual("{\"xms_az_claim\": [\"GUID\", \"GUID2\", \"GUID3\"]}", decodedToken.Payload["customClaims"]);
            }

            if (useSha2AndPss)
            {
                Assert.AreEqual(
                    "bmQeK7jALQuzsm3zZhXskUB41iAU0lyzzX2AKJAtiZ8",
                    decodedToken.Header["x5t#S256"]);
            }
            else
            {
                Assert.AreEqual(
                    "5wxQ2k6mb5QimllLwRLLS0_ynrQ",
                    decodedToken.Header["x5t"]);
            }

            if (sendX5C)
            {
                Assert.AreEqual(
                    "MIIDQjCCAiqgAwIBAgIQTuexEO9cdYhC0jy1nmS6jTANBgkqhkiG9w0BAQsFADAiMSAwHgYDVQQDDBd0cndhbGtlLm9ubWljcm9zb2Z0LmNvbTAeFw0xNzA4MTExODEzMTBaFw0xODA4MTExODMzMTBaMCIxIDAeBgNVBAMMF3Ryd2Fsa2Uub25taWNyb3NvZnQuY29tMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA5Qe3Ah/E97K0o288gYUNa0H8FO/w8pb1dvls/boQDoZxUD11TpAQrKZwstS6+ulGF6cHmj44AH8MNBKNUbW2L1NTjFG9bltaSXpJXzbIH/cUppF9rxngZ0CM7cHtuoccBPBVEuQiJ86pD7qlqE2EA2BdBmfz3Hd41rybdaWkHMxMcBC7nh6w87/KoyikKXCMLUUyRTJLSivo+gfKJsiYGAjqZ54aJraP5LMiPG2qYTOZR6wMme93mYRp85sqGTvgzRCq37STH2HmcYilUQ9kZFe5SR+1vOki97XLg+H7FuFtkSMM7dEnTWkDv+BJ1ZQvCEj623cJxXlq0fd7hVUxIQIDAQABo3QwcjAOBgNVHQ8BAf8EBAMCBaAwHQYDVR0lBBYwFAYIKwYBBQUHAwIGCCsGAQUFBwMBMCIGA1UdEQQbMBmCF3Ryd2Fsa2Uub25taWNyb3NvZnQuY29tMB0GA1UdDgQWBBSauRo9cNk8J6RTLWMQSyUQnxjQzDANBgkqhkiG9w0BAQsFAAOCAQEAhYl1I8qETtvVt6m/YrGknA90R/FtIePt/ViBae3mxPJWlVoq5fTTriQcuPHXfI5kbjTQJIwCVTT/CRSlKkzRcrSsQUxxHNE7IdpvvDbkf6AMPxQhNACHQd0cIWmsmf+ItKsC70LKQ+93+VgmBsv2j8XwF0JTqwuKoqXnDjCzHvmU67xhPY6CSPA/0XOiVTx1BDWd5cPdsH2bZnAeApsvrzU8W7iPgV/oN9MMfogocvDUXd6T+QGLMAYoInHXsqG6+SEarqRDUPQZOHo5Ax4Mvhsnd2b4u5d5Y/R0z0wUwtOiF0Tu+w79JIqDRYaaJLTKxZ+2DyYOu54u0LGsGhki1g==",
                    decodedToken.Header["x5c"]);
            }
        }

        private static void AssertClientAssertionHeader(
            X509Certificate2 cert,
            JwtSecurityToken decodedToken,
            bool sendX5c,
            bool useSha2AndPss)
        {

            // Wilson is guaranteed to parse the token correctly - use it as baseline
            Assert.AreEqual(sendX5c ? 4 : 3, decodedToken.Header.Count);
            Assert.AreEqual("JWT", decodedToken.Header["typ"]);
            Assert.AreEqual(useSha2AndPss ? "PS256" : "RS256", decodedToken.Header["alg"]);

            if (useSha2AndPss)
            {
                Assert.AreEqual(
                    ComputeCertThumbprint(cert, true),
                    decodedToken.Header["x5t#S256"]);
            }
            else
            {
                Assert.AreEqual(
                    ComputeCertThumbprint(cert, false),
                    decodedToken.Header["x5t"]);
            }

            if (sendX5c)
            {
                Assert.AreEqual(
                    Convert.ToBase64String(cert.RawData),
                    decodedToken.Header["x5c"]);
            }
        }

        [TestMethod]
        [Description("Check that the private key is accessable when signing")]
        public async Task CheckRSAPrivateKeyCanSignAssertionAsync()
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);
                var certificate = new X509Certificate2(
                    ResourceHelper.GetTestResourceRelativePath("RSATestCertDotNet.pfx"));

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandler(true));

                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                appCacheAccess.AssertAccessCounts(1, 1);
                userCacheAccess.AssertAccessCounts(0, 0);

                // from the cache
                result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);

                //Check app cache
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                appCacheAccess.AssertAccessCounts(2, 1);
                userCacheAccess.AssertAccessCounts(0, 0);
            }
        }

        [TestMethod]
        [Description("Check if the certificate is disposed and throw proper exception")]
        public async Task DisposedCert_ThrowsSpecificException_Test()
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);

                ConfidentialClientApplication app = null;
                //initialize client app with cert then dispose of it
                using (var certificate = new X509Certificate2(
                    ResourceHelper.GetTestResourceRelativePath("RSATestCertDotNet.pfx")))
                {
                    app = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                        .WithRedirectUri(TestConstants.RedirectUri)
                        .WithHttpManager(harness.HttpManager)
                        .WithCertificate(certificate)
                        .BuildConcrete();
                }

                //Testing client credential flow
                var exception = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                {
                    await app.AcquireTokenForClient(TestConstants.s_scope)
                             .ExecuteAsync(CancellationToken.None)
                             .ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.AreEqual(MsalError.CryptographicError, exception.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.CryptographicError, exception.Message);

                //Testing auth code flow
                exception = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                {
                    await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                             .ExecuteAsync(CancellationToken.None)
                             .ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.AreEqual(MsalError.CryptographicError, exception.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.CryptographicError, exception.Message);

                //Testing OBO flow
                exception = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                {
                    await app.AcquireTokenOnBehalfOf(TestConstants.s_scope, new UserAssertion(TestConstants.UserAssertion))
                             .ExecuteAsync(CancellationToken.None)
                             .ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.AreEqual(MsalError.CryptographicError, exception.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.CryptographicError, exception.Message);
            }
        }

        // regression test for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4913
        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task RopcCcaSendsX5CAsync(bool sendX5C)
        {
            using (var harness = CreateTestHarness())
            {
                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate, sendX5C)
                    .Build();

                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                harness.HttpManager.AddMockHandler(
                    CreateTokenResponseHttpHandlerWithX5CValidation(
                        clientCredentialFlow: false,
                        expectedX5C: sendX5C ? exportedCertificate : null));

                var result = await (app as IByUsernameAndPassword)
                    .AcquireTokenByUsernamePassword(
                        TestConstants.s_scope,
                        TestConstants.Username,
                        TestConstants.DefaultPassword)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task RopcCcaSendsX5CUsingRequestLevelAPIAsync(bool sendX5C)
        {
            using (var harness = CreateTestHarness())
            {
                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .Build();

                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                harness.HttpManager.AddMockHandler(
                    CreateTokenResponseHttpHandlerWithX5CValidation(
                        clientCredentialFlow: false,
                        expectedX5C: sendX5C ? exportedCertificate : null));

                var result = await (app as IByUsernameAndPassword)
                    .AcquireTokenByUsernamePassword(
                        TestConstants.s_scope,
                        TestConstants.Username,
                        TestConstants.DefaultPassword)
                    .WithSendX5C(sendX5C)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }


        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task EnsureCertificateSerialNumberIsAddedToCacheKeyTestAsync(bool useCertificateOptions)
        {
            using (var httpManager = new MockHttpManager())
            {
                var certificate = CertHelper.GetOrCreateTestCert();

                var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                              .WithRedirectUri(TestConstants.RedirectUri)
                                              .WithHttpManager(httpManager)
                                              .WithExperimentalFeatures();

                if (useCertificateOptions)
                {
                    CertificateOptions certificateOptions = new CertificateOptions()
                    {
                        SendX5C = true,
                        AssociateTokensWithCertificate = true
                    };

                    builder.WithCertificate(certificate, certificateOptions);
                }
                else
                {
                    builder.WithCertificate(certificate, true, true);
                }

                var app = builder.BuildConcrete();

                app.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
                app.AppTokenCache.SetAfterAccess(AfterCacheAccess);

                var appCacheAccess = app.AppTokenCache.RecordAccess();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                //Ensure serial number matches
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("header.payload.signature", result.AccessToken);

                var serialNumber = app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First().AdditionalCacheKeyComponents.FirstOrDefault().Value;
                Assert.AreEqual(certificate.SerialNumber, serialNumber);

                //Ensure serial number is available from cache

                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("header.payload.signature", result.AccessToken);

                serialNumber = app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First().AdditionalCacheKeyComponents.FirstOrDefault().Value;
                Assert.AreEqual(certificate.SerialNumber, serialNumber);

                //Ensure serial number is available from cache in new app
                var app2 = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                              .WithRedirectUri(TestConstants.RedirectUri)
                              .WithCertificate(certificate, true, true)
                              .WithHttpManager(httpManager)
                              .WithExperimentalFeatures()
                              .BuildConcrete();

                app2.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
                app2.AppTokenCache.SetAfterAccess(AfterCacheAccess);

                appCacheAccess = app2.AppTokenCache.RecordAccess();

                //Ensure serial number matches
                result = await app2.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("header.payload.signature", result.AccessToken);

                serialNumber = app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First().AdditionalCacheKeyComponents.FirstOrDefault().Value;
                Assert.AreEqual(certificate.SerialNumber, serialNumber);

                //Ensure different cert does not acquire previous tokens from cache
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                var certificate2 = CertHelper.GetOrCreateTestCert(regenerateCert: true);

                var app3 = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                            .WithRedirectUri(TestConstants.RedirectUri)
                            .WithCertificate(certificate2, true, true)
                            .WithHttpManager(httpManager)
                            .WithExperimentalFeatures()
                            .BuildConcrete();

                app3.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
                app3.AppTokenCache.SetAfterAccess(AfterCacheAccess);

                //Ensure serial number does not match
                result = await app3.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource); //Token should be from ESTS
                Assert.AreEqual("header.payload.signature", result.AccessToken);
            }
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task EnsureDefaultCacheKeyBehaviorWhenCertSerialNumberIsNotUsedTestAsync(bool useCertificateOptions)
        {
            using (var httpManager = new MockHttpManager())
            {
                var certificate = CertHelper.GetOrCreateTestCert();

                //Acquire initial token
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                              .WithRedirectUri(TestConstants.RedirectUri)
                                              .WithCertificate(certificate, true, false)
                                              .WithHttpManager(httpManager)
                                              .WithExperimentalFeatures()
                                              .BuildConcrete();




                app.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
                app.AppTokenCache.SetAfterAccess(AfterCacheAccess);

                var appCacheAccess = app.AppTokenCache.RecordAccess();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("header.payload.signature", result.AccessToken);

                //Ensure token is available from cache
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("header.payload.signature", result.AccessToken);

                //Ensure token is no longer available from cache when cert serial number is enabled
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var app2 = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                              .WithRedirectUri(TestConstants.RedirectUri)
                              .WithCertificate(certificate, true, true)
                              .WithHttpManager(httpManager)
                              .WithExperimentalFeatures()
                              .BuildConcrete();

                app2.AppTokenCache.SetBeforeAccess(BeforeCacheAccess);
                app2.AppTokenCache.SetAfterAccess(AfterCacheAccess);

                app2.AppTokenCache.RecordAccess();

                result = await app2.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("header.payload.signature", result.AccessToken);

                var token = app2.AppTokenCacheInternal.Accessor.GetAllAccessTokens()
                                                                .Where(x =>
                                                                x.AdditionalCacheKeyComponents?.Any() == true &&
                                                                x.AdditionalCacheKeyComponents.Any()).FirstOrDefault();

                Assert.IsNotNull(token);

                Assert.AreEqual(certificate.SerialNumber, token.AdditionalCacheKeyComponents.FirstOrDefault().Value);
            }
        }

        [TestMethod]
        public void EnsureNullCertDoesNotSetSerialNumberTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var certificate = CertHelper.GetOrCreateTestCert();

                var exception = Assert.ThrowsException<ArgumentNullException>(() =>
                {
                    var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                              .WithRedirectUri(TestConstants.RedirectUri)
                                              .WithCertificate(null, true, true)
                                              .WithHttpManager(httpManager)
                                              .WithExperimentalFeatures()
                                              .BuildConcrete();
                });

                Assert.IsTrue(exception.Message.Contains("Value cannot be null"));
            }
        }

        [TestMethod]
        public async Task AcquireTokenForClient_ShouldSendClientInfoParameter_WithValueTwo_Async()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                // Set up the expected POST data to include client_info = "2"
                var expectedPostData = new Dictionary<string, string>
                {
                    [OAuth2Parameter.GrantType] = OAuth2GrantType.ClientCredentials,
                    [OAuth2Parameter.Scope] = TestConstants.s_scope.AsSingleString(),
                    [OAuth2Parameter.ClientInfo] = "2"
                };

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                    expectedPostData: expectedPostData);

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithAuthority(TestConstants.AuthorityCommonTenant)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task ClientAssertionWithComplexClaims()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                // Test complex nested claims as described in the issue
                IDictionary<string, string> extraAssertionContent = new Dictionary<string, string>
                {
                    { "foo", "bar" },
                    { "custom_claims", "{\"xms_foo\":[\"abc\",\"def\"],\"xms_az_foo\":\"bar\"}" }
                };

#pragma warning disable CS0618 // Type or member is obsolete
                var cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority("https://login.microsoftonline.com/tid")
                    .WithHttpManager(harness.HttpManager)
                    .WithClientClaims(certificate, extraAssertionContent, mergeWithDefaultClaims: true, sendX5C: true)
                    .Build();
#pragma warning restore CS0618 // Type or member is obsolete

                var handler = harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);
                JwtSecurityToken assertion = null;
                handler.AdditionalRequestValidation = (r) =>
                {
                    var requestContent = r.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var formsData = CoreHelpers.ParseKeyValueList(requestContent, '&', true, null);

                    // Check presence of client_assertion in request
                    Assert.IsTrue(formsData.TryGetValue("client_assertion", out string encodedJwt), "Missing client_assertion from request");

                    // Decode and validate the JWT
                    var jwtHandler = new JwtSecurityTokenHandler();
                    assertion = jwtHandler.ReadJwtToken(encodedJwt);

                    // Validate that custom_claims is a nested object, not an escaped string
                    var customClaimsClaim = assertion.Claims.FirstOrDefault(c => c.Type == "custom_claims");
                    Assert.IsNotNull(customClaimsClaim, "custom_claims should be present");

                    // Validate the type of claim value is a string
                    Assert.IsTrue(typeof(string).IsAssignableFrom(customClaimsClaim.Value.GetType()), "custom_claims claim value should be a string");

                    // The value should be a JSON object, not an escaped string
                    string customClaimsValue = customClaimsClaim.Value;
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(customClaimsValue); // This will throw if not valid JSON object
                    Assert.AreEqual(JsonValueKind.Object, jsonElement.ValueKind, "custom_claims claim value should be a JSON object");
                };

                AuthenticationResult result = await cca.AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
            }
        }

        [TestMethod]
        public async Task WithExtraClientAssertionClaims_AddsClaimsToCacheKey_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var certificate = CertHelper.GetOrCreateTestCert();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority("https://login.microsoftonline.com/tid")
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .Build();

                var handler = harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);
                JwtSecurityToken assertion = null;
                handler.AdditionalRequestValidation = (r) =>
                {
                    var requestContent = r.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var formsData = CoreHelpers.ParseKeyValueList(requestContent, '&', true, null);

                    Assert.IsTrue(formsData.TryGetValue("client_assertion", out string encodedJwt), "Missing client_assertion from request");

                    var jwtHandler = new JwtSecurityTokenHandler();
                    assertion = jwtHandler.ReadJwtToken(encodedJwt);

                    // Validate extra claims are present
                    var customClaimsClaim = assertion.Claims.FirstOrDefault(c => c.Type == "custom_claims");
                    Assert.IsNotNull(customClaimsClaim, "custom_claims should be present");

                    // Parse the nested claim value as JSON
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(customClaimsClaim.Value);
                    Assert.AreEqual(JsonValueKind.Object, jsonElement.ValueKind, "custom_claims should be a JSON object");
                    
                    // Validate nested properties
                    Assert.IsTrue(jsonElement.TryGetProperty("xms_az_tm", out var tmProperty));
                    Assert.AreEqual("azureinfra", tmProperty.GetString());
                    
                    Assert.IsTrue(jsonElement.TryGetProperty("xms_az_nwperimid", out var nwperimidProperty));
                    Assert.AreEqual(JsonValueKind.Array, nwperimidProperty.ValueKind);
                    Assert.AreEqual(2, nwperimidProperty.GetArrayLength());
                };

                AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraClientAssertionClaims(_clientAssertionClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.IsNotNull(assertion);
            }
        }

        [TestMethod]
        public async Task WithExtraClientAssertionClaims_DifferentClaims_ResultsInDifferentCacheEntries_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var certificate = CertHelper.GetOrCreateTestCert();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority("https://login.microsoftonline.com/tid")
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .BuildConcrete();

                // First request with _clientAssertionClaims
                harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);

                AuthenticationResult result1 = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraClientAssertionClaims(_clientAssertionClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result1.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                // Second request with different claims should go to IdP
                harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);
                string extraClaims2 = "{\"claim2\":\"value2\"}";

                AuthenticationResult result2 = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraClientAssertionClaims(extraClaims2)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result2.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);

                // Verify we have 2 tokens in cache
                Assert.AreEqual(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            }
        }

        [TestMethod]
        public async Task WithExtraClientAssertionClaims_SameClaims_UsesCache_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var certificate = CertHelper.GetOrCreateTestCert();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority("https://login.microsoftonline.com/tid")
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .BuildConcrete();

                harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);

                // First request
                AuthenticationResult result1 = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraClientAssertionClaims(_clientAssertionClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result1.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                // Second request with same claims should use cache
                AuthenticationResult result2 = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraClientAssertionClaims(_clientAssertionClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result2.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(result1.AccessToken, result2.AccessToken);
            }
        }

        [TestMethod]
        public async Task WithExtraClientAssertionClaims_ComplexNestedClaims_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var certificate = CertHelper.GetOrCreateTestCert();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority("https://login.microsoftonline.com/tid")
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .Build();

                var handler = harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_ClientCredentials);
                JwtSecurityToken assertion = null;
                handler.AdditionalRequestValidation = (r) =>
                {
                    var requestContent = r.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var formsData = CoreHelpers.ParseKeyValueList(requestContent, '&', true, null);

                    Assert.IsTrue(formsData.TryGetValue("client_assertion", out string encodedJwt), "Missing client_assertion from request");

                    var jwtHandler = new JwtSecurityTokenHandler();
                    assertion = jwtHandler.ReadJwtToken(encodedJwt);

                    // Validate nested claims
                    var customClaimsClaim = assertion.Claims.FirstOrDefault(c => c.Type == "custom_claims");
                    Assert.IsNotNull(customClaimsClaim, "custom_claims should be present");

                    // Parse the nested claim value as JSON
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(customClaimsClaim.Value);
                    Assert.AreEqual(JsonValueKind.Object, jsonElement.ValueKind, "custom_claims should be a JSON object");
                    
                    // Validate xms_az_tm property
                    Assert.IsTrue(jsonElement.TryGetProperty("xms_az_tm", out var tmProperty));
                    Assert.AreEqual("azureinfra", tmProperty.GetString());
                    
                    // Validate xms_az_nwperimid array property
                    Assert.IsTrue(jsonElement.TryGetProperty("xms_az_nwperimid", out var nwperimidProperty));
                    Assert.AreEqual(JsonValueKind.Array, nwperimidProperty.ValueKind);
                    
                    var arrayElements = nwperimidProperty.EnumerateArray().ToList();
                    Assert.AreEqual(2, arrayElements.Count);
                    Assert.AreEqual("00000000-1403-0100-0000-000000000000", arrayElements[0].GetString());
                    Assert.AreEqual("00000000-dc4b-4eb1-9fa3-902c8d13b5bd", arrayElements[1].GetString());
                };

                AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                    .WithExtraClientAssertionClaims(_clientAssertionClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.IsNotNull(assertion);
            }
        }

        [TestMethod]
        public async Task WithExtraClientAssertionClaims_WorksWithOboFlow_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var certificate = CertHelper.GetOrCreateTestCert();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority("https://login.microsoftonline.com/tid")
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .Build();

                var handler = harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_UserFlows);
                JwtSecurityToken assertion = null;
                handler.AdditionalRequestValidation = (r) =>
                {
                    var requestContent = r.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var formsData = CoreHelpers.ParseKeyValueList(requestContent, '&', true, null);

                    Assert.IsTrue(formsData.TryGetValue("client_assertion", out string encodedJwt), "Missing client_assertion from request");

                    var jwtHandler = new JwtSecurityTokenHandler();
                    assertion = jwtHandler.ReadJwtToken(encodedJwt);

                    // Validate extra claims are present in the client assertion
                    var customClaimsClaim = assertion.Claims.FirstOrDefault(c => c.Type == "custom_claims");
                    Assert.IsNotNull(customClaimsClaim, "custom_claims should be present");

                    // Parse the nested claim value as JSON
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(customClaimsClaim.Value);
                    Assert.AreEqual(JsonValueKind.Object, jsonElement.ValueKind, "custom_claims should be a JSON object");
                    
                    // Validate nested properties
                    Assert.IsTrue(jsonElement.TryGetProperty("xms_az_tm", out var tmProperty));
                    Assert.AreEqual("azureinfra", tmProperty.GetString());
                    
                    Assert.IsTrue(jsonElement.TryGetProperty("xms_az_nwperimid", out var nwperimidProperty));
                    Assert.AreEqual(JsonValueKind.Array, nwperimidProperty.ValueKind);
                    Assert.AreEqual(2, nwperimidProperty.GetArrayLength());

                    // Verify it's an OBO flow by checking for the user assertion
                    Assert.IsTrue(formsData.TryGetValue("grant_type", out string grantType));
                    Assert.AreEqual("urn:ietf:params:oauth:grant-type:jwt-bearer", grantType);
                    Assert.IsTrue(formsData.TryGetValue("assertion", out string userAssertion));
                    Assert.AreEqual(TestConstants.DefaultAccessToken, userAssertion);
                };

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                AuthenticationResult result = await app.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .WithExtraClientAssertionClaims(_clientAssertionClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.IsNotNull(assertion);
            }
        }

        [TestMethod]
        public async Task WithExtraClientAssertionClaims_OboFlow_DifferentClaims_ResultsInDifferentCacheEntries_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var certificate = CertHelper.GetOrCreateTestCert();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority("https://login.microsoftonline.com/tid")
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .BuildConcrete();

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                // First request with _clientAssertionClaims
                harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_UserFlows);

                AuthenticationResult result1 = await app.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .WithExtraClientAssertionClaims(_clientAssertionClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result1.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                // Second request with different claims should go to IdP
                harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_UserFlows);
                string extraClaims2 = "{\"claim2\":\"value2\"}";

                AuthenticationResult result2 = await app.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .WithExtraClientAssertionClaims(extraClaims2)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result2.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task WithExtraClientAssertionClaims_OboFlow_SameClaims_UsesCache_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var certificate = CertHelper.GetOrCreateTestCert();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority("https://login.microsoftonline.com/tid")
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .BuildConcrete();

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                harness.HttpManager.AddTokenResponse(TokenResponseType.Valid_UserFlows);

                // First request
                AuthenticationResult result1 = await app.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .WithExtraClientAssertionClaims(_clientAssertionClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result1.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

                // Second request with same claims should use cache
                AuthenticationResult result2 = await app.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .WithExtraClientAssertionClaims(_clientAssertionClaims)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result2.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result2.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(result1.AccessToken, result2.AccessToken);
            }
        }
        private void BeforeCacheAccess(TokenCacheNotificationArgs args)
        {
            args.TokenCache.DeserializeMsalV3(_serializedCache);
        }

        private void AfterCacheAccess(TokenCacheNotificationArgs args)
        {
            _serializedCache = args.TokenCache.SerializeMsalV3();
        }

        private static string ComputeCertThumbprint(X509Certificate2 certificate, bool useSha2)
        {
            string thumbprint = null;

            if (useSha2)
            {
                thumbprint = Base64UrlHelpers.Encode(certificate.GetCertHash(HashAlgorithmName.SHA256));
            }
            else
            {
                thumbprint = Base64UrlHelpers.Encode(certificate.GetCertHash());
            }

            return thumbprint;
        }
    }
}
#endif
