// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using System.Linq;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

    internal class JsonWebToken
    {
        // (64K) This is an arbitrary large value for the token length. We can adjust it as needed.
        private const int MaxTokenLength = 65536;
        public readonly JWTPayload Payload;
        public IDictionary<string, string> ClaimsToSign { get; private set; }
        public long ValidTo { get { return Payload.ValidTo; } }
        private readonly ICryptographyManager _cryptographyManager;

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

        public JsonWebToken(ICryptographyManager cryptographyManager, string clientId, string audience, IDictionary<string, string> claimsToSign)
            : this(cryptographyManager, clientId, audience)
        {
            ClaimsToSign = claimsToSign;
        }

        public string Sign(ClientCredentialWrapper credential, bool sendCertificate)
        {
            // Base64Url encoded header and claims
            string token = Encode(credential, sendCertificate);

            // Length check before sign
            if (MaxTokenLength < token.Length)
            {
                throw new MsalException(MsalError.EncodedTokenTooLong);
            }

            return string.Concat(token, ".", UrlEncodeSegment(credential.Sign(_cryptographyManager, token)));
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
                //add opening bracket
                jsonPayload = "{";
                foreach (KeyValuePair<string, string> claim in ClaimsToSign)
                {

                    jsonPayload = jsonPayload + string.Format(CultureInfo.InvariantCulture, "\"{0}\":\"{1}\",",
#if WINDOWS_APP || NETSTANDARD1_3
                                                claim.Key.ToString(),
                                                claim.Value.ToString());
#else
                                                claim.Key.ToString(CultureInfo.InvariantCulture),
                                                claim.Value.ToString(CultureInfo.InvariantCulture));
#endif
                }

                //remove last comma
                jsonPayload = jsonPayload.Substring(0, jsonPayload.Length - 1);
                //add closing bracket
                jsonPayload = jsonPayload + "}";
            }
            else
            {
                jsonPayload = JsonHelper.SerializeToJson(Payload);
            }

            string encodedPayload = EncodeSegment(jsonPayload);

            return string.Concat(encodedHeader, ".", encodedPayload);
        }

        [DataContract]
        internal class JWTHeader
        {
            public JWTHeader(ClientCredentialWrapper credential)
            {
                Credential = credential;
            }

            protected ClientCredentialWrapper Credential { get; }

            [DataMember(Name = JsonWebTokenConstants.ReservedHeaderParameters.Type)]
            public static string Type
            {
                get { return JsonWebTokenConstants.HeaderType; }

                set
                {
                    // This setter is required by DataContractJsonSerializer
                }
            }

            [DataMember(Name = JsonWebTokenConstants.ReservedHeaderParameters.Algorithm)]
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
                    // This setter is required by DataContractJsonSerializer
                }
            }
        }

        [DataContract]
        internal class JWTPayload
        {
            [DataMember(Name = JsonWebTokenConstants.ReservedClaims.Audience)]
            public string Audience { get; set; }

            [DataMember(Name = JsonWebTokenConstants.ReservedClaims.Issuer)]
            public string Issuer { get; set; }

            [DataMember(Name = JsonWebTokenConstants.ReservedClaims.NotBefore)]
            public long ValidFrom { get; set; }

            [DataMember(Name = JsonWebTokenConstants.ReservedClaims.ExpiresOn)]
            public long ValidTo { get; set; }

            [DataMember(Name = JsonWebTokenConstants.ReservedClaims.Subject, IsRequired = false,
                EmitDefaultValue = false)]
            public string Subject { get; set; }

            [DataMember(Name = JsonWebTokenConstants.ReservedClaims.JwtIdentifier, IsRequired = false,
                EmitDefaultValue = false)]
            public string JwtIdentifier { get; set; }
        }

        [DataContract]
        internal sealed class JWTHeaderWithCertificate : JWTHeader
        {
            public JWTHeaderWithCertificate(ClientCredentialWrapper credential, bool sendCertificate)
                : base(credential)
            {
                X509CertificateThumbprint = Credential.Thumbprint;
                X509CertificatePublicCertValue = null;

                if (!sendCertificate)
                {
                    return;
                }

#if DESKTOP
                X509CertificatePublicCertValue = Convert.ToBase64String(credential.Certificate.GetRawCertData());
#else
                X509CertificatePublicCertValue = Convert.ToBase64String(credential.Certificate.RawData);
#endif
            }

            [DataMember(Name = JsonWebTokenConstants.ReservedHeaderParameters.X509CertificateThumbprint)]
            public string X509CertificateThumbprint { get; set; }

            [DataMember(Name = JsonWebTokenConstants.ReservedHeaderParameters.X509CertificatePublicCertValue, EmitDefaultValue = false)]
            public string X509CertificatePublicCertValue { get; set; }
        }
    }
#endif
}
