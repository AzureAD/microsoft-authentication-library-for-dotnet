// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Extensions.Msal;
#if NET_CORE
using Microsoft.Identity.Client.Broker;
#endif
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.IdentityModel.Protocols.SignedHttpRequest;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.Identity.Client.TelemetryCore.TelemetryClient;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Internal;
using System.Security.Claims;
using System.Net.Sockets;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.ConstrainedExecution;
using System.Collections.Generic;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]    
    public class PoPTests
    {

        private static readonly string[] s_keyvaultScope = { "https://vault.azure.net/.default" };
        private static readonly string[] s_ropcScope = { "User.Read" };

        private const string ProtectedUrl = "https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b";

        [TestInitialize]
        public void TestInitialize()
        {
            ApplicationBase.ResetStateForTest();
        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task PoP_MultipleKeys_Async()
        {
            await MultipleKeys_Async().ConfigureAwait(false);
        }

        [DoNotRunOnLinux] // POP is not supported on Linux
        [RunOn(TargetFrameworks.NetCore)]
        public async Task PoP_BearerAndPoP_CanCoexist_Async()
        {
            await BearerAndPoP_CanCoexist_Async().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task HappyPath_Async()
        {            
            var popConfig = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl));
            popConfig.HttpMethod = HttpMethod.Get;

            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            var clientCertificate = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(appConfig.Authority)
                .WithCertificate(clientCertificate, sendX5C: true)
                .WithExperimentalFeatures(true)
                .WithTestLogging()
                .Build();

            var result = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .WithSignedHttpRequestProofOfPossession(popConfig)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            PoPValidator.VerifyPoPToken(
                 appConfig.AppId,
                 ProtectedUrl,
                 HttpMethod.Get,
                 result);
        }

        private async Task BearerAndPoP_CanCoexist_Async()
        {
            // Arrange
            var popConfig = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl));
            popConfig.HttpMethod = HttpMethod.Get;

            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            string secret = LabResponseHelper.FetchSecretString(appConfig.SecretName, LabResponseHelper.KeyVaultSecretsProviderMsal);

            var cca = ConfidentialClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithClientSecret(secret)
                .WithExperimentalFeatures(true)
                .WithTestLogging()
                .WithAuthority(appConfig.Authority).Build();
            ConfigureInMemoryCache(cca);

            // Act - acquire both a PoP and a Bearer token
            Trace.WriteLine("Getting a PoP token");
            AuthenticationResult result = await cca
                .AcquireTokenForClient(s_keyvaultScope)
                .WithSignedHttpRequestProofOfPossession(popConfig)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            PoPValidator.VerifyPoPToken(
                appConfig.AppId,
                ProtectedUrl,
                HttpMethod.Get,
                result);

            Trace.WriteLine("Getting a Bearer token");
            result = await cca
                .AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync()
                .ConfigureAwait(false);
            Assert.AreEqual("Bearer", result.TokenType);
            Assert.AreEqual(
                2,
                (cca as ConfidentialClientApplication).AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
        }

        private async Task MultipleKeys_Async()
        {

            var cryptoProvider = new RSACertificatePopCryptoProvider(GetCertificate());

            var popConfig1 = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl));
            popConfig1.HttpMethod = HttpMethod.Get;
            popConfig1.PopCryptoProvider = cryptoProvider;
            const string OtherProtectedUrl = "https://www.bing.com/path3/path4?queryParam5=c&queryParam6=d";
            var popConfig2 = new PoPAuthenticationConfiguration(new Uri(OtherProtectedUrl));
            popConfig2.HttpMethod = HttpMethod.Post;
            popConfig2.PopCryptoProvider = cryptoProvider;

            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            string secret = LabResponseHelper.FetchSecretString(appConfig.SecretName, LabResponseHelper.KeyVaultSecretsProviderMsal);

            var cca = ConfidentialClientApplicationBuilder.Create(appConfig.AppId)
                .WithTestLogging()
                .WithAuthority(appConfig.Authority)
                .WithClientSecret(secret)
                .WithExperimentalFeatures(true)
                .Build();
            ConfigureInMemoryCache(cca);

            var result = await cca
                .AcquireTokenForClient(s_keyvaultScope)
                .WithSignedHttpRequestProofOfPossession(popConfig1)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            PoPValidator.VerifyPoPToken(
                appConfig.AppId,
                ProtectedUrl,
                HttpMethod.Get,
                result);

            // recreate the pca to ensure that the silent call is served from the cache, i.e. the key remains stable
            cca = ConfidentialClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(appConfig.Authority)
                .WithClientSecret(secret)
                .WithExperimentalFeatures(true)
                .WithHttpClientFactory(new NoAccessHttpClientFactory()) // token should be served from the cache, no network access necessary
                .Build();
            ConfigureInMemoryCache(cca);

            result = await cca
                .AcquireTokenForClient(s_keyvaultScope)
                .WithSignedHttpRequestProofOfPossession(popConfig1)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual("pop", result.TokenType);

            PoPValidator.VerifyPoPToken(
                appConfig.AppId,
                ProtectedUrl,
                HttpMethod.Get,
                result);

            // Call some other Uri - the same pop assertion can be reused, i.e. no need to call Evo
            result = await cca
                .AcquireTokenForClient(s_keyvaultScope)
                .WithSignedHttpRequestProofOfPossession(popConfig2)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

            PoPValidator.VerifyPoPToken(
                appConfig.AppId,
                OtherProtectedUrl,
                HttpMethod.Post,
                result);
        }

        [DoNotRunOnLinux] // POP is not supported on Linux
        [RunOn(TargetFrameworks.NetCore)]
        public async Task PopTestWithConfigObjectAsync()
        {
            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            string secret = LabResponseHelper.FetchSecretString(appConfig.SecretName, LabResponseHelper.KeyVaultSecretsProviderMsal);

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(appConfig.Authority)
                .WithClientSecret(secret)
                .WithExperimentalFeatures(true)
                .WithTestLogging()
                .Build();

            var popConfig = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl));
            popConfig.PopCryptoProvider = new RSACertificatePopCryptoProvider(GetCertificate());
            popConfig.HttpMethod = HttpMethod.Get;

            var result = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .WithSignedHttpRequestProofOfPossession(popConfig)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            PoPValidator.VerifyPoPToken(
                appConfig.AppId,
                ProtectedUrl,
                HttpMethod.Get,
                result);

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(result.AccessToken);
            var alg = token.Header.Alg;

            // Check the algorithm
            Assert.AreEqual("RS256", alg, "The algorithm in the token header should be RS256");
        }

        [DoNotRunOnLinux] // POP is not supported on Linux
        [TestMethod]
        public async Task PopTestWithRSAAsync()
        {
            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            string secret = LabResponseHelper.FetchSecretString(appConfig.SecretName, LabResponseHelper.KeyVaultSecretsProviderMsal);

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(appConfig.Authority)
                .WithClientSecret(secret)
                .WithExperimentalFeatures(true)
                .Build();

            //RSA provider
            var popConfig = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl));
            popConfig.PopCryptoProvider = new RSACertificatePopCryptoProvider(GetCertificate());
            popConfig.HttpMethod = HttpMethod.Get;

            var result = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .WithSignedHttpRequestProofOfPossession(popConfig)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            PoPValidator.VerifyPoPToken(
                appConfig.AppId,
                ProtectedUrl,
                HttpMethod.Get,
                result);

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(result.AccessToken);
            var alg = token.Header.Alg;

            // Check the algorithm
            Assert.AreEqual("RS256", alg, "The algorithm in the token header should be RS256");
        }

        [DoNotRunOnLinux] // POP is not supported on Linux
        [RunOn(TargetFrameworks.NetCore)]
        public async Task ROPC_PopTestWithRSAAsync()
        {
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            string secret = LabResponseHelper.FetchSecretString(app.SecretName, LabResponseHelper.KeyVaultSecretsProviderMsal);

            // Use the lab response app and tenant for consistency instead of mixing configurations
            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(app.AppId)
                .WithAuthority($"https://login.microsoftonline.com/{user.TenantId}")
                .WithClientSecret(secret)
                .WithExperimentalFeatures(true)
                .Build();

            //RSA provider
            var popConfig = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl));
            popConfig.PopCryptoProvider = new RSACertificatePopCryptoProvider(GetCertificate());
            popConfig.HttpMethod = HttpMethod.Get;

            var result = await (confidentialApp as IByUsernameAndPassword).AcquireTokenByUsernamePassword(s_ropcScope, user.Upn, user.GetOrFetchPassword())
                .WithSignedHttpRequestProofOfPossession(popConfig)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            PoPValidator.VerifyPoPToken(
                app.AppId, // Use consistent app ID from lab response
                ProtectedUrl,
                HttpMethod.Get,
                result);

        }

        [TestMethod]
        public async Task PopTest_WithCustomClaims_Async()
        {
            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            var clientCertificate = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(appConfig.Authority)
                .WithCertificate(clientCertificate)
                .WithExperimentalFeatures(true)
                .Build();

            // Use the client certificate to create signing credentials. This avoids maintaining 2 keys (client cert and POP key) and provides good security.
            // it also helps cache the POP tokens, since the same key can be used on multiple machines.
            // But a separate key can be used.
            
            var popCredentials = new SigningCredentials(new X509SecurityKey(clientCertificate), SecurityAlgorithms.RsaSha256);
            var popConfig = new PoPAuthenticationConfiguration()
            {
                PopCryptoProvider = new SigningCredentialsToPopCryptoProviderAdapter(popCredentials, true),
                SignHttpRequest = false,
            };

            // this fetches the POP assertion, i.e. an incomplete POP token. The client still needs to add SHR and to sign it.
            var result = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .WithSignedHttpRequestProofOfPossession(popConfig)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            Assert.AreEqual(
                TokenSource.IdentityProvider,
                result.AuthenticationResultMetadata.TokenSource);

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(result.AccessToken);
            var alg = token.Header.Alg;

            // Check the algorithm
            Assert.AreEqual("RS256", alg, "The algorithm in the token header should be RS256");

            // Create custom SHR with additional claims.
            SignedHttpRequestDescriptor signedHttpRequestDescriptor =
                new SignedHttpRequestDescriptor(
                    result.AccessToken,
                    new IdentityModel.Protocols.HttpRequestData()
                    {
                        Uri = new Uri(ProtectedUrl),
                        Method = HttpMethod.Post.ToString(),                         
                    },
                    popCredentials)
                {
                    AdditionalPayloadClaims = new Dictionary<string, object>()
                    {
                        { "custom_parameter", "custom_value" }
                    }
                };

            var signedHttpRequestHandler = new SignedHttpRequestHandler();
            string finalPopToken = signedHttpRequestHandler.CreateSignedHttpRequest(signedHttpRequestDescriptor);

            var claims = PoPValidator.VerifyPoPToken(
                appConfig.AppId,
                ProtectedUrl,
                HttpMethod.Post,
                finalPopToken, "pop");

            Claim customClaim = claims.FindFirst("custom_parameter");
            Assert.IsNotNull(customClaim, "custom_parameter claim should be present in the token");
            Assert.AreEqual("custom_value", customClaim.Value, "custom_parameter claim should have the expected value");

            var result2 = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
             .WithSignedHttpRequestProofOfPossession(popConfig)
             .ExecuteAsync(CancellationToken.None)
             .ConfigureAwait(false);
            Assert.AreEqual(
                TokenSource.Cache,
                result2.AuthenticationResultMetadata.TokenSource);
        }
        
        [DoNotRunOnLinux] // POP is not supported on Linux
        [TestMethod]
        public async Task PopTestWithECDAsync()
        {
            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            string secret = LabResponseHelper.FetchSecretString(appConfig.SecretName, LabResponseHelper.KeyVaultSecretsProviderMsal);

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(appConfig.Authority)
                .WithClientSecret(secret)
                .WithExperimentalFeatures(true)
                .Build();

            //ECD Provider
            var popConfig = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl));
            popConfig.PopCryptoProvider = new ECDCertificatePopCryptoProvider();
            popConfig.HttpMethod = HttpMethod.Post;

            var result = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .WithSignedHttpRequestProofOfPossession(popConfig)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(result.AccessToken);
            var alg = token.Header.Alg;

            // Check the algorithm
            Assert.AreEqual("ES256", alg, "The algorithm in the token header should be ES256");

            PoPValidator.VerifyPoPToken(
                appConfig.AppId,
                ProtectedUrl,
                HttpMethod.Post,
                result);
        }

        [DoNotRunOnLinux] // POP is not supported on Linux
        [TestMethod]
        public async Task NewPOP_WithKeyIdOnly_Async()
        {
            // Arrange - outside MSAL

            // 1.1. Create an RSA key (here using Wilson primitives, but vanialla crypto primitives also work, see ComputeCannonicalJwk bellow for example
            RsaSecurityKey popKey = CreateRsaSecurityKey();
            // 1.2. Get the JWK and base64 encode it
            string base64EncodedJwk = Base64UrlHelpers.Encode(popKey.ComputeJwkThumbprint());
            // 1.3. Put it in JSON format
            var reqCnf = $@"{{""kid"":""{base64EncodedJwk}""}}";
            // 1.4. Base64 encode it again
            var keyId = Base64UrlHelpers.Encode(reqCnf);

            // Arrange MSALfin

            // 2. Create a normal CCA 
            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            string secret = LabResponseHelper.FetchSecretString(appConfig.SecretName, LabResponseHelper.KeyVaultSecretsProviderMsal);

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithExperimentalFeatures()
                .WithAuthority(appConfig.Authority)
                .WithClientSecret(secret)
                .Build();

            // 3. When acquiring a token, use WithPopKeyId and OnBeforeTokenRequest extensiblity methods
            var result = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                 .WithProofOfPosessionKeyId(keyId, "pop")       // ensure tokens are bound to the key_id
                 .OnBeforeTokenRequest((data) =>
                 {
                     // add extra data to request
                     data.BodyParameters.Add("req_cnf", keyId);
                     data.BodyParameters.Add("token_type", "pop");

                     return Task.CompletedTask;
                 })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            Assert.AreEqual(
                TokenSource.IdentityProvider,
                result.AuthenticationResultMetadata.TokenSource);

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(result.AccessToken);
            var alg = token.Header.Alg;

            // Check the algorithm
            Assert.AreEqual("RS256", alg, "The algorithm in the token header should be RS256");

            // Outside MSAL - Create the SHR (using Wilson)

            var popCredentials = new SigningCredentials(popKey, SecurityAlgorithms.RsaSha256);
            SignedHttpRequestDescriptor signedHttpRequestDescriptor =
               new SignedHttpRequestDescriptor(
                   result.AccessToken,
                   new IdentityModel.Protocols.HttpRequestData()
                   {
                       Uri = new Uri(ProtectedUrl),
                       Method = HttpMethod.Post.ToString()
                   },
                   popCredentials);

            var signedHttpRequestHandler = new SignedHttpRequestHandler();
            string req = signedHttpRequestHandler.CreateSignedHttpRequest(signedHttpRequestDescriptor);

            // play the POP token against a webservice that accepts POP to validate the keys
            PoPValidator.VerifyPoPToken(
                appConfig.AppId,
                ProtectedUrl,
                HttpMethod.Post,
                req, "pop");

            // Additional check - if using the same key, the token should come from the cache
            var result2 = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .WithProofOfPosessionKeyId(keyId, "pop")       // ensure tokens are bound to the key_id
                .OnBeforeTokenRequest((data) =>
                {
                    // add extra data to request
                    data.BodyParameters.Add("req_cnf", keyId);
                    data.BodyParameters.Add("token_type", "pop");

                    return Task.CompletedTask;
                })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
            Assert.AreEqual(
                TokenSource.Cache,
                result2.AuthenticationResultMetadata.TokenSource);
        }

        [DoNotRunOnLinux] // POP is not supported on Linux
        [TestMethod]
        public async Task InMemoryCryptoProvider_AlgIsPS256()
        {
            // Arrange - create a Confidential Client Application with PoP configuration
            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            string secret = LabResponseHelper.FetchSecretString(appConfig.SecretName, LabResponseHelper.KeyVaultSecretsProviderMsal);

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(appConfig.Authority)
                .WithClientSecret(secret)
                .WithExperimentalFeatures(true)
                .Build();

            // Create a new InMemoryCryptoProvider and get its JWK
            var cryptoProvider = new InMemoryCryptoProvider();
            var canonicalJwk = cryptoProvider.CannonicalPublicKeyJwk;
            var base64EncodedJwk = Base64UrlHelpers.Encode(Encoding.UTF8.GetBytes(canonicalJwk));

            // Calculate the expected kid
            string expectedKid;
            using (var sha256 = SHA256.Create())
            {
                var kidBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonicalJwk));
                expectedKid = Base64UrlHelpers.Encode(kidBytes);
            }

            // Use InMemoryCryptoProvider
            var popConfig = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl))
            {
                PopCryptoProvider = cryptoProvider,
                HttpMethod = HttpMethod.Get
            };

            var result = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .WithSignedHttpRequestProofOfPossession(popConfig)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert token type
            Assert.AreEqual("pop", result.TokenType);

            // Validate the token
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(result.AccessToken);
            var alg = token.Header.Alg;
            var kid = token.Header.Kid;

            // Check the algorithm and kid
            Assert.AreEqual("PS256", alg, "The algorithm in the token header should be PS256");
            Assert.AreEqual(expectedKid, kid, "The kid in the token header should match the generated key");
        }

        [Ignore("This test is ignored because it is not ready yet.")]
        [TestMethod]
        public async Task InMemoryCryptoProvider_WithGraph()
        {
            // Arrange - create a Confidential Client Application with PoP configuration
            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            string secret = LabResponseHelper.FetchSecretString(appConfig.SecretName, LabResponseHelper.KeyVaultSecretsProviderMsal);

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(appConfig.Authority)
                .WithClientSecret(secret)
                .WithExperimentalFeatures(true)
                .Build();

            // Create a new InMemoryCryptoProvider and get its JWK
            var cryptoProvider = new InMemoryCryptoProvider();
            var canonicalJwk = cryptoProvider.CannonicalPublicKeyJwk;
            var base64EncodedJwk = Base64UrlHelpers.Encode(Encoding.UTF8.GetBytes(canonicalJwk));

            // Calculate the expected kid
            string expectedKid;
            using (var sha256 = SHA256.Create())
            {
                var kidBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonicalJwk));
                expectedKid = Base64UrlHelpers.Encode(kidBytes);
            }

            // Use InMemoryCryptoProvider
            var popConfig = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl))
            {
                PopCryptoProvider = cryptoProvider,
                HttpMethod = HttpMethod.Get
            };

            var result = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .WithSignedHttpRequestProofOfPossession(popConfig)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert token type
            Assert.AreEqual("pop", result.TokenType);

            // Validate the token
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(result.AccessToken);
            var alg = token.Header.Alg;
            var kid = token.Header.Kid;

            // Check the algorithm and kid
            Assert.AreEqual("PS256", alg, "The algorithm in the token header should be PS256");
            Assert.AreEqual(expectedKid, kid, "The kid in the token header should match the generated key");

            // Integration Test: Call the Graph API and test for success
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/users").ConfigureAwait(false);

            // Check for WWW-Authenticate header
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized, "The response should be Unauthorized (401)");

            // Extract WWW-Authenticate header to get the nonce
            var authParams = await WwwAuthenticateParameters.CreateFromAuthenticationResponseAsync(
                "https://graph.microsoft.com/v1.0/users", "Pop").ConfigureAwait(false);

            // Use the nonce to acquire a PoP token
            var popConfigWithNonce = new PoPAuthenticationConfiguration(new Uri("https://graph.microsoft.com/v1.0/users"))
            {
                PopCryptoProvider = cryptoProvider,
                HttpMethod = HttpMethod.Get,
                Nonce = authParams.Nonce
            };

            var resultWithNonce = await confidentialApp.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
                .WithSignedHttpRequestProofOfPossession(popConfigWithNonce)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Make a new request with the PoP token to access a specific application
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("pop", resultWithNonce.AccessToken);
            var responseWithPopToken = await httpClient.GetAsync($"https://graph.microsoft.com/v1.0/users").ConfigureAwait(false);

            response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/applications").ConfigureAwait(false);

            // Check for success
            Assert.IsTrue(response.IsSuccessStatusCode, "The response should be successful");
            var applications = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // Assert that the response is successful
            Assert.IsTrue(responseWithPopToken.IsSuccessStatusCode, "The response should be successful with the PoP token");
        }

        [DoNotRunOnLinux] // POP is not supported on Linux
        [TestMethod]
        public async Task PoPToken_ShouldHaveCorrectAlgorithm_PS256_Async()
        {
            // Arrange
            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            string secret = LabResponseHelper.FetchSecretString(appConfig.SecretName, LabResponseHelper.KeyVaultSecretsProviderMsal);
            
            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(appConfig.Authority)
                .WithClientSecret(secret)
                .WithExperimentalFeatures(true)
                .Build();

            var popConfig = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl))
            {
                PopCryptoProvider = new InMemoryCryptoProvider(),
                HttpMethod = HttpMethod.Get
            };

            // Act
            var result = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .WithSignedHttpRequestProofOfPossession(popConfig)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            // Assert token type
            Assert.AreEqual("pop", result.TokenType);

            // Validate the token
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(result.AccessToken);
            var alg = token.Header.Alg;
            var kid = token.Header.Kid;

            // Check the algorithm
            Assert.AreEqual("PS256", alg, "The algorithm in the token header should be PS256");
        }

