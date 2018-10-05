//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Helpers;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.ClientCreds
{
    internal static class JsonWebTokenConstants
    {
        public const uint JwtToAadLifetimeInSeconds = 60 * 10; // Ten minutes

        public const string HeaderType = "JWT";

        internal static class Algorithms
        {
            public const string RsaSha256 = "RS256";
            public const string None = "none";
        }

        internal static class ReservedClaims
        {
            public const string Audience = "aud";
            public const string Issuer = "iss";
            public const string Subject = "sub";
            public const string NotBefore = "nbf";
            public const string ExpiresOn = "exp";
            public const string JwtIdentifier = "jti";
        }

        internal static class ReservedHeaderParameters
        {
            public const string Algorithm = "alg";
            public const string Type = "typ";
            public const string X509CertificateThumbprint = "x5t";
            public const string X509CertificatePublicCertValue = "x5c";
        }
    }

    internal class JsonWebToken
    {
        // (64K) This is an arbitrary large value for the token length. We can adjust it as needed.
        private const int MaxTokenLength = 65536;

        private readonly JWTPayload payload;

        public JsonWebToken(IClientAssertionCertificate certificate, string audience)
        {
            DateTime validFrom = DateTime.UtcNow;
            DateTime validTo = validFrom + TimeSpan.FromSeconds(JsonWebTokenConstants.JwtToAadLifetimeInSeconds);

            payload = new JWTPayload
            {
                Audience = audience,
                Issuer = certificate.ClientId,
                ValidFrom = ConvertToTimeT(validFrom),
                ValidTo = ConvertToTimeT(validTo),
                Subject = certificate.ClientId,
                JwtIdentifier = Guid.NewGuid().ToString()
            };
        }

        public ClientAssertion Sign(IClientAssertionCertificate credential, bool sendX5c)
        {
            // Base64Url encoded header and claims
            string token = Encode(credential, sendX5c);

            // Length check before sign
            if (MaxTokenLength < token.Length)
            {
                throw new AdalException(AdalError.EncodedTokenTooLong);
            }

            return new ClientAssertion(payload.Issuer, string.Concat(token, ".", UrlEncodeSegment(credential.Sign(token))));
        }

        private static string EncodeSegment(string segment)
        {
            return UrlEncodeSegment(Encoding.UTF8.GetBytes(segment));
        }

        private static string UrlEncodeSegment(byte[] segment)
        {
            return Base64UrlHelpers.Encode(segment);
        }

        private static string EncodeHeaderToJson(IClientAssertionCertificate credential, bool sendX5c)
        {
            JWTHeaderWithCertificate header = new JWTHeaderWithCertificate(credential, sendX5c);
            return JsonHelper.SerializeToJson(header);
        }

        private static long ConvertToTimeT(DateTime time)
        {
            var startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = time - startTime;
            return (long)(diff.TotalSeconds);
        }

        private string Encode(IClientAssertionCertificate credential, bool sendX5c)
        {
            // Header segment
            string jsonHeader = EncodeHeaderToJson(credential, sendX5c);
            string encodedHeader = EncodeSegment(jsonHeader);

            // Payload segment
            string jsonPayload = JsonHelper.SerializeToJson(payload);

            string encodedPayload = EncodeSegment(jsonPayload);

            return string.Concat(encodedHeader, ".", encodedPayload);
        }

        [DataContract]
        internal class JWTHeader
        {
            protected IClientAssertionCertificate Credential { get; private set; }
            private string _type;
            private string _alg;

            public JWTHeader(IClientAssertionCertificate credential)
            {
                Credential = credential;
                _alg = (Credential == null)
                    ? JsonWebTokenConstants.Algorithms.None
                    : JsonWebTokenConstants.Algorithms.RsaSha256;

                _type = JsonWebTokenConstants.HeaderType;
            }

            [DataMember(Name = JsonWebTokenConstants.ReservedHeaderParameters.Type)]
            public string Type
            {
                get
                {
                    return _type;
                }

                set { _type = value; }
            }

            [DataMember(Name = JsonWebTokenConstants.ReservedHeaderParameters.Algorithm)]
            public string Algorithm
            {
                get
                {
                    return _alg;
                }


                set { _alg = value; }
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

            [DataMember(Name = JsonWebTokenConstants.ReservedClaims.JwtIdentifier, IsRequired = false, EmitDefaultValue = false)]
            public string JwtIdentifier { get; set; }
        }

        [DataContract]
        internal sealed class JWTHeaderWithCertificate : JWTHeader
        {
            public JWTHeaderWithCertificate(IClientAssertionCertificate credential, bool sendX5c)
                : base(credential)
            {
                X509CertificateThumbprint = Credential.Thumbprint;
                X509CertificatePublicCertValue = null;

                if (!sendX5c)
                {
                    return;
                }

                //Check to see if credential is our implementation or developer provided.
                if (credential.GetType().ToString() != "Microsoft.IdentityModel.Clients.ActiveDirectory.ClientAssertionCertificate")
                {
                    CoreLoggerBase.Default.Warning("The implementation of IClientAssertionCertificate is developer provided and it should be replaced with library provided implementation.");
                    return;
                }

#if  NET45
                if (credential is ClientAssertionCertificate cert)
                {
                    X509CertificatePublicCertValue = Convert.ToBase64String(cert.Certificate.GetRawCertData());
                }
#elif NETSTANDARD1_3
                if (credential is ClientAssertionCertificate cert)
                {
                    X509CertificatePublicCertValue = Convert.ToBase64String(cert.Certificate.RawData);
                }
#endif
            }

            [DataMember(Name = JsonWebTokenConstants.ReservedHeaderParameters.X509CertificateThumbprint)]
            public string X509CertificateThumbprint { get; set; }

            [DataMember(Name = JsonWebTokenConstants.ReservedHeaderParameters.X509CertificatePublicCertValue, EmitDefaultValue = false)]
            public string X509CertificatePublicCertValue { get; set; }
        }
    }
}
