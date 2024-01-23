// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;
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

        public string Sign(X509Certificate2 certificate, string base64EncodedThumbprint, bool sendX5C)
        {
            // Base64Url encoded header and claims
            string token = Encode(certificate, base64EncodedThumbprint, sendX5C);

            // Length check before sign
            if (MaxTokenLength < token.Length)
            {
                throw new MsalException(MsalError.EncodedTokenTooLong);
            }

            byte[] signature = _cryptographyManager.SignWithCertificate(token, certificate);
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

        private static string EncodeHeaderToJson(X509Certificate2 certificate, string base64EncodedThumbprint, bool sendX5C)
        {
            JWTHeaderWithCertificate header = new JWTHeaderWithCertificate(certificate, base64EncodedThumbprint, sendX5C);
            return JsonHelper.SerializeToJson(header);
        }

        internal static long ConvertToTimeT(DateTime time)
        {
            var startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = time - startTime;
            return (long)diff.TotalSeconds;
        }

        private string Encode(X509Certificate2 certificate, string base64EncodedThumbprint, bool sendCertificate)
        {
            // Header segment
            string jsonHeader = EncodeHeaderToJson(certificate, base64EncodedThumbprint, sendCertificate);

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
        internal class JWTHeader
        {
            public JWTHeader(X509Certificate2 certificate)
            {
                Certificate = certificate;
            }

            protected X509Certificate2 Certificate { get; }

            [JsonProperty(JsonWebTokenConstants.ReservedHeaderParameters.Type)]
            public string Type
            {
                get { return JsonWebTokenConstants.JWTHeaderType; }

                set
                {
                    // This setter is required by the serializer
                }
            }

            [JsonProperty(JsonWebTokenConstants.ReservedHeaderParameters.Algorithm)]
            public string Algorithm
            {
                get
                {
                    return Certificate == null
                        ? JsonWebTokenConstants.Algorithms.None
                        : JsonWebTokenConstants.Algorithms.RsaSha256;
                }

                set
                {
                    // This setter is required by the serializer
                }
            }
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

        [JsonObject]
        [Preserve(AllMembers = true)]
        internal sealed class JWTHeaderWithCertificate : JWTHeader
        {
            public JWTHeaderWithCertificate(X509Certificate2 certificate, string base64EncodedThumbprint, bool sendCertificate)
                : base(certificate)
            {
                // this is just Base64UrlHelpers.Encode(certificate.GetCertHash()) but computed higher up so that it can be cached
                X509CertificateThumbprint = base64EncodedThumbprint;
                X509CertificateKeyId = certificate.Thumbprint;

                X509CertificatePublicCertValue = null;

                if (sendCertificate)
                {
#if NETFRAMEWORK
                    X509CertificatePublicCertValue = Convert.ToBase64String(certificate.GetRawCertData());
#else
                    X509CertificatePublicCertValue = Convert.ToBase64String(certificate.RawData);
#endif
                }
            }

            /// <summary>
            /// x5t = base64 URL encoded cert thumbprint 
            /// </summary>
            /// <remarks>
            /// Mandatory for ADFS 2019
            /// </remarks>
            [JsonProperty(JsonWebTokenConstants.ReservedHeaderParameters.X509CertificateThumbprint)]
            public string X509CertificateThumbprint { get; set; }

            /// <summary>
            /// kid (key id) = cert thumbprint
            /// </summary>
            /// <remarks>
            /// Key Id is an optional param, but recommended. Wilson adds both kid and x5t to JWT header
            /// </remarks>
#if SUPPORTS_SYSTEM_TEXT_JSON
            [JsonProperty(JsonWebTokenConstants.ReservedHeaderParameters.KeyId)]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
#else
            [JsonProperty(
                PropertyName = JsonWebTokenConstants.ReservedHeaderParameters.KeyId,
                DefaultValueHandling = DefaultValueHandling.Ignore)]
#endif
            public string X509CertificateKeyId { get; set; }

#if SUPPORTS_SYSTEM_TEXT_JSON
            [JsonProperty(JsonWebTokenConstants.ReservedHeaderParameters.X509CertificatePublicCertValue)]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
#else
            [JsonProperty(
                PropertyName = JsonWebTokenConstants.ReservedHeaderParameters.X509CertificatePublicCertValue,
                DefaultValueHandling = DefaultValueHandling.Ignore)]
#endif
            public string X509CertificatePublicCertValue { get; set; }
        }
    }
}
