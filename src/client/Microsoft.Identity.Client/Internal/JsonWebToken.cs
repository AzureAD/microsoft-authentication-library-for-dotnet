// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;
using System.Security.Cryptography;
#if SUPPORTS_SYSTEM_TEXT_JSON
using JObject = System.Text.Json.Nodes.JsonObject;
#else
using Microsoft.Identity.Json.Linq;
#endif

namespace Microsoft.Identity.Client.Internal
{
    internal class JsonWebToken
    {
        // (64K) This is an arbitrary large value for the token length. We can adjust it as needed.
        private const int MaxTokenLength = 65536;
        public const long JwtToAadLifetimeInSeconds = 60 * 10; // Ten minutes

        private readonly IDictionary<string, string> _claimsToSign;
        private readonly ICryptographyManager _cryptographyManager;
        private readonly string _clientId;
        private readonly string _audience;
        private readonly bool _appendDefaultClaims;

        public JsonWebToken(ICryptographyManager cryptographyManager, string clientId, string audience)
        {
            _cryptographyManager = cryptographyManager;
            _clientId = clientId;
            _audience = audience;
        }

        public JsonWebToken(
             ICryptographyManager cryptographyManager,
             string clientId,
             string audience,
             IDictionary<string, string> claimsToSign,
             bool appendDefaultClaims = false)
         : this(cryptographyManager, clientId, audience)
        {
            _claimsToSign = claimsToSign;
            _appendDefaultClaims = appendDefaultClaims;
        }

        private string CreateJsonPayload()
        {
            long validFrom = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long validTo = validFrom + JwtToAadLifetimeInSeconds; // 10 min

            if (_claimsToSign == null || _claimsToSign.Count == 0)
            {
                return $$"""{"aud":"{{_audience}}","iss":"{{_clientId}}","sub":"{{_clientId}}","nbf":"{{validFrom}}","exp":"{{validTo}}","jti":"{{Guid.NewGuid()}}"}""";
            }

            // extra claims
            StringBuilder payload = new StringBuilder();

            if (_appendDefaultClaims)
            {
                string defaultClaims = $$"""{"aud":"{{_audience}}","iss":"{{_clientId}}","sub":"{{_clientId}}","nbf":"{{validFrom}}","exp":"{{validTo}}","jti":"{{Guid.NewGuid()}}",""";
                payload.Append(defaultClaims);
            }
            else
            {
                payload.Append('{');
            }

            var json = new JObject();

            foreach (var claim in _claimsToSign)
            {
                json[claim.Key] = claim.Value;
            }

            var jsonClaims = JsonHelper.JsonObjectToString(json);

            //Remove extra brackets from JSON result
            payload.Append(jsonClaims.Substring(1, jsonClaims.Length - 2));

            payload.Append('}');

            return payload.ToString();
        }

        public string Sign(X509Certificate2 certificate, bool sendX5C, bool useSha2AndPss)
        {
            // Base64Url encoded header and claims
            string token = CreateJwtHeaderAndBody(certificate, sendX5C, useSha2AndPss);

            // Length check before sign
            if (MaxTokenLength < token.Length)
            {
                throw new MsalClientException(MsalError.EncodedTokenTooLong);
            }

            //codeql [SM03799] Backwards Compatibility: Requires using PKCS1 padding for Identity Providers not supporting PSS (AAD, B2C, CIAM support it)
            byte[] signature = _cryptographyManager.SignWithCertificate(
                token,
                certificate,
                useSha2AndPss ?
                    RSASignaturePadding.Pss :      // ESTS added support for PSS
                    RSASignaturePadding.Pkcs1);    // Other IdPs may only support PKCS1

            return string.Concat(token, ".", Base64UrlHelpers.Encode(signature));
        }

        private static string CreateJsonHeader(X509Certificate2 certificate, bool sendX5C, bool useSha2AndPss)
        {
            string thumbprint = ComputeCertThumbprint(certificate, useSha2AndPss);

            string alg = useSha2AndPss ? "PS256" : "RS256";
            string thumbprintKey = useSha2AndPss ? "x5t#S256" : "x5t";
            string header;

            if (sendX5C)
            {
#if NETFRAMEWORK
                string x5cValue = Convert.ToBase64String(certificate.GetRawCertData());
#else
                string x5cValue = Convert.ToBase64String(certificate.RawData);
#endif
                header = $$"""{"alg":"{{alg}}","typ":"JWT","{{thumbprintKey}}":"{{thumbprint}}","x5c":"{{x5cValue}}"}""";
            }
            else
            {

                header = $$"""{"alg":"{{alg}}","typ":"JWT","{{thumbprintKey}}":"{{thumbprint}}"}""";
            }

            return header;
        }

        private static string ComputeCertThumbprint(X509Certificate2 certificate, bool useSha2)
        {
            string thumbprint = null;
            try
            {
                if (useSha2)
                {
#if NET6_0_OR_GREATER

                    thumbprint = Base64UrlHelpers.Encode(certificate.GetCertHash(HashAlgorithmName.SHA256));
#else
                using (var hasher = SHA256.Create())
                {
                    byte[] hash = hasher.ComputeHash(certificate.RawData);
                    thumbprint = Base64UrlHelpers.Encode(hash);
                }
#endif
                }
                else
                {
                    thumbprint = Base64UrlHelpers.Encode(certificate.GetCertHash());
                }
            }
            catch (CryptographicException ex)
            {
                throw new MsalClientException(MsalError.CryptographicError, MsalErrorMessage.CryptographicError, ex);
            }
            return thumbprint;
        }

        private string CreateJwtHeaderAndBody(
            X509Certificate2 certificate,
            bool addX5C,
            bool useSha2AndPss)
        {

            string jsonHeader = CreateJsonHeader(certificate, addX5C, useSha2AndPss);
            string encodedHeader = Base64UrlHelpers.EncodeString(jsonHeader);

            string jsonPayload = CreateJsonPayload();
            string encodedPayload = Base64UrlHelpers.EncodeString(jsonPayload);

            return string.Concat(encodedHeader, ".", encodedPayload);
        }
    }
}
