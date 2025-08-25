// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Formats.Asn1;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    /// <summary>
    /// Helper class for parsing and validating Certificate Signing Request (CSR) content and structure.
    /// </summary>
    internal static class CsrValidator
    {
        /// <summary>
        /// Parses a PEM-encoded CSR and returns the DER bytes.
        /// </summary>
        public static byte[] ParseCsrFromPem(string pemCsr)
        {
            if (string.IsNullOrWhiteSpace(pemCsr))
                throw new ArgumentException("PEM CSR cannot be null or empty");

            const string beginMarker = "-----BEGIN CERTIFICATE REQUEST-----";
            const string endMarker = "-----END CERTIFICATE REQUEST-----";

            int beginIndex = pemCsr.IndexOf(beginMarker, StringComparison.Ordinal);
            int endIndex = pemCsr.IndexOf(endMarker, StringComparison.Ordinal);

            if (beginIndex < 0 || endIndex < 0)
                throw new ArgumentException("Invalid PEM format - missing CSR headers");

            beginIndex += beginMarker.Length;
            string base64Content = pemCsr.Substring(beginIndex, endIndex - beginIndex)
                .Replace("\r", "").Replace("\n", "").Replace(" ", "");

            try
            {
                return Convert.FromBase64String(base64Content);
            }
            catch (FormatException)
            {
                throw new FormatException("Invalid Base64 content in PEM CSR");
            }
        }

        /// <summary>
        /// Validates the content of a CSR PEM string against expected values.
        /// </summary>
        public static void ValidateCsrContent(string pemCsr, string expectedClientId, string expectedTenantId, CuidInfo expectedCuid)
        {
            byte[] csrBytes = ParseCsrFromPem(pemCsr);

            // Parse the CSR using AsnReader
            var reader = new AsnReader(csrBytes, AsnEncodingRules.DER);
            var csrSequence = reader.ReadSequence();

            // certificationRequestInfo
            var certReqInfoBytes = csrSequence.PeekEncodedValue().ToArray();
            var certReqInfoReader = new AsnReader(csrSequence.ReadEncodedValue().ToArray(), AsnEncodingRules.DER);
            var certReqInfoSeq = certReqInfoReader.ReadSequence();

            // version
            int version = (int)certReqInfoSeq.ReadInteger();
            Assert.AreEqual(0, version, "CSR version should be 0");

            // subject
            var subjectBytes = certReqInfoSeq.PeekEncodedValue().ToArray();
            var subject = new X500DistinguishedName(certReqInfoSeq.ReadEncodedValue().ToArray());
            string subjectString = subject.Name;

            Assert.IsTrue(subjectString.Contains($"CN={expectedClientId}"), "Client ID (CN) not found in subject");
            Assert.IsTrue(subjectString.Contains($"DC={expectedTenantId}"), "Tenant ID (DC) not found in subject");

            // subjectPKInfo
            var pkInfoReader = new AsnReader(certReqInfoSeq.ReadEncodedValue().ToArray(), AsnEncodingRules.DER);
            var pkInfoSeq = pkInfoReader.ReadSequence();

            // algorithm
            var algIdSeq = pkInfoSeq.ReadSequence();
            string algOid = algIdSeq.ReadObjectIdentifier();
            Assert.AreEqual("1.2.840.113549.1.1.1", algOid, "Public key algorithm is not RSA");
            if (algIdSeq.HasData)
            {
                algIdSeq.ReadNull();
            }

            // subjectPublicKey BIT STRING
            var publicKeyBitString = pkInfoSeq.ReadBitString(out _);

            // Parse the RSAPublicKey structure from the BIT STRING (SEQUENCE of modulus, exponent)
            var rsaKeyReader = new AsnReader(publicKeyBitString, AsnEncodingRules.DER);
            var rsaKeySeq = rsaKeyReader.ReadSequence();
            byte[] modulus = rsaKeySeq.ReadIntegerBytes().ToArray();
            byte[] exponent = rsaKeySeq.ReadIntegerBytes().ToArray();

            // Validate modulus length (2048 bits = 256 bytes, may have leading zero)
            Assert.IsTrue(modulus.Length == 256 || modulus.Length == 257, $"RSA modulus should be 2048 bits, got {modulus.Length * 8} bits");

            // Validate exponent (commonly 65537 = 0x010001)
            Assert.IsTrue(exponent.Length >= 1 && exponent.Length <= 4, "RSA exponent has invalid length");

            // attributes [0] (optional)
            if (certReqInfoSeq.HasData)
            {
                var attrTag = new Asn1Tag(TagClass.ContextSpecific, 0);
                if (certReqInfoSeq.PeekTag().HasSameClassAndValue(attrTag))
                {
                    var attrSetReader = certReqInfoSeq.ReadSetOf(attrTag);
                    bool foundCuid = false;
                    while (attrSetReader.HasData)
                    {
                        var attrSeq = attrSetReader.ReadSequence();
                        string oid = attrSeq.ReadObjectIdentifier();
                        if (oid == "1.3.6.1.4.1.311.90.2.10") // challengePassword
                        {
                            var valueSet = attrSeq.ReadSetOf();
                            while (valueSet.HasData)
                            {
                                string cuidJson = valueSet.ReadCharacterString(UniversalTagNumber.UTF8String);
                                string expectedCuidJson = JsonHelper.SerializeToJson(expectedCuid);
                                Assert.AreEqual(expectedCuidJson, cuidJson, "CUID attribute JSON value does not match expected");
                                foundCuid = true;
                            }
                        }
                    }
                    Assert.IsTrue(foundCuid, "CUID (challengePassword) attribute not found");
                }
            }

            // signatureAlgorithm
            var sigAlgSeq = csrSequence.ReadSequence();
            string sigAlgOid = sigAlgSeq.ReadObjectIdentifier();
            Assert.AreEqual("1.2.840.113549.1.1.10", sigAlgOid, "Signature algorithm is not RSASSA-PSS (SHA256withRSA/PSS)");

            // signature
            csrSequence.ReadBitString(out _);

            Assert.IsFalse(csrSequence.HasData, "Extra data found after CSR structure");
        }
    }
}
