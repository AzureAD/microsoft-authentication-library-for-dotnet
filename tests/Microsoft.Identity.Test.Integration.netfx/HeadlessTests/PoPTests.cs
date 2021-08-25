// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.PlatformsCommon;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.net45;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{

    // Note: these tests require permission to a KeyVault Microsoft account;
    // Please ignore them if you are not a Microsoft FTE, they will run as part of the CI build
    [TestClass]
    public class PoPTests
    {
        // This endpoint is hosted in the MSID Lab and is able to verify any pop token bound to an HTTP request
        private const string PoPValidatorEndpoint = "https://signedhttprequest.azurewebsites.net/api/validateSHR";

        private static readonly string[] s_keyvaultScope = { "https://vault.azure.net/.default" };

        private const string PublicCloudConfidentialClientID = "16dab2ba-145d-4b1b-8569-bf4b9aed4dc8";
        private const string PublicCloudTestAuthority = "https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47";
        private const string ProtectedUrl = "https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b";
        private static string s_publicCloudCcaSecret;
        private KeyVaultSecretsProvider _keyVault;

        private string _popValidationEndpointSecret;     

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
            if (_popValidationEndpointSecret == null)
            {
                _popValidationEndpointSecret = LabUserHelper.KeyVaultSecretsProvider.GetSecret(
                    "https://buildautomation.vault.azure.net/secrets/automation-pop-validation-endpoint/841fc7c2ccdd48d7a9ef727e4ae84325").Value;
            }

            if (_keyVault == null)
            {
                _keyVault = new KeyVaultSecretsProvider();
                s_publicCloudCcaSecret = _keyVault.GetSecret(TestConstants.MsalCCAKeyVaultUri).Value;
            }
        }

        [TestMethod]
        public async Task PoP_MultipleKeys_Async()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            await MultipleKeys_Async(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task PoP_BearerAndPoP_CanCoexist_Async()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            await BearerAndPoP_CanCoexist_Async().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task HappyPath_Async()
        {
            await RunTestWithClientSecretAsync(PublicCloudConfidentialClientID, PublicCloudTestAuthority, s_publicCloudCcaSecret).ConfigureAwait(false);
        }

        private async Task BearerAndPoP_CanCoexist_Async()
        {
            // Arrange
            var popConfig = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl));
            popConfig.HttpMethod = HttpMethod.Get;

            var pca = ConfidentialClientApplicationBuilder
                .Create(PublicCloudConfidentialClientID)
                .WithExperimentalFeatures()
                .WithClientSecret(s_publicCloudCcaSecret)
                .WithTestLogging()
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs).Build();
            ConfigureInMemoryCache(pca);

            // Act - acquire both a PoP and a Bearer token
            Trace.WriteLine("Getting a PoP token");
            AuthenticationResult result = await pca
                .AcquireTokenForClient(s_keyvaultScope)
                .WithExtraQueryParameters(GetTestSliceParams())
                .WithProofOfPossession(popConfig)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            await VerifyPoPTokenAsync(PublicCloudConfidentialClientID, popConfig, result).ConfigureAwait(false);

            Trace.WriteLine("Getting a Bearer token");
            result = await pca
                .AcquireTokenForClient(s_keyvaultScope)
                .ExecuteAsync()
                .ConfigureAwait(false);
            Assert.AreEqual("Bearer", result.TokenType);
            Assert.AreEqual(
                2,
                (pca as ConfidentialClientApplication).AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
        }

        private async Task MultipleKeys_Async(LabResponse labResponse)
        {
            var popConfig1 = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl));
            popConfig1.HttpMethod = HttpMethod.Get;
            var popConfig2 = new PoPAuthenticationConfiguration(new Uri("https://www.bing.com/path3/path4?queryParam5=c&queryParam6=d"));
            popConfig2.HttpMethod = HttpMethod.Post;

            var user = labResponse.User;

            var pca = ConfidentialClientApplicationBuilder.Create(PublicCloudConfidentialClientID)
                .WithExperimentalFeatures()
                .WithTestLogging()
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .WithClientSecret(s_publicCloudCcaSecret).Build();
            ConfigureInMemoryCache(pca);

            var result = await pca
                .AcquireTokenForClient(s_keyvaultScope)
                .WithExtraQueryParameters(GetTestSliceParams())
                .WithProofOfPossession(popConfig1)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            await VerifyPoPTokenAsync(
                 PublicCloudConfidentialClientID,
                 popConfig1, result).ConfigureAwait(false);

            // recreate the pca to ensure that the silent call is served from the cache, i.e. the key remains stable
            pca = ConfidentialClientApplicationBuilder
                .Create(PublicCloudConfidentialClientID)
                .WithExperimentalFeatures()
                .WithClientSecret(s_publicCloudCcaSecret)
                .WithHttpClientFactory(new NoAccessHttpClientFactory()) // token should be served from the cache, no network access necessary
                .Build();
            ConfigureInMemoryCache(pca);