#if NET_CORE
        [DoNotRunOnLinux] // POP is not supported on Linux
        [IgnoreOnOneBranch]
        public async Task WamUsernamePasswordRequestWithPOPAsync()
        {
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppPCAClient).ConfigureAwait(false);
            string[] scopes = { "User.Read" };

            WamLoggerValidator wastestLogger = new WamLoggerValidator();

            IPublicClientApplication pca = PublicClientApplicationBuilder
               .Create(app.AppId)
               .WithAuthority(app.Authority, "organizations")
               .WithLogging(wastestLogger)
               .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
               .Build();

            Assert.IsTrue(pca.IsProofOfPossessionSupportedByClient(), "Either the broker is not configured or it does not support POP.");

            #pragma warning disable CS0618 // Type or member is obsolete
            var result = await pca
                .AcquireTokenByUsernamePassword(
                    scopes,
                    user.Upn,
                    user.GetOrFetchPassword())
                .WithProofOfPossession("nonce", HttpMethod.Get, new Uri(ProtectedUrl))
                .ExecuteAsync().ConfigureAwait(false);
            #pragma warning restore CS0618

            MsalAssert.AssertAuthResult(result, TokenSource.Broker, user.TenantId, scopes, true);

            Assert.IsTrue(wastestLogger.HasLogged);

            PoPValidator.VerifyPoPToken(
                app.AppId,
                ProtectedUrl,
                HttpMethod.Get,
                result);
        }

        [TestMethod]
        public void CheckPopRuntimeBrokerSupportTest()
        {
            //Broker enabled
            if (SharedUtilities.IsWindowsPlatform()) {
                CheckPopSupport(new BrokerOptions(BrokerOptions.OperatingSystems.Windows), true);
            }

            //Broker disabled
            CheckPopSupport(new BrokerOptions(BrokerOptions.OperatingSystems.None), false);

            // POP is not supported on Linux
            if (SharedUtilities.IsLinuxPlatform()) {
                CheckPopSupport(new BrokerOptions(BrokerOptions.OperatingSystems.Linux), false);
            }
        }
        
        private static void CheckPopSupport(BrokerOptions brokerOptions, bool isPopSupported)
        {
            var pcaBuilder = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId);

            pcaBuilder = pcaBuilder.WithBroker(brokerOptions);

            IPublicClientApplication app = pcaBuilder.Build();

            Assert.AreEqual(isPopSupported, app.IsProofOfPossessionSupportedByClient());
        }
