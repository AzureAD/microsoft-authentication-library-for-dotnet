// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    // Tests in this class will run on .NET Core and .NET FWK
    [TestClass]
    public class ClientCredentialsTests
    {
        private static readonly string[] s_scopes = { "User.Read" };
        private static readonly string[] s_keyvaultScope = { "https://vault.azure.net/.default" };

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
            ApplicationBase.ResetStateForTest();
        }

        // regression test based on SAL introducing a new SKU value and making ESTS not issue the refresh_in value
        // This needs to run on .NET and .NET FWK to protect against MSAL SKU value changes
        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task RefreshOnIsEnabled(bool useRegional)
        {
            // if this test runs on local devbox, disable it
            if (useRegional && Environment.GetEnvironmentVariable("TF_BUILD") == null)
            {
                Assert.Inconclusive("Can't run regional on local devbox.");
            }

            var cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var builder = ConfidentialClientApplicationBuilder.Create(LabAuthenticationHelper.LabAccessConfidentialClientId)
                .WithCertificate(cert, sendX5C: true)
                .WithAuthority(LabAuthenticationHelper.LabClientInstance, LabAuthenticationHelper.LabClientTenantId);

            // auto-detect should work on Azure DevOps build
            if (useRegional)
                builder = builder.WithAzureRegion();

            var cca = builder.Build();

            var result = await cca.AcquireTokenForClient([LabAuthenticationHelper.LabScope]).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(result.AuthenticationResultMetadata.RefreshOn.HasValue, "refresh_in was not issued - did the MSAL SKU value change?");

            if (useRegional)
                Assert.AreEqual(
                    Client.Region.RegionOutcome.AutodetectSuccess,
                    result.AuthenticationResultMetadata.RegionDetails.RegionOutcome);
        }

        [DataTestMethod]
        [DataRow(Cloud.Public, TargetFrameworks.NetFx | TargetFrameworks.NetCore)]
#if !IGNORE_FEDERATED
        [DataRow(Cloud.Adfs, TargetFrameworks.NetFx | TargetFrameworks.NetCore)]
#endif
        [DataRow(Cloud.Public, TargetFrameworks.NetCore, true)]
        //[DataRow(Cloud.Arlington)] - cert not setup
        public async Task WithCertificate_TestAsync(Cloud cloud, TargetFrameworks runOn, bool useAppIdUri = false)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.Cert, useAppIdUri).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Cloud.Public, TargetFrameworks.NetCore)]
#if !IGNORE_FEDERATED
        [DataRow(Cloud.Adfs, TargetFrameworks.NetFx)]
#endif
        [DataRow(Cloud.Arlington, TargetFrameworks.NetCore)]
        public async Task WithSecret_TestAsync(Cloud cloud, TargetFrameworks runOn)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.Secret).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Cloud.Public, TargetFrameworks.NetCore)]
#if !IGNORE_FEDERATED
        [DataRow(Cloud.Adfs, TargetFrameworks.NetCore)]
#endif
        // [DataRow(Cloud.Arlington)] - cert not setup
        public async Task WithClientAssertion_Manual_TestAsync(Cloud cloud, TargetFrameworks runOn)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.ClientAssertion_Manual).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Cloud.Public, TargetFrameworks.NetFx)]
#if !IGNORE_FEDERATED
        [DataRow(Cloud.Adfs, TargetFrameworks.NetFx)]
#endif
        // [DataRow(Cloud.Arlington)] - cert not setup
        public async Task WithClientAssertion_Wilson_TestAsync(Cloud cloud, TargetFrameworks runOn)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.ClientAssertion_Wilson).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Cloud.Public, TargetFrameworks.NetCore)]
        // [DataRow(Cloud.Arlington)] - cert not setup
        public async Task WithClientClaims_ExtraClaims_TestAsync(Cloud cloud, TargetFrameworks runOn)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.ClientClaims_ExtraClaims).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Cloud.Public, TargetFrameworks.NetFx)]
#if !IGNORE_FEDERATED
        [DataRow(Cloud.Adfs, TargetFrameworks.NetCore)]
