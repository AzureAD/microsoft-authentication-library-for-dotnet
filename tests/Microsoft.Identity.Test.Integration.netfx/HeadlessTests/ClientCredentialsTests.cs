// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{

    [TestClass]
    public class ClientCredentialsTests
    {
        private static readonly string[] s_scopes = { "User.Read" };
        private static readonly string[] s_keyvaultScope = { "https://vault.azure.net/.default" };
        private const string PublicCloudConfidentialClientID = "16dab2ba-145d-4b1b-8569-bf4b9aed4dc8";
        private const string PublicCloudTestAuthority = "https://login.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47";

        private enum CredentialType
        {
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
        [DataRow(Cloud.PPE, new object[] { RunOn.NetFx, true })]
        [DataRow(Cloud.Public, new object[] { RunOn.NetFx | RunOn.NetCore, true })]
        //[DataRow(Cloud.Arlington)] - cert not setup
        public async Task WithCertificate_TestAsync(Cloud cloud, RunOn runOn, bool useAppIdUri = false)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.Cert, useAppIdUri).ConfigureAwait(false);
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

        [TestMethod]
        [DataRow(Cloud.Public, RunOn.NetCore)]
        // [DataRow(Cloud.Arlington)] - cert not setup
        public async Task WithClientClaims_SendX5C_ExtraClaims_TestAsync(Cloud cloud, RunOn runOn)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.ClientClaims_ExtraClaims, sendX5C: true).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(Cloud.Public, RunOn.NetFx)]
        [DataRow(Cloud.Adfs, RunOn.NetCore)]
        // [DataRow(Cloud.Arlington)] - cert not setup
        public async Task WithClientClaims_SendX5C_OverrideClaims_TestAsync(Cloud cloud, RunOn runOn)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.ClientClaims_MergeClaims, sendX5C: true).ConfigureAwait(false);
        }

        private async Task RunClientCredsAsync(Cloud cloud, CredentialType credentialType, bool UseAppIdUri = false, bool sendX5C = false)
        {
            Trace.WriteLine($"Running test with settings for cloud {cloud}, credential type {credentialType}");
            IConfidentialAppSettings settings = ConfidentialAppSettings.GetSettings(cloud);

            settings.UseAppIdUri = UseAppIdUri;

            AuthenticationResult authResult;

            IConfidentialClientApplication confidentialApp = CreateApp(credentialType, settings, sendX5C);
            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();
            Guid correlationId = Guid.NewGuid();
            authResult = await confidentialApp
                .AcquireTokenForClient(settings.AppScopes)
                .WithCorrelationId(correlationId)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            appCacheRecorder.AssertAccessCounts(1, 1);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            Assert.AreEqual(correlationId, appCacheRecorder.LastAfterAccessNotificationArgs.CorrelationId);
            Assert.AreEqual(correlationId, appCacheRecorder.LastBeforeAccessNotificationArgs.CorrelationId);
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
            Assert.AreNotEqual(correlationId, appCacheRecorder.LastAfterAccessNotificationArgs.CorrelationId);
            Assert.AreNotEqual(correlationId, appCacheRecorder.LastBeforeAccessNotificationArgs.CorrelationId);
            Assert.AreEqual(
               GetExpectedCacheKey(settings.ClientId, settings.TenantId),
               appCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);
        }

        private static IConfidentialClientApplication CreateApp(CredentialType credentialType, IConfidentialAppSettings settings, bool sendX5C)
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
                    builder.WithClientClaims(settings.GetCertificate(), GetClaims(true), mergeWithDefaultClaims: false, sendX5C: sendX5C);
                    break;
                case CredentialType.ClientClaims_MergeClaims:
                    builder.WithClientClaims(settings.GetCertificate(), GetClaims(false), mergeWithDefaultClaims: true, sendX5C: sendX5C);
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

        private static IDictionary<string, string> GetClaims(bool useDefaultClaims = true)
        {
            const uint JwtToAadLifetimeInSeconds = 60 * 10; // Ten minutes

            if (useDefaultClaims)
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset validFrom = now; // AAD will take clock skew into consideration
                DateTimeOffset validUntil = now.AddSeconds(JwtToAadLifetimeInSeconds);

                return new Dictionary<string, string>()
                {
                { "aud", TestConstants.ClientCredentialAudience },
                { "exp", validUntil.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) },
                { "iss", PublicCloudConfidentialClientID.ToString(CultureInfo.InvariantCulture) },
                { "jti", Guid.NewGuid().ToString() },
                { "nbf", validFrom.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) },
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

        /// <summary>
        /// Creates a signed assertion in JWT format which can be used in the client_credentials flow. 
        /// </summary>
        /// <param name="issuer">the client ID</param>
        /// <param name="audience">the token endpoint, i.e. ${authority}/oauth2/v2.0/token for AAD or ${authority}/oauth2/token for ADFS</param>
        /// <param name="certificate"></param>
        /// <returns></returns>
        internal static string GetSignedClientAssertionManual(
            string issuer, 
            string audience, 
            X509Certificate2 certificate)
        {
            const uint JwtToAadLifetimeInSeconds = 60 * 10; // Ten minutes

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset validFrom = now; // AAD will take clock skew into consideration
            DateTimeOffset validUntil = now.AddSeconds(JwtToAadLifetimeInSeconds);

            // as per https://datatracker.ietf.org/doc/html/rfc7523#section-3
            // more claims can be added here
            var claims = new Dictionary<string, object>()
            {
                { "aud", audience },
                { "exp", validUntil.ToUnixTimeSeconds() },
                { "iss", issuer },
                { "jti", Guid.NewGuid().ToString() },
                { "nbf", validFrom.ToUnixTimeSeconds() },
                { "sub", issuer }
            };

            RSACng rsa = certificate.GetRSAPrivateKey() as RSACng;

            //alg represents the desired signing algorithm, which is SHA-256 in this case
            //kid represents the certificate thumbprint
            var header = new Dictionary<string, string>()
            {
              { "alg", "RS256"},
              { "typ", "JWT"},
              { "x5t", Base64UrlHelpers.Encode(certificate.GetCertHash())},
            };

            var headerBytes = JsonSerializer.SerializeToUtf8Bytes(header);
            var claimsBytes = JsonSerializer.SerializeToUtf8Bytes(claims);
            string token = Base64UrlHelpers.Encode(headerBytes) + "." + Base64UrlHelpers.Encode(claimsBytes);

            string signature = Base64UrlHelpers.Encode(
                rsa.SignData(
                    Encoding.UTF8.GetBytes(token),
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1));
            return string.Concat(token, ".", signature);
        }
    }
}