#endif

        private static X509Certificate2 GetCertificate()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            
            if (cert == null)
            {
                throw new InvalidOperationException(
                    "Test setup error - cannot find a certificate in the My store for KeyVault. This is available for Microsoft employees only.");
            }

            return cert;
        }

        private class NoAccessHttpClientFactory : IMsalHttpClientFactory
        {
            private const string Message = "Not expecting to make HTTP requests.";

            public HttpClient GetHttpClient()
            {
                Assert.Fail(Message);
                throw new InvalidOperationException(Message);
            }
        }     

        private string _inMemoryCache = "{}";
        private void ConfigureInMemoryCache(IConfidentialClientApplication pca)
        {
            pca.AppTokenCache.SetBeforeAccess(notificationArgs =>
            {
                byte[] bytes = Encoding.UTF8.GetBytes(_inMemoryCache);
                notificationArgs.TokenCache.DeserializeMsalV3(bytes);
            });

            pca.AppTokenCache.SetAfterAccess(notificationArgs =>
            {
                if (notificationArgs.HasStateChanged)
                {
                    byte[] bytes = notificationArgs.TokenCache.SerializeMsalV3();
                    _inMemoryCache = Encoding.UTF8.GetString(bytes);
                }
            });
        }

        private static RsaSecurityKey CreateRsaSecurityKey()
        {
#if NETFRAMEWORK
            RSA rsa = RSA.Create(2048);
#else
            RSA rsa = new RSACryptoServiceProvider(2048);
#endif
            // the reason for creating the RsaSecurityKey from RSAParameters is so that a SignatureProvider created with this key
            // will own the RSA object and dispose it. If we pass a RSA object, the SignatureProvider does not own the object, the RSA object will not be disposed.
            RSAParameters rsaParameters = rsa.ExportParameters(true);
            RsaSecurityKey rsaSecurityKey = new RsaSecurityKey(rsaParameters) { KeyId = CreateRsaKeyId(rsaParameters) };
            rsa.Dispose();
            return rsaSecurityKey;
        }

        private static string CreateRsaKeyId(RSAParameters rsaParameters)
        {
            byte[] kidBytes = new byte[rsaParameters.Exponent.Length + rsaParameters.Modulus.Length];
            Array.Copy(rsaParameters.Exponent, 0, kidBytes, 0, rsaParameters.Exponent.Length);
            Array.Copy(rsaParameters.Modulus, 0, kidBytes, rsaParameters.Exponent.Length, rsaParameters.Modulus.Length);
            using (var sha2 = SHA256.Create())
                return Base64UrlEncoder.Encode(sha2.ComputeHash(kidBytes));
        }
    }
}