#endif
        // [DataRow(Cloud.Arlington)] - cert not setup
        public async Task WithClientClaims_OverrideClaims_TestAsync(Cloud cloud, TargetFrameworks runOn)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.ClientClaims_MergeClaims).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Cloud.Public, TargetFrameworks.NetCore)]
        // [DataRow(Cloud.Arlington)] - cert not setup
        public async Task WithClientClaims_SendX5C_ExtraClaims_TestAsync(Cloud cloud, TargetFrameworks runOn)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.ClientClaims_ExtraClaims, sendX5C: true).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Cloud.Public, TargetFrameworks.NetFx)]
#if !IGNORE_FEDERATED
        [DataRow(Cloud.Adfs, TargetFrameworks.NetCore)]
#endif
        // [DataRow(Cloud.Arlington)] - cert not setup
        public async Task WithClientClaims_SendX5C_OverrideClaims_TestAsync(Cloud cloud, TargetFrameworks runOn)
        {
            runOn.AssertFramework();
            await RunClientCredsAsync(cloud, CredentialType.ClientClaims_MergeClaims, sendX5C: true).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Cloud.Public, TargetFrameworks.NetCore)]
        public async Task WithOnBeforeTokenRequest_TestAsync(Cloud cloud, TargetFrameworks runOn)
        {
            runOn.AssertFramework();

            var appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            var cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            string[] appScopes = new[] { "https://vault.azure.net/.default" };

            AuthenticationResult authResult;

            IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder
                .Create(appConfig.AppId)
                .WithAuthority(appConfig.Authority, true)
                .WithTestLogging()
                .Build();

            authResult = await confidentialApp
                .AcquireTokenForClient(appScopes)
                .OnBeforeTokenRequest((data) =>
                {
                    ModifyRequest(data, cert); // Adding a certificate via handler instead of using WithCertificate
                    return Task.CompletedTask;
                })
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);

            // Call again to ensure token cache is hit
            authResult = await confidentialApp
               .AcquireTokenForClient(appScopes)
                .OnBeforeTokenRequest((data) =>
                {
                    throw new InvalidOperationException("Should not be invoking this callback when the token is fetched from the cache");
                })
               .ExecuteAsync()
               .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, authResult.AuthenticationResultMetadata.TokenSource);
        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task ByRefreshTokenTestAsync()
        {
            // Arrange
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppPCAClient).ConfigureAwait(false);

            var msalPublicClient = PublicClientApplicationBuilder
                .Create(app.AppId)
                .WithTestLogging()
                .WithAuthority(app.Authority, "organizations")
                .BuildConcrete();

#pragma warning disable CS0618 // Type or member is obsolete
            AuthenticationResult authResult = await msalPublicClient
                .AcquireTokenByUsernamePassword(s_scopes, user.Upn, user.GetOrFetchPassword())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
#pragma warning restore CS0618

