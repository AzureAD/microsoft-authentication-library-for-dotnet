// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;
using System.Security.Cryptography;

#if SUPPORTS_SYSTEM_TEXT_JSON
using Microsoft.Identity.Client.Platforms.net6;
using System.Text.Json.Serialization;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JObject = System.Text.Json.Nodes.JsonObject;
#else
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;
#endif

namespace Microsoft.Identity.Client.Internal
{
    internal class JsonWebToken
    {
        // (64K) This is an arbitrary large value for the token length. We can adjust it as needed.
        private const int MaxTokenLength = 65536;
        public readonly JWTPayload Payload;
        public IDictionary<string, string> ClaimsToSign { get; private set; }
        public long ValidTo { get { return Payload.ValidTo; } }
        private readonly ICryptographyManager _cryptographyManager;
        private readonly bool _appendDefaultClaims;

        public JsonWebToken(ICryptographyManager cryptographyManager, string clientId, string audience)
        {
            _cryptographyManager = cryptographyManager;
            DateTime validFrom = DateTime.UtcNow;

            Payload = new JWTPayload
            {
                Audience = audience,
                Issuer = clientId,
                ValidFrom = ConvertToTimeT(validFrom),
                ValidTo = ConvertToTimeT(validFrom + TimeSpan.FromSeconds(JsonWebTokenConstants.JwtToAadLifetimeInSeconds)),
                Subject = clientId,
                JwtIdentifier = Guid.NewGuid().ToString()
            };
        }

        public JsonWebToken(ICryptographyManager cryptographyManager, string clientId, string audience, IDictionary<string, string> claimsToSign, bool appendDefaultClaims = false)
            : this(cryptographyManager, clientId, audience)
        {
            ClaimsToSign = claimsToSign;
            _appendDefaultClaims = appendDefaultClaims;
        }

        public string Sign(X509Certificate2 certificate,  bool sendX5C, bool useSha2AndPss)
        {
            // Base64Url encoded header and claims
            string token = Encode(certificate, sendX5C, useSha2AndPss);

            // Length check before sign
            if (MaxTokenLength < token.Length)
            {
                throw new MsalClientException(MsalError.EncodedTokenTooLong);
            }

            byte[] signature = _cryptographyManager.SignWithCertificate(
                token,
                certificate,
                useSha2AndPss ?
                    System.Security.Cryptography.RSASignaturePadding.Pss :      // ESTS added support for PSS
                    System.Security.Cryptography.RSASignaturePadding.Pkcs1);    // Other IdPs may only support PKCS1

            return string.Concat(token, ".", UrlEncodeSegment(signature));
        }

        private static string EncodeSegment(string segment)
        {
            return UrlEncodeSegment(Encoding.UTF8.GetBytes(segment));
        }

        private static string UrlEncodeSegment(byte[] segment)
        {
            return Base64UrlHelpers.Encode(segment);
        }

        private static string EncodeHeaderToJson(X509Certificate2 certificate, bool sendX5C, bool useSha2AndPss)
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

            return thumbprint;
        }

        internal static long ConvertToTimeT(DateTime time)
        {
            var startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = time - startTime;
            return (long)diff.TotalSeconds;
        }

        private string Encode(
            X509Certificate2 certificate,
            bool addX5C,
            bool useSha2AndPss)
        {
            // Header segment
            string jsonHeader = EncodeHeaderToJson(certificate, addX5C, useSha2AndPss);

            string encodedHeader = EncodeSegment(jsonHeader);
            string jsonPayload;

            // Payload segment
            if (ClaimsToSign != null && ClaimsToSign.Any())
            {
                var json = new JObject();

                if (_appendDefaultClaims)
                {
                    json[JsonWebTokenConstants.ReservedClaims.Audience] = Payload.Audience;
                    json[JsonWebTokenConstants.ReservedClaims.Issuer] = Payload.Issuer;
                    json[JsonWebTokenConstants.ReservedClaims.NotBefore] = Payload.ValidFrom;
                    json[JsonWebTokenConstants.ReservedClaims.ExpiresOn] = Payload.ValidTo;
                    json[JsonWebTokenConstants.ReservedClaims.Subject] = Payload.Subject;
                    json[JsonWebTokenConstants.ReservedClaims.JwtIdentifier] = Payload.JwtIdentifier;
                }

                foreach (var claim in ClaimsToSign)
                {
                    json[claim.Key] = claim.Value;
                }

                jsonPayload = JsonHelper.JsonObjectToString(json);
            }
            else
            {
                jsonPayload = JsonHelper.SerializeToJson(Payload);
            }

            string encodedPayload = EncodeSegment(jsonPayload);

            return string.Concat(encodedHeader, ".", encodedPayload);
        }

        [JsonObject]
        [Preserve(AllMembers = true)]
        internal class JWTPayload
        {
            [JsonProperty(JsonWebTokenConstants.ReservedClaims.Audience)]
            public string Audience { get; set; }

            [JsonProperty(JsonWebTokenConstants.ReservedClaims.Issuer)]
            public string Issuer { get; set; }

            [JsonProperty(JsonWebTokenConstants.ReservedClaims.NotBefore)]
#if SUPPORTS_SYSTEM_TEXT_JSON
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
#endif
            public long ValidFrom { get; set; }

            [JsonProperty(JsonWebTokenConstants.ReservedClaims.ExpiresOn)]
#if SUPPORTS_SYSTEM_TEXT_JSON
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
#endif
            public long ValidTo { get; set; }

#if SUPPORTS_SYSTEM_TEXT_JSON
            [JsonProperty(JsonWebTokenConstants.ReservedClaims.Subject)]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
#else
            [JsonProperty(
                PropertyName = JsonWebTokenConstants.ReservedClaims.Subject,
                DefaultValueHandling = DefaultValueHandling.Ignore)]
#endif
            public string Subject { get; set; }

#if SUPPORTS_SYSTEM_TEXT_JSON
            [JsonProperty(JsonWebTokenConstants.ReservedClaims.JwtIdentifier)]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
#else
            [JsonProperty(
                PropertyName = JsonWebTokenConstants.ReservedClaims.JwtIdentifier,
                DefaultValueHandling = DefaultValueHandling.Ignore)]
#endif
            public string JwtIdentifier { get; set; }
        }
    }
}