#pragma warning disable CS0618 // Type or member is obsolete
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete
            result = await pca
                .AcquireTokenSilent(s_keyvaultScope, accounts.Single())
                .WithProofOfPossession(popConfig1)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            await VerifyPoPTokenAsync(
                PublicCloudConfidentialClientID,
                popConfig1, result).ConfigureAwait(false);

            // Call some other Uri - the same pop assertion can be reused, i.e. no need to call Evo
            result = await pca
              .AcquireTokenSilent(s_keyvaultScope, accounts.Single())
              .WithProofOfPossession(popConfig2)
              .ExecuteAsync()
              .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            await VerifyPoPTokenAsync(
                PublicCloudConfidentialClientID,
                popConfig2, result).ConfigureAwait(false);
        }

        public async Task RunTestWithClientSecretAsync(string clientID, string authority, string secret)
        {
            var popConfig = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl));
            popConfig.HttpMethod = HttpMethod.Get;

            var confidentialClientAuthority = authority;

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(clientID)
                .WithExperimentalFeatures()
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithClientSecret(secret)
                .WithTestLogging()
                .Build();

            var result = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .WithProofOfPossession(popConfig)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            await VerifyPoPTokenAsync(
                clientID,
                popConfig, result).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task PopTestWithConfigObjectAsync()
        {
            var confidentialClientAuthority = PublicCloudTestAuthority;

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(PublicCloudConfidentialClientID)
                .WithExperimentalFeatures()
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithClientSecret(s_publicCloudCcaSecret)
                .WithTestLogging()
                .Build();

            var popConfig = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl));
            popConfig.PopCryptoProvider = new RSACertificatePopCryptoProvider(GetCertificate());
            popConfig.HttpMethod = HttpMethod.Get;

            var result = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .WithProofOfPossession(popConfig)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            await VerifyPoPTokenAsync(
                PublicCloudConfidentialClientID,
                popConfig, result).ConfigureAwait(false);
        }


        [TestMethod]
        public async Task PopTestWithRSAAsync()
        {
            var confidentialClientAuthority = PublicCloudTestAuthority;

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(PublicCloudConfidentialClientID)
                .WithExperimentalFeatures()
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithClientSecret(s_publicCloudCcaSecret)
                .Build();

            //RSA provider
            var popConfig = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl));
            popConfig.PopCryptoProvider = new RSACertificatePopCryptoProvider(GetCertificate());
            popConfig.HttpMethod = HttpMethod.Get;

            var result = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .WithProofOfPossession(popConfig)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            await VerifyPoPTokenAsync(
                PublicCloudConfidentialClientID,
                popConfig, result).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task PopTestWithECDAsync()
        {
            var confidentialClientAuthority = PublicCloudTestAuthority;

            var confidentialApp = ConfidentialClientApplicationBuilder
                .Create(PublicCloudConfidentialClientID)
                .WithExperimentalFeatures()
                .WithAuthority(new Uri(confidentialClientAuthority), true)
                .WithClientSecret(s_publicCloudCcaSecret)
                .Build();

            //ECD Provider
            var popConfig = new PoPAuthenticationConfiguration(new Uri(ProtectedUrl));
            popConfig.PopCryptoProvider = new ECDCertificatePopCryptoProvider();
            popConfig.HttpMethod = HttpMethod.Post;

            var result = await confidentialApp.AcquireTokenForClient(s_keyvaultScope)
                .WithProofOfPossession(popConfig)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual("pop", result.TokenType);
            await VerifyPoPTokenAsync(
                PublicCloudConfidentialClientID,
                popConfig, result).ConfigureAwait(false);
        }

        private static X509Certificate2 GetCertificate()
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByThumbprint(TestConstants.AutomationTestThumbprint);
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

        /// <summary>
        /// This calls a special endpoint that validates any POP token against a configurable HTTP request.
        /// The HTTP request is configured through headers.
        /// </summary>
        private async Task VerifyPoPTokenAsync(string clientId, PoPAuthenticationConfiguration popConfig, AuthenticationResult result)
        {
            var httpClient = new HttpClient();
            HttpResponseMessage response;
            var request = new HttpRequestMessage(HttpMethod.Post, PoPValidatorEndpoint);

            var authHeader = new AuthenticationHeaderValue(result.TokenType, result.AccessToken);

            request.Headers.Add("Secret", _popValidationEndpointSecret);
            request.Headers.Add("Authority", "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/");
            request.Headers.Add("ClientId", clientId);
            request.Headers.Authorization = authHeader;

            // the URI the POP token is bound to
            request.Headers.Add("ShrUri", ProtectedUrl);

            // the method the POP token in bound to
            request.Headers.Add("ShrMethod", popConfig.HttpMethod.ToString());

            response = await httpClient.SendAsync(request).ConfigureAwait(false);

            Assert.IsTrue(response.IsSuccessStatusCode);
        }


        private static Dictionary<string, string> GetTestSliceParams()
        {
            return new Dictionary<string, string>()
            {
                { "dc", "prod-wst-test1" },
            };
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
    }

    public class RSACertificatePopCryptoProvider : IPoPCryptoProvider
    {
        private readonly X509Certificate2 _cert;

       
        public RSACertificatePopCryptoProvider(X509Certificate2 cert)
        {
            _cert = cert ?? throw new ArgumentNullException(nameof(cert));

            RSA provider = _cert.GetRSAPublicKey();
            RSAParameters publicKeyParams = provider.ExportParameters(false);
            CannonicalPublicKeyJwk = ComputeCannonicalJwk(publicKeyParams);
        }

        public byte[] Sign(byte[] payload)
        {
            using (RSA key = _cert.GetRSAPrivateKey())
            {
                return key.SignData(
                    payload,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
            }
        }

        public string CannonicalPublicKeyJwk { get; }

        public string CryptographicAlgorithm { get => "RS256"; }


        /// <summary>
        /// Creates the cannonical representation of the JWK.  See https://tools.ietf.org/html/rfc7638#section-3
        /// The number of parameters as well as the lexicographic order is important, as this string will be hashed to get a thumbprint
        /// </summary>
        private static string ComputeCannonicalJwk(RSAParameters rsaPublicKey)
        {
            return $@"{{""{JsonWebKeyParameterNames.E}"":""{Base64UrlHelpers.Encode(rsaPublicKey.Exponent)}"",""{JsonWebKeyParameterNames.Kty}"":""{JsonWebAlgorithmsKeyTypes.RSA}"",""{JsonWebKeyParameterNames.N}"":""{Base64UrlHelpers.Encode(rsaPublicKey.Modulus)}""}}";
        }
    }
}