var confidentialApp = ConfidentialClientApplicationBuilder
    .Create(app.AppId)
    .WithAuthority(app.Authority, user.TenantId)
    .WithTestLogging()
    .BuildConcrete();

            var rt = msalPublicClient.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().FirstOrDefault();

            // Act
            authResult = await (confidentialApp as IByRefreshToken).AcquireTokenByRefreshToken(s_scopes, rt.Secret).ExecuteAsync().ConfigureAwait(false);

            //Ensure we can get account from application
            var account = await confidentialApp.GetAccountAsync(authResult.Account.HomeAccountId.Identifier).ConfigureAwait(false);

            //Ensure we can get account from application using GetAccountsAsync
            var accounts = await confidentialApp.GetAccountsAsync().ConfigureAwait(false);

            var account2 = accounts.FirstOrDefault();

            //Validate that the refreshed token can be used
            authResult = await confidentialApp.AcquireTokenSilent(s_scopes, account).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(authResult);
            Assert.AreEqual(user.Upn, authResult.Account.Username);
            Assert.AreEqual(user.ObjectId.ToString(), authResult.Account.HomeAccountId.ObjectId);
            Assert.AreEqual(user.TenantId, authResult.Account.HomeAccountId.TenantId);
            Assert.AreEqual(user.Upn, account2.Username);
            Assert.AreEqual(user.ObjectId.ToString(), account2.HomeAccountId.ObjectId);
            Assert.AreEqual(user.TenantId, account2.HomeAccountId.TenantId);
        }

        private static void ModifyRequest(OnBeforeTokenRequestData data, X509Certificate2 certificate)
        {
            string clientId = data.BodyParameters["client_id"];
            string tokenEndpoint = data.RequestUri.AbsoluteUri;

            string assertion = GetSignedClientAssertionManual(
                issuer: clientId,
                audience: tokenEndpoint,
                certificate: certificate,
                useSha2AndPss: true);

            data.BodyParameters.Add("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer");
            data.BodyParameters.Add("client_assertion", assertion);
        }

        private static async Task<X509Certificate2> GetAdfsCertificateAsync()
        {
            const string AdfsCertName = "IDLABS-APP-Confidential-Client-Cert-OnPrem";
            KeyVaultSecretsProvider kv = new KeyVaultSecretsProvider();
            return await kv.GetCertificateWithPrivateMaterialAsync(AdfsCertName).ConfigureAwait(false);
        }

        private async Task RunClientCredsAsync(Cloud cloud, CredentialType credentialType, bool UseAppIdUri = false, bool sendX5C = false)
        {
            Trace.WriteLine($"Running test with settings for cloud {cloud}, credential type {credentialType}");
            
            // Get configuration based on cloud type
            AppConfig appConfig;
            X509Certificate2 cert = null;
            string secret = null;
            string tenantId;
            string authority;
            string[] scopes;

            if (cloud == Cloud.Adfs)
            {
                appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppPCAClient).ConfigureAwait(false);
                secret = LabResponseHelper.FetchSecretString(KeyVaultSecrets.DefaultAppSecret, LabResponseHelper.KeyVaultSecretsProviderMsal);
                cert = await GetAdfsCertificateAsync().ConfigureAwait(false);
                tenantId = "";  // ADFS doesn't use tenant ID
                authority = "https://fs.id4slab1.com/adfs";
                scopes = new[] { "openid", "profile" };
            }
            else if (cloud == Cloud.Arlington)
            {
                appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.MsalAppArlingtonCCA).ConfigureAwait(false);
                secret = LabResponseHelper.FetchSecretString(appConfig.SecretName, LabResponseHelper.KeyVaultSecretsProviderMsid);
                cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
                tenantId = appConfig.TenantId;
                authority = appConfig.Authority;
                scopes = new[] { "https://graph.microsoft.com/.default" };
            }
            else // Cloud.Public
            {
                appConfig = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
                secret = LabResponseHelper.FetchSecretString(appConfig.SecretName, LabResponseHelper.KeyVaultSecretsProviderMsal);
                cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
                tenantId = appConfig.TenantId;
                authority = appConfig.Authority;
                scopes = new[] { "https://vault.azure.net/.default" };
            }

            AuthenticationResult authResult;

            IConfidentialClientApplication confidentialApp = CreateApp(
                credentialType, 
                appConfig.AppId, 
                authority, 
                cert, 
                secret, 
                sendX5C, 
                cloud != Cloud.Adfs);
            
            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();
            Guid correlationId = Guid.NewGuid();
            authResult = await confidentialApp
                .AcquireTokenForClient(scopes)
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
            CollectionAssert.AreEquivalent(scopes.ToArray(), appCacheRecorder.LastBeforeAccessNotificationArgs.RequestScopes.ToArray());
            CollectionAssert.AreEquivalent(scopes.ToArray(), appCacheRecorder.LastAfterAccessNotificationArgs.RequestScopes.ToArray());
            Assert.AreEqual(tenantId, appCacheRecorder.LastBeforeAccessNotificationArgs.RequestTenantId ?? "");
            Assert.AreEqual(tenantId, appCacheRecorder.LastAfterAccessNotificationArgs.RequestTenantId ?? "");
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationTotalInMs > 0);
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationInHttpInMs > 0);
            Assert.AreEqual(
              GetExpectedCacheKey(appConfig.AppId, tenantId),
              appCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);

            Assert.IsNull(authResult.BindingCertificate,
                          "BindingCertificate should be null for bearer tokens.");

            // Call again to ensure token cache is hit
            authResult = await confidentialApp
               .AcquireTokenForClient(scopes)
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
               GetExpectedCacheKey(appConfig.AppId, tenantId),
               appCacheRecorder.LastAfterAccessNotificationArgs.SuggestedCacheKey);

            Assert.IsNull(authResult.BindingCertificate,
                          "BindingCertificate should be null for bearer tokens.");
        }

        private static IConfidentialClientApplication CreateApp(
            CredentialType credentialType,
            string clientId,
            string authority,
            X509Certificate2 cert,
            string secret,
            bool sendX5C,
            bool useSha2AndPssForAssertion)
        {
            var builder = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(authority, true)
                .WithTestLogging();

            switch (credentialType)
            {
                case CredentialType.Cert:
                    builder.WithCertificate(cert);
                    break;
                case CredentialType.Secret:
                    builder.WithClientSecret(secret);
                    break;
                case CredentialType.ClientAssertion_Manual:

                    var aud = authority.Contains("/adfs/") ?
                        authority + "/oauth2/token" :
                        authority + "/oauth2/v2.0/token";

                    builder.WithClientAssertion(() => GetSignedClientAssertionManual(
                      clientId,
                      aud, // for AAD use v2.0, but not for ADFS
                      cert,
                      useSha2AndPssForAssertion));
                    break;

                case CredentialType.ClientAssertion_Wilson:
                    var aud2 = authority.Contains("/adfs/") ?
                       authority + "/oauth2/token" :
                       authority + "/oauth2/v2.0/token";

                    builder.WithClientAssertion(
                        () => GetSignedClientAssertionUsingWilson(
                            clientId,
                            aud2,
                            cert));
                    break;

                case CredentialType.ClientClaims_ExtraClaims:
                    builder.WithClientClaims(cert, GetClaims(true), mergeWithDefaultClaims: false, sendX5C: sendX5C);
                    break;
                case CredentialType.ClientClaims_MergeClaims:
                    builder.WithClientClaims(cert, GetClaims(false), mergeWithDefaultClaims: true, sendX5C: sendX5C);
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
                { "iss", TestConstants.PublicCloudConfidentialClientID.ToString(CultureInfo.InvariantCulture) },
                { "jti", Guid.NewGuid().ToString() },
                { "nbf", validFrom.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) },
                { "sub", TestConstants.PublicCloudConfidentialClientID.ToString(CultureInfo.InvariantCulture) },
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
        private static string GetSignedClientAssertionManual(
            string issuer,
            string audience,
            X509Certificate2 certificate,
            bool useSha2AndPss)
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

            // cng is Windows specific, so we use RSA instead
            RSA rsa = certificate.GetRSAPrivateKey();  // Works cross-platform

            Dictionary<string, string> header;
            if (useSha2AndPss)
            {
                header = new Dictionary<string, string>()
                {
                  { "alg", "PS256"},
                  { "typ", "JWT"},
                  { "x5t#S256", Base64UrlHelpers.Encode(certificate.GetCertHash(HashAlgorithmName.SHA256))},
                };
            }
            else
            {
                header = new Dictionary<string, string>()
                {
                  { "alg", "RS256"},
                  { "typ", "JWT"},
                  { "x5t", Base64UrlHelpers.Encode(certificate.GetCertHash())},
                };
            }

            var headerBytes = JsonSerializer.SerializeToUtf8Bytes(header);
            var claimsBytes = JsonSerializer.SerializeToUtf8Bytes(claims);
            string token = Base64UrlHelpers.Encode(headerBytes) + "." + Base64UrlHelpers.Encode(claimsBytes);
            if (rsa is null)
            {
                throw new InvalidOperationException("RSA private key expected");
            }
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Token expected");
            }
            //codeql [SM03799] Backwards Compatibility: Requires accepting PKCS1 for supporting ADFS 
            byte[] signatureBytes = rsa.SignData(
                    Encoding.UTF8.GetBytes(token),
                    HashAlgorithmName.SHA256,
                    useSha2AndPss ? RSASignaturePadding.Pss : RSASignaturePadding.Pkcs1);
            string signature = Base64UrlHelpers.Encode(signatureBytes);

            return string.Concat(token, ".", signature);
        }
    }
}
