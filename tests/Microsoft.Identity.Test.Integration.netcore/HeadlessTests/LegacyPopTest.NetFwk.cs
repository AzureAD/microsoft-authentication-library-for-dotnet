// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    // These tests run on .NET FWK as well. Use the RunOn attribute to limit this.
    [TestClass]
    public class LegacyPopTests
    {
        public struct JwtClaimTypes
        {
            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string Actort = "actort";

            /// <summary>
            /// AzureSpecific
            /// </summary>
            public const string ActorToken = "actortoken";

            /// <summary>
            /// http://openid.net/specs/openid-connect-core-1_0.html#IDToken
            /// </summary>
            public const string Acr = "acr";

            /// <summary>
            /// https://tools.ietf.org/html/rfc7515#section-4.1.1
            /// </summary>
            public const string Alg = "alg";

            /// <summary>
            /// https://tools.ietf.org/html/rfc7515#section-4.1.1
            /// </summary>
            public const string Altsecid = "altsecid";

            /// <summary>
            /// http://openid.net/specs/openid-connect-core-1_0.html#IDToken
            /// </summary>
            public const string Amr = "amr";

            /// <summary>
            /// Azure specific
            /// </summary>
            public const string AppId = "appid";

            /// <summary>
            /// Azure specific
            /// </summary>
            public const string AppIdAcr = "appidacr";

            /// <summary>
            /// http://openid.net/specs/openid-connect-core-1_0.html#CodeIDToken
            /// </summary>
            public const string AtHash = "at_hash";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string Aud = "aud";

            /// <summary>
            /// http://openid.net/specs/openid-connect-core-1_0.html#IDToken
            /// </summary>
            public const string AuthTime = "auth_time";

            /// <summary>
            /// http://openid.net/specs/openid-connect-core-1_0.html#IDToken
            /// </summary>
            public const string Azp = "azp";

            /// <summary>
            /// Azure specific
            /// </summary>
            public const string AzpAcr = "azpacr";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string Birthdate = "birthdate";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string CHash = "c_hash";

            /// <summary>
            /// Azure Specific
            /// </summary>
            public const string Cid = "cid";

            /// <summary>
            /// Azure Specific
            /// </summary>
            public const string ClientAppid = "clientappid";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string Email = "email";

            /// <summary>
            /// when hueristically checking for an appToken
            /// these claims should never be found
            /// </summary>
            public static IList<string> ExcludedAppClaims = new List<string> { Scp, UniqueName };

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string Exp = "exp";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string FamilyName = "family_name";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string Gender = "gender";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string GivenName = "given_name";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string Iat = "iat";

            /// <summary>
            /// AAD claim that determines what type of token
            /// </summary>
            public const string Idtyp = "idtyp";

            /// <summary>
            /// Azure Specific
            /// </summary>
            public const string IsConsumer = "isconsumer";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string Iss = "iss";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string Jti = "jti";

            /// <summary>
            /// https://tools.ietf.org/html/rfc7515#section-4.1.4
            /// </summary>
            public const string Kid = "kid";

            /// <summary>
            /// Azure Specific
            /// </summary>
            public const string IpAddr = "ipaddr";

            /// <summary>
            /// Azure Specific
            /// </summary>
            public const string Name = "name";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string NameId = "nameid";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string Nonce = "nonce";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string Nbf = "nbf";

            /// <summary>
            /// Azure Specific
            /// </summary>
            public const string Oid = "oid";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string PopJwk = "pop_jwk";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string Prn = "prn";

            /// <summary>
            /// Azure specific
            /// </summary>
            public const string Puid = "puid";

            /// <summary>
            /// Azure specific
            /// </summary>
            public const string Roles = "roles";

            /// <summary>
            /// Azure specific
            /// </summary>
            public const string Scp = "scp";

            /// <summary>
            /// http://openid.net/specs/openid-connect-frontchannel-1_0.html#OPLogout
            /// </summary>
            public const string Sid = "sid";

            /// <summary>
            /// Azure specific
            /// </summary>
            public const string Smtp = "smtp";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string Sub = "sub";

            /// <summary>
            /// Azure specific
            /// </summary>
            public const string Tid = "tid";

            /// <summary>
            /// https://tools.ietf.org/html/rfc7519#section-5.1
            /// </summary>
            public const string Typ = "typ";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string UniqueName = "unique_name";

            /// <summary>
            /// Azure Specific
            /// </summary>
            public const string Upn = "upn";

            /// <summary>
            /// Azure Specific
            /// </summary>
            public const string Ver = "ver";

            /// <summary>
            /// http://tools.ietf.org/html/rfc7519#section-4
            /// </summary>
            public const string Website = "website";

            /// <summary>
            /// https://tools.ietf.org/html/rfc7515#section-4.1.4
            /// </summary>
            public const string X5t = "x5t";

            /// <summary>
            /// Contains the X.509 public key certificate or certificate chain corresponding to the key used to digitally sign the JWS.
            /// https://tools.ietf.org/html/rfc7515#section-4.1.6
            /// </summary>
            public const string X5c = "x5c";

            /// <summary>
            /// An STI(Substrate Token Issuer) specific claim that contains the key used to validate the signature on the token. The key itself is signed and must be validated before use.
            /// https://aka.ms/s2s/epk
            /// </summary>
            public const string Epk = "epk";
        }

        [TestMethod]
        public async Task LegacyPoPAsync()
        {
            IConfidentialAppSettings settings = ConfidentialAppSettings.GetSettings(Cloud.Public);
            X509Certificate2 clientCredsCert = settings.GetCertificate();
            RsaSecurityKey popKey = CreateRsaSecurityKey();

            var cca = ConfidentialClientApplicationBuilder
                .Create(settings.ClientId)
                .WithAuthority(settings.Authority, true)
                .WithExperimentalFeatures(true)
                .WithTestLogging()
                .Build();

            var result = await cca.AcquireTokenForClient(settings.AppScopes)
                .WithProofOfPosessionKeyId(popKey.KeyId)
                .OnBeforeTokenRequest((data) =>
                    {
                        ModifyRequestWithLegacyPop(data, settings, clientCredsCert, popKey);
                        return Task.CompletedTask;
                    })
                .ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            MsalAccessTokenCacheItem at = (cca.AppTokenCache as ITokenCacheInternal).Accessor.GetAllAccessTokens().Single();
            Assert.AreEqual(at.KeyId, popKey.KeyId);

            result = await cca.AcquireTokenForClient(settings.AppScopes)
                .WithProofOfPosessionKeyId(popKey.KeyId)
                .OnBeforeTokenRequest((data) =>
                {
                    ModifyRequestWithLegacyPop(data, settings, clientCredsCert, popKey);
                    return Task.CompletedTask;
                }).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            at = (cca.AppTokenCache as ITokenCacheInternal).Accessor.GetAllAccessTokens().Single();
            Assert.AreEqual(at.KeyId, popKey.KeyId);

            RsaSecurityKey popKey2 = CreateRsaSecurityKey();

            result = await cca.AcquireTokenForClient(settings.AppScopes)
                .WithProofOfPosessionKeyId(popKey2.KeyId)
                .OnBeforeTokenRequest((data) =>
                {
                    ModifyRequestWithLegacyPop(data, settings, clientCredsCert, popKey2);
                    return Task.CompletedTask;
                }).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            var ats = (cca.AppTokenCache as ITokenCacheInternal).Accessor.GetAllAccessTokens();
            Assert.IsNotNull(ats.SingleOrDefault(a => a.KeyId == popKey2.KeyId));
            Assert.IsNotNull(ats.SingleOrDefault(a => a.KeyId == popKey.KeyId));
        }

        private static void ModifyRequestWithLegacyPop(OnBeforeTokenRequestData data, IConfidentialAppSettings settings, X509Certificate2 clientCredsCert, RsaSecurityKey popKey)
        {
            var clientCredsSigningCredentials = new SigningCredentials(new X509SecurityKey(clientCredsCert), SecurityAlgorithms.RsaSha256, SecurityAlgorithms.Sha256);
            string request = CreateMs10ATPOPAssertion(
                settings.ClientId,
                data.RequestUri.AbsoluteUri,
                clientCredsSigningCredentials,
                popKey,
                true);

            data.BodyParameters.Add("request", request);
        }

        private static RsaSecurityKey CreateRsaSecurityKey()
        {
#if NET472
            RSA rsa = RSA.Create(2048);
#else
            RSA rsa = new RSACryptoServiceProvider(2048);
#endif
            // the reason for creating the RsaSecurityKey from RSAParameters is so that a SignatureProvider created with this key
            // will own the RSA object and dispose it. If we pass a RSA object, the SignatureProvider does not own the object, the RSA object will not be disposed.
            RSAParameters rsaParameters = rsa.ExportParameters(true);
            RsaSecurityKey rsaSecuirtyKey = new RsaSecurityKey(rsaParameters) { KeyId = CreateRsaKeyId(rsaParameters) };
            rsa.Dispose();
            return rsaSecuirtyKey;
        }

        private static string CreateRsaKeyId(RSAParameters rsaParameters)
        {
            byte[] kidBytes = new byte[rsaParameters.Exponent.Length + rsaParameters.Modulus.Length];
            Array.Copy(rsaParameters.Exponent, 0, kidBytes, 0, rsaParameters.Exponent.Length);
            Array.Copy(rsaParameters.Modulus, 0, kidBytes, rsaParameters.Exponent.Length, rsaParameters.Modulus.Length);
            using (var sha2 = SHA256.Create())
                return Base64UrlEncoder.Encode(sha2.ComputeHash(kidBytes));
        }

        private static string CreateMs10ATPOPAssertion(
           string clientId,
           string audience,
           SigningCredentials signingCredentials,
           RsaSecurityKey popKey,
           bool includeX5cClaim)
        {
            var header = new JObject
            {
                { JwtClaimTypes.Typ, "JWT" },
                { JwtClaimTypes.Alg, signingCredentials.Algorithm },
                { JwtClaimTypes.Kid, signingCredentials.Key.KeyId }
            };

            if (signingCredentials.Key is X509SecurityKey x509SecurityKey)
            {
                header[JwtClaimTypes.Kid] = Base64UrlEncoder.Encode(x509SecurityKey.Certificate.GetCertHash());

                if (includeX5cClaim)
                    header[JwtClaimTypes.X5c] = JArray.FromObject(new List<string>() { Convert.ToBase64String(x509SecurityKey.Certificate.GetRawCertData()) });
            }

            long nbf = EpochTime.GetIntDate(DateTime.UtcNow);
            var payload = new JObject
            {
                {JwtClaimTypes.Iss, clientId},
                {JwtClaimTypes.Aud, audience},
                {JwtClaimTypes.Sub, clientId},
                {JwtClaimTypes.Nbf, nbf},
                {JwtClaimTypes.Iat, nbf},
                {JwtClaimTypes.Exp, nbf + 600 },
                {"pop_jwk", CreateJwkClaim(popKey, signingCredentials.Algorithm)}
            };

            return CreateJWS(payload.ToString(Formatting.None), header.ToString(Formatting.None), signingCredentials);
        }

        private static string CreateJWS(string payload, string header, SigningCredentials signingCredentials)
        {
            var actualHeader = header != null ? JObject.Parse(header) : new JObject
            {
                { JwtClaimTypes.Alg, signingCredentials.Algorithm },
                { JwtClaimTypes.Kid, signingCredentials.Key.KeyId },
                { JwtClaimTypes.Typ, "JWT"}
            };

            if (header == null && signingCredentials.Key is X509SecurityKey x509SecurityKey)
                actualHeader[JwtClaimTypes.X5t] = x509SecurityKey.X5t;

            var cryptoFactory = signingCredentials.CryptoProviderFactory ?? signingCredentials.Key.CryptoProviderFactory;
            var signatureProvider = cryptoFactory.CreateForSigning(signingCredentials.Key, signingCredentials.Algorithm);

            try
            {
                var message = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(actualHeader.ToString(Formatting.None))) + "." + Base64UrlEncoder.Encode(payload);
                return message + "." + Base64UrlEncoder.Encode(signatureProvider.Sign(Encoding.UTF8.GetBytes(message)));
            }
            finally
            {
                cryptoFactory.ReleaseSignatureProvider(signatureProvider);
            }
        }
        private static string CreateJwkClaim(RsaSecurityKey key, string algorithm)
        {
            var parameters = key.Rsa == null ? key.Parameters : key.Rsa.ExportParameters(false);
            return "{\"kty\":\"RSA\",\"n\":\"" + Base64UrlEncoder.Encode(parameters.Modulus) + "\",\"e\":\"" + Base64UrlEncoder.Encode(parameters.Exponent) + "\",\"alg\":\"" + algorithm + "\",\"kid\":\"" + key.KeyId + "\"}";
        }
    }

}
