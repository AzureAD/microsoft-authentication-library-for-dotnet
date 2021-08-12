// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;
using Microsoft.Identity.Json;

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
        private bool _appendDefaultClaims;

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

        public string Sign(ClientCredentialWrapper credential, bool sendX5C)
        {
            // Base64Url encoded header and claims
            string token = Encode(credential, sendX5C);

            // Length check before sign
            if (MaxTokenLength < token.Length)
            {
                throw new MsalException(MsalError.EncodedTokenTooLong);
            }

            byte[] signature = _cryptographyManager.SignWithCertificate(token, credential.Certificate);                
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

        private static string EncodeHeaderToJson(ClientCredentialWrapper credential, bool sendCertificate)
        {
            JWTHeaderWithCertificate header = new JWTHeaderWithCertificate(credential, sendCertificate);
            return JsonHelper.SerializeToJson(header);
        }

        internal static long ConvertToTimeT(DateTime time)
        {
            var startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = time - startTime;
            return (long)diff.TotalSeconds;
        }

        private string Encode(ClientCredentialWrapper credential, bool sendCertificate)
        {
            // Header segment
            string jsonHeader = EncodeHeaderToJson(credential, sendCertificate);

            string encodedHeader = EncodeSegment(jsonHeader);
            string jsonPayload;

            // Payload segment
            if (ClaimsToSign != null && ClaimsToSign.Any())
            {
                if (_appendDefaultClaims)
                {
                    JObject json = JObject.FromObject(Payload);
                    json.Merge(JObject.FromObject(ClaimsToSign));
                    jsonPayload = json.ToString();
                }
                else
                {
                    JObject json = JObject.FromObject(ClaimsToSign);
                    jsonPayload = json.ToString();
                }
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
            public JWTHeader(ClientCredentialWrapper credential)
            {
                Credential = credential;
            }

            protected ClientCredentialWrapper Credential { get; }

            [JsonProperty(PropertyName = JsonWebTokenConstants.ReservedHeaderParameters.Type)]
            public static string Type
            {
                get { return JsonWebTokenConstants.JWTHeaderType; }

                set
                {
                    // This setter is required by the serializer
                }
            }

            [JsonProperty(PropertyName = JsonWebTokenConstants.ReservedHeaderParameters.Algorithm)]
            public string Algorithm
            {
                get
                {
                    return Credential == null
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
            [JsonProperty(PropertyName = JsonWebTokenConstants.ReservedClaims.Audience)]
            public string Audience { get; set; }

            [JsonProperty(PropertyName = JsonWebTokenConstants.ReservedClaims.Issuer)]
            public string Issuer { get; set; }

            [JsonProperty(PropertyName = JsonWebTokenConstants.ReservedClaims.NotBefore)]
            public long ValidFrom { get; set; }

            [JsonProperty(PropertyName = JsonWebTokenConstants.ReservedClaims.ExpiresOn)]
            public long ValidTo { get; set; }

            [JsonProperty(
                PropertyName = JsonWebTokenConstants.ReservedClaims.Subject, 
                DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string Subject { get; set; }

            [JsonProperty(
                PropertyName = JsonWebTokenConstants.ReservedClaims.JwtIdentifier,
                DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string JwtIdentifier { get; set; }
        }

        [JsonObject]
        [Preserve(AllMembers = true)]
        internal sealed class JWTHeaderWithCertificate : JWTHeader
        {
            public JWTHeaderWithCertificate(ClientCredentialWrapper credential, bool? sendCertificate = null)
                : base(credential)
            {
                X509CertificateThumbprint = Credential.Thumbprint;
                if (Credential?.Certificate?.Thumbprint != null)
                {
                    X509CertificateKeyId = Credential.Certificate.Thumbprint;
                }

                X509CertificatePublicCertValue = null;

                bool perRequestSendX5C = sendCertificate ?? credential?.SendX5C ?? false;

                if (!perRequestSendX5C)
                {
                    return;
                }

#if DESKTOP
                X509CertificatePublicCertValue = Convert.ToBase64String(credential.Certificate.GetRawCertData());
#else
                X509CertificatePublicCertValue = Convert.ToBase64String(credential.Certificate.RawData);
#endif
            }

            /// <summary>
            /// x5t = base64 URL encoded cert thumbprint 
            /// </summary>
            /// <remarks>
            /// Mandatory for ADFS 2019
            /// </remarks>
            [JsonProperty(PropertyName = JsonWebTokenConstants.ReservedHeaderParameters.X509CertificateThumbprint)]
            public string X509CertificateThumbprint { get; set; }

            /// <summary>
            /// kid (key id) = cert thumbprint
            /// </summary>
            /// <remarks>
            /// Key Id is an optional param, but recommended. Wilson adds both kid and x5t to JWT header
            /// </remarks>
            [JsonProperty(PropertyName = JsonWebTokenConstants.ReservedHeaderParameters.KeyId, 
                DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string X509CertificateKeyId { get; set; }

            [JsonProperty(
                PropertyName = JsonWebTokenConstants.ReservedHeaderParameters.X509CertificatePublicCertValue, 
                DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string X509CertificatePublicCertValue { get; set; }
        }
    }
}
