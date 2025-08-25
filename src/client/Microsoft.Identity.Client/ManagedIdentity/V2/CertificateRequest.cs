// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal class CertificateRequest
    {
        private X500DistinguishedName _subjectName;
        private RSA _rsa;
        private HashAlgorithmName _hashAlgorithmName;
        private RSASignaturePadding _rsaPadding;

        internal CertificateRequest(
            X500DistinguishedName subjectName,
            RSA key,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding)
        {
            _subjectName = subjectName;
            _rsa = key;
            _hashAlgorithmName = hashAlgorithm;
            _rsaPadding = padding;
        }

        internal Collection<AsnEncodedData> OtherRequestAttributes { get; } = new Collection<AsnEncodedData>();

        private static string MakePem(byte[] ber, string header)
        {
            const int LineLength = 64;

            string base64 = Convert.ToBase64String(ber);
            int offset = 0;

            StringBuilder builder = new StringBuilder("-----BEGIN ");
            builder.Append(header);
            builder.AppendLine("-----");

            while (offset < base64.Length)
            {
                int lineEnd = Math.Min(offset + LineLength, base64.Length);
                builder.AppendLine(base64.Substring(offset, lineEnd - offset));
                offset = lineEnd;
            }

            builder.Append("-----END ");
            builder.Append(header);
            builder.AppendLine("-----");

            return builder.ToString();
        }

        internal string CreateSigningRequestPem()
        {
            byte[] csr = CreateSigningRequest();
            return MakePem(csr, "CERTIFICATE REQUEST");
        }

        internal byte[] CreateSigningRequest()
        {
            if (_hashAlgorithmName != HashAlgorithmName.SHA256)
            {
                throw new NotSupportedException("Signature Processing has only been written for SHA256");
            }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            // RSAPublicKey ::= SEQUENCE {
            //     modulus           INTEGER,  -- n
            //     publicExponent    INTEGER   -- e
            // }

            using (writer.PushSequence())
            {
                RSAParameters rsaParameters = _rsa.ExportParameters(false);
                writer.WriteIntegerUnsigned(rsaParameters.Modulus);
                writer.WriteIntegerUnsigned(rsaParameters.Exponent);
            }

            byte[] publicKey = writer.Encode();
            writer.Reset();

            // CertificationRequestInfo ::= SEQUENCE {
            //      version       INTEGER { v1(0) } (v1,...),
            //      subject       Name,
            //      subjectPKInfo SubjectPublicKeyInfo{{ PKInfoAlgorithms }},
            //      attributes    [0] Attributes{{ CRIAttributes }}
            // }
            //
            // SubjectPublicKeyInfo { ALGORITHM: IOSet} ::= SEQUENCE {
            //     algorithm AlgorithmIdentifier { { IOSet} },
            //     subjectPublicKey BIT STRING
            // }
            //
            // Attributes { ATTRIBUTE:IOSet } ::= SET OF Attribute{{ IOSet }}
            //
            // Attribute { ATTRIBUTE:IOSet } ::= SEQUENCE {
            //     type   ATTRIBUTE.&id({IOSet}),
            //     values SET SIZE(1..MAX) OF ATTRIBUTE.&Type({IOSet}{@type})
            // }

            using (writer.PushSequence())
            {
                writer.WriteInteger(0);
                writer.WriteEncodedValue(_subjectName.RawData);

                // subjectPKInfo
                using (writer.PushSequence())
                {
                    // algorithm
                    using (writer.PushSequence())
                    {
                        writer.WriteObjectIdentifier("1.2.840.113549.1.1.1");
                        // RSA uses an explicit NULL value for parameters
                        writer.WriteNull();
                    }

                    writer.WriteBitString(publicKey);
                }

                if (OtherRequestAttributes.Count > 0)
                {
                    // attributes
                    using (writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 0)))
                    {
                        foreach (AsnEncodedData attribute in OtherRequestAttributes)
                        {
                            using (writer.PushSequence())
                            {
                                writer.WriteObjectIdentifier(attribute.Oid.Value);

                                using (writer.PushSetOf())
                                {
                                    writer.WriteEncodedValue(attribute.RawData);
                                }
                            }
                        }
                    }
                }
            }

            byte[] certReqInfo = writer.Encode();
            writer.Reset();

            // CertificationRequest ::= SEQUENCE {
            //     certificationRequestInfo CertificationRequestInfo,
            //     signatureAlgorithm AlgorithmIdentifier{{ SignatureAlgorithms }},
            //     signature          BIT STRING
            // }

            using (writer.PushSequence())
            {
                writer.WriteEncodedValue(certReqInfo);

                // signatureAlgorithm
                using (writer.PushSequence())
                {
                    if (_rsaPadding == RSASignaturePadding.Pss)
                    {
                        if (_hashAlgorithmName != HashAlgorithmName.SHA256)
                        {
                            throw new NotSupportedException("Only SHA256 is supported with PSS padding.");
                        }

                        writer.WriteObjectIdentifier("1.2.840.113549.1.1.10");

                        // RSASSA-PSS-params ::= SEQUENCE {
                        //     hashAlgorithm      [0] HashAlgorithm      DEFAULT sha1,
                        //     maskGenAlgorithm   [1] MaskGenAlgorithm   DEFAULT mgf1SHA1,
                        //     saltLength         [2] INTEGER            DEFAULT 20,
                        //     trailerField       [3] TrailerField       DEFAULT trailerFieldBC
                        // }

                        using (writer.PushSequence())
                        {
                            string digestOid = "2.16.840.1.101.3.4.2.1";

                            // hashAlgorithm
                            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
                            {
                                using (writer.PushSequence())
                                {
                                    writer.WriteObjectIdentifier(digestOid);
                                }
                            }

                            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1)))
                            {
                                using (writer.PushSequence())
                                {
                                    writer.WriteObjectIdentifier("1.2.840.113549.1.1.8");

                                    using (writer.PushSequence())
                                    {
                                        writer.WriteObjectIdentifier(digestOid);
                                    }
                                }
                            }

                            // saltLength (SHA256.Length, 32 bytes)
                            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 2)))
                            {
                                writer.WriteInteger(32);
                            }

                            // trailerField 1, which is trailerFieldBC, which is the DEFAULT,
                            // so don't write it down.
                        }
                    }
                    else if (_rsaPadding == RSASignaturePadding.Pkcs1)
                    {
                        writer.WriteObjectIdentifier("1.2.840.113549.1.1.11");
                        // RSA PKCS1 uses an explicit NULL value for parameters
                        writer.WriteNull();
                    }
                    else
                    {
                        throw new NotSupportedException("Unsupported RSA padding.");
                    }
                }

                byte[] signature = _rsa.SignData(certReqInfo, _hashAlgorithmName, _rsaPadding);
                writer.WriteBitString(signature);
            }

            return writer.Encode();
        }
    }
}
