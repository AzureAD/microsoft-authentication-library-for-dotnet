// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{

    [TestClass]
    public class ClientCredentialsTests
    {
        private static readonly string[] s_scopes = { "User.Read" };
        private static readonly string[] s_keyvaultScope = { "https://vault.azure.net/.default" };        
        private const string PublicCloudConfidentialClientID = "16dab2ba-145d-4b1b-8569-bf4b9aed4dc8";
        private const string PublicCloudTestAuthority = "https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47";

        private enum CredentialType { 
            Cert, 
            Secret, 
            ClientAssertion_Wilson, 
            ClientAssertion_Manual,
            ClientClaims_MergeClaims,
            ClientClaims_ExtraClaims
        };

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();          
        }
    
        [TestMethod]
        [DataRow(Cloud.Public, RunOn.NetFx | RunOn.NetCore)]
        [DataRow(Cloud.Adfs, RunOn.NetCore)]
        [DataRow(Cloud.PPE, RunOn.NetFx)]
         //[DataRow(Cloud.Arlington)] - cert not setup
        public async Task WithCertificate_TestAsync(Cloud cloud, RunOn runOn)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.Cert).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(Cloud.Public, RunOn.NetFx | RunOn.NetCore)]
        [DataRow(Cloud.Adfs, RunOn.NetFx)]        
        [DataRow(Cloud.Arlington, RunOn.NetCore)]
        //[DataRow(Cloud.PPE)] - secret not setup
        public async Task WithSecret_TestAsync(Cloud cloud, RunOn runOn)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.Secret).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(Cloud.Public, RunOn.NetFx | RunOn.NetCore)]
        [DataRow(Cloud.Adfs, RunOn.NetFx)]
        [DataRow(Cloud.PPE, RunOn.NetCore)]
        // [DataRow(Cloud.Arlington)] - cert not setup
        public async Task WithClientAssertion_Manual_TestAsync(Cloud cloud, RunOn runOn)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.ClientAssertion_Manual).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(Cloud.Public, RunOn.NetFx | RunOn.NetCore)]
        [DataRow(Cloud.Adfs, RunOn.NetFx)]
        [DataRow(Cloud.PPE, RunOn.NetCore)]
        // [DataRow(Cloud.Arlington)] - cert not setup
        public async Task WithClientAssertion_Wilson_TestAsync(Cloud cloud, RunOn runOn)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.ClientAssertion_Wilson).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(Cloud.Public, RunOn.NetCore)]        
        // [DataRow(Cloud.Arlington)] - cert not setup
        public async Task WithClientClaims_ExtraClaims_TestAsync(Cloud cloud, RunOn runOn)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.ClientClaims_ExtraClaims).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(Cloud.Public, RunOn.NetFx)]
        [DataRow(Cloud.Adfs, RunOn.NetCore)]
        // [DataRow(Cloud.Arlington)] - cert not setup
        public async Task WithClientClaims_OverrideClaims_TestAsync(Cloud cloud, RunOn runOn)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.ClientClaims_MergeClaims).ConfigureAwait(false);
        }

        private async Task RunClientCredsAsync(Cloud cloud, CredentialType credentialType)
        {
            Trace.WriteLine($"Running test with settings for cloud {cloud}, credential type {credentialType}");
            IConfidentialAppSettings settings = ConfidentialAppSettings.GetSettings(cloud);

            AuthenticationResult authResult;

            IConfidentialClientApplication confidentialApp = CreateApp(credentialType, settings);
            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();

            authResult = await confidentialApp
                .AcquireTokenForClient(settings.AppScopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            appCacheRecorder.AssertAccessCounts(1, 1);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationTotalInMs > 0);
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationInHttpInMs > 0);
            Assert.AreEqual(
              GetExpectedCacheKey(settings.ClientId, settings.TenantId),
              appCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);

            // Call again to ensure token cache is hit
            authResult = await confidentialApp
               .AcquireTokenForClient(settings.AppScopes)
               .ExecuteAsync()
               .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
                Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationInHttpInMs == 0);
            
            appCacheRecorder.AssertAccessCounts(2, 1);
            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(
               GetExpectedCacheKey(settings.ClientId, settings.TenantId),
               appCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);        
        }

        private static IConfidentialClientApplication CreateApp(CredentialType credentialType, IConfidentialAppSettings settings)
        {
            var builder = ConfidentialClientApplicationBuilder
                .Create(settings.ClientId)
                .WithAuthority(settings.Authority, true)
                .WithTestLogging();

            switch (credentialType)
            {
                case CredentialType.Cert:
                    builder.WithCertificate(settings.GetCertificate());
                    break;
                case CredentialType.Secret:
                    builder.WithClientSecret(settings.GetSecret());
                    break;
                case CredentialType.ClientAssertion_Manual:

                    var aud = settings.Cloud == Cloud.Adfs ?
                        settings.Authority + "/oauth2/token" :
                        settings.Authority + "/oauth2/v2.0/token";

                    string signedAssertionManual = GetSignedClientAssertionManual(
                      settings.ClientId,
                      aud, // for AAD use v2.0, but not for ADFS
                      settings.GetCertificate());

                    builder.WithClientAssertion(signedAssertionManual);
                    break;

                case CredentialType.ClientAssertion_Wilson:
                    var aud2 = settings.Cloud == Cloud.Adfs ?
                       settings.Authority + "/oauth2/token" :
                       settings.Authority + "/oauth2/v2.0/token";

                    string clientAssertion = GetSignedClientAssertionUsingWilson(
                        settings.ClientId,
                        aud2,
                        settings.GetCertificate());

                    builder.WithClientAssertion(clientAssertion);
                    break;

                case CredentialType.ClientClaims_ExtraClaims:
                    builder.WithClientClaims(settings.GetCertificate(), GetClaims(true), mergeWithDefaultClaims: false);
                    break;
                case CredentialType.ClientClaims_MergeClaims:
                    builder.WithClientClaims(settings.GetCertificate(), GetClaims(false), mergeWithDefaultClaims: true);
                    break;
                default:
                    throw new NotImplementedException();
            }

            var confidentialApp = builder.Build();
            return confidentialApp;
        }

        private string GetExpectedCacheKey(string clientId, string tenantId)
        {
            return $"{clientId}_{tenantId ?? ""}_AppTokenCache";
        }       

        internal static long ConvertToTimeT(DateTime time)
        {
            var startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = time - startTime;
            return (long)diff.TotalSeconds;
        }

        private static IDictionary<string, string> GetClaims(bool useDefaultClaims = true)
        {
            if (useDefaultClaims)
            {
                DateTime validFrom = DateTime.UtcNow;
                var nbf = ConvertToTimeT(validFrom);
                var exp = ConvertToTimeT(validFrom + TimeSpan.FromSeconds(TestConstants.JwtToAadLifetimeInSeconds));

                return new Dictionary<string, string>()
                {
                { "aud", TestConstants.ClientCredentialAudience },
                { "exp", exp.ToString(CultureInfo.InvariantCulture) },
                { "iss", PublicCloudConfidentialClientID.ToString(CultureInfo.InvariantCulture) },
                { "jti", Guid.NewGuid().ToString() },
                { "nbf", nbf.ToString(CultureInfo.InvariantCulture) },
                { "sub", PublicCloudConfidentialClientID.ToString(CultureInfo.InvariantCulture) },
                { "ip", "192.168.2.1" }
                };
            }
            else
            {
                return new Dictionary<string, string>()
                {
                    { "ip", "192.168.2.1" }
                };
            }
        }

        private static string GetSignedClientAssertionUsingMsalInternal(string clientId, IDictionary<string, string> claims)
        {
#if NET_CORE
            var manager = new Client.Platforms.netcore.NetCoreCryptographyManager();
#else
            var manager = new Client.Platforms.net45.NetDesktopCryptographyManager();
#endif
            var jwtToken = new Client.Internal.JsonWebToken(manager, clientId, TestConstants.ClientCredentialAudience, claims);
            var clientCredential = ClientCredentialWrapper.CreateWithCertificate(GetCertificate(), claims, false);
            return jwtToken.Sign(clientCredential, true);
        }

        private static X509Certificate2 GetCertificate(bool useRSACert = false)
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByThumbprint(useRSACert ?
                TestConstants.RSATestCertThumbprint :
                TestConstants.AutomationTestThumbprint);
            if (cert == null)
            {
                throw new InvalidOperationException(
                    "Test setup error - cannot find a certificate in the My store for KeyVault. This is available for Microsoft employees only.");
            }

            return cert;
        }

        private static string GetSignedClientAssertionUsingWilson(
            string issuer,
            string aud,
            X509Certificate2 cert)
        {
            // no need to add exp, nbf as JsonWebTokenHandler will add them by default.
            var claims = new Dictionary<string, object>()
            {
                { "aud", aud },
                { "iss", issuer },
                { "jti", Guid.NewGuid().ToString() },
                { "sub", issuer }
            };

            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = claims,
                SigningCredentials = new X509SigningCredentials(cert)
            };

            var handler = new JsonWebTokenHandler();
            var signedClientAssertion = handler.CreateToken(securityTokenDescriptor);

            return signedClientAssertion;
        }

       
        private static string GetSignedClientAssertionManual(
            string issuer, // client ID
            string audience, // ${authority}/oauth2/v2.0/token for AAD or ${authority}/oauth2/token for ADFS
            X509Certificate2 certificate)
        {
            const uint JwtToAadLifetimeInSeconds = 60 * 10; // Ten minutes
            DateTime validFrom = DateTime.UtcNow;
            var nbf = ConvertToTimeT(validFrom);
            var exp = ConvertToTimeT(validFrom + TimeSpan.FromSeconds(JwtToAadLifetimeInSeconds));

            var payload = new Dictionary<string, string>()
            {
                { "aud", audience },
                { "iss", issuer },
                { "jti", Guid.NewGuid().ToString() },
                { "sub", issuer },
                { "exp", exp.ToString(CultureInfo.InvariantCulture) },
                { "nbf", nbf.ToString(CultureInfo.InvariantCulture) },
                { "iat", nbf.ToString(CultureInfo.InvariantCulture) }
            };

            RSACng rsa = certificate.GetRSAPrivateKey() as RSACng;

            //alg represents the desired signing algorithm, which is SHA-256 in this case
            //kid represents the certificate thumbprint
            var header = new Dictionary<string, string>()
            {
              { "alg", "RS256"},
              { "kid",  certificate.Thumbprint},
              { "typ", "JWT"},
              { "x5t", Encode(certificate.GetCertHash())},
            };

            string token = Encode(
                Encoding.UTF8.GetBytes(JObject.FromObject(header).ToString())) +
                "." +
                Encode(Encoding.UTF8.GetBytes(JObject.FromObject(payload).ToString()));

            string signature = Encode(
                rsa.SignData(
                    Encoding.UTF8.GetBytes(token),
                    HashAlgorithmName.SHA256,
                    System.Security.Cryptography.RSASignaturePadding.Pkcs1));
            return string.Concat(token, ".", signature);
        }

        private static string Encode(byte[] arg)
        {
            char Base64PadCharacter = '=';
            char Base64Character62 = '+';
            char Base64Character63 = '/';
            char Base64UrlCharacter62 = '-';
            char Base64UrlCharacter63 = '_';


            string s = Convert.ToBase64String(arg);
            s = s.Split(Base64PadCharacter)[0]; // RemoveAccount any trailing padding
            s = s.Replace(Base64Character62, Base64UrlCharacter62); // 62nd char of encoding
            s = s.Replace(Base64Character63, Base64UrlCharacter63); // 63rd char of encoding

            return s;
        }

    }
}
