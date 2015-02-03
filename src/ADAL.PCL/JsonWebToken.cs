//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class JsonWebTokenConstants
    {
        public const uint JwtToAadLifetimeInSeconds = 60 * 10; // Ten minutes

        public const string HeaderType = "JWT";

        internal class Algorithms
        {
            public const string RsaSha256 = "RS256";
            public const string None = "none";
        }

        internal class ReservedClaims
        {
            public const string Audience = "aud";
            public const string Issuer = "iss";
            public const string Subject = "sub";
            public const string NotBefore = "nbf";
            public const string ExpiresOn = "exp";
            public const string JwtIdentifier = "jti";
        }

        internal class ReservedHeaderParameters
        {
            public const string Algorithm = "alg";
            public const string Type = "typ";
            public const string X509CertificateThumbprint = "x5t";
        }
    }   

    internal class JsonWebToken
    {
        // (64K) This is an arbitrary large value for the token length. We can adjust it as needed.
        private const int MaxTokenLength = 65536;   

        private readonly JWTPayload payload;

        public JsonWebToken(ClientAssertionCertificate certificate, string audience)
        {
            DateTime validFrom = PlatformPlugin.HttpClientFactory.GetJsonWebTokenValidFrom();

            DateTime validTo = validFrom + TimeSpan.FromSeconds(JsonWebTokenConstants.JwtToAadLifetimeInSeconds);

            this.payload = new JWTPayload
                           {
                               Audience = audience,
                               Issuer = certificate.ClientId,
                               ValidFrom = ConvertToTimeT(validFrom),
                               ValidTo = ConvertToTimeT(validTo),
                               Subject = certificate.ClientId,
                               JwtIdentifier = PlatformPlugin.HttpClientFactory.GetJsonWebTokenId()
                           };
        }

        public ClientAssertion Sign(ClientAssertionCertificate credential)
        {
            // Base64Url encoded header and claims
            string token = this.Encode(credential);     

            // Length check before sign
            if (MaxTokenLength < token.Length)
            {
                throw new AdalException(AdalError.EncodedTokenTooLong);
            }

            return new ClientAssertion(this.payload.Issuer, string.Concat(token, ".", UrlEncodeSegment(credential.Sign(token))));
        }

        private static string EncodeSegment(string segment)
        {
            return UrlEncodeSegment(Encoding.UTF8.GetBytes(segment));
        }

        private static string UrlEncodeSegment(byte[] segment)
        {
            return Base64UrlEncoder.Encode(segment);
        }

        private static string EncodeToJson<T>(T toEncode)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
                ser.WriteObject(stream, toEncode);
                return Encoding.UTF8.GetString(stream.ToArray(), 0, (int)stream.Position);
            }
        }

        private static string EncodeHeaderToJson(ClientAssertionCertificate credential)
        {
            JWTHeaderWithCertificate header = new JWTHeaderWithCertificate(credential);
            return EncodeToJson(header);
        }

        private static long ConvertToTimeT(DateTime time)
        {
            var startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = time - startTime;
            return (long)(diff.TotalSeconds);
        }

        private string Encode(ClientAssertionCertificate credential)
        {
            // Header segment
            string jsonHeader = EncodeHeaderToJson(credential);

            string encodedHeader = EncodeSegment(jsonHeader);

            // Payload segment
            string jsonPayload = EncodeToJson(this.payload);

            string encodedPayload = EncodeSegment(jsonPayload);

            return string.Concat(encodedHeader, ".", encodedPayload);
        }

        [DataContract]
        internal class JWTHeader
        {
            protected ClientAssertionCertificate Credential { get; private set; }

            public JWTHeader(ClientAssertionCertificate credential)
            {
                this.Credential = credential;
            }

            [DataMember(Name = JsonWebTokenConstants.ReservedHeaderParameters.Type)]
            public static string Type
            {
                get
                {
                    return JsonWebTokenConstants.HeaderType;
                }

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
                    return this.Credential == null ? JsonWebTokenConstants.Algorithms.None : JsonWebTokenConstants.Algorithms.RsaSha256;
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

            [DataMember(Name = JsonWebTokenConstants.ReservedClaims.JwtIdentifier, IsRequired=false, EmitDefaultValue=false)]
            public string JwtIdentifier { get; set; }
        }

        [DataContract]
        internal sealed class JWTHeaderWithCertificate : JWTHeader
        {
            public JWTHeaderWithCertificate(ClientAssertionCertificate credential)
                : base(credential)
            {
            }

            [DataMember(Name = JsonWebTokenConstants.ReservedHeaderParameters.X509CertificateThumbprint)]
            public string X509CertificateThumbprint
            {
                get
                {
                    // Thumbprint should be url encoded
                    return PlatformPlugin.CryptographyHelper.GetX509CertificateThumbprint(this.Credential);
                }

                set
                {
                    // This setter is required by DataContractJsonSerializer
                }
            }
        }
    }
}
