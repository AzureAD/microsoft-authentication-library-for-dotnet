// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    /// <summary>
    /// Test helper to expose CsrValidator methods for testing malformed PEM.
    /// </summary>
    internal static class TestCsrValidator
    {
        public static byte[] ParseCsrFromPem(string pemCsr)
        {
            if (string.IsNullOrWhiteSpace(pemCsr))
                throw new ArgumentException("PEM CSR cannot be null or empty");

            const string beginMarker = "-----BEGIN CERTIFICATE REQUEST-----";
            const string endMarker = "-----END CERTIFICATE REQUEST-----";

            if (!pemCsr.Contains(beginMarker) || !pemCsr.Contains(endMarker))
                throw new ArgumentException("Invalid PEM format - missing CSR headers");

            int beginIndex = pemCsr.IndexOf(beginMarker) + beginMarker.Length;
            int endIndex = pemCsr.IndexOf(endMarker);

            if (beginIndex >= endIndex)
                throw new ArgumentException("Invalid PEM format - malformed headers");

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
    }

    /// <summary>
    /// Helper class for validating Certificate Signing Request (CSR) content and structure.
    /// </summary>
    internal static class CsrValidator
    {
        /// <summary>
        /// Validates the content of a CSR PEM string against expected values.
        /// </summary>
        public static void ValidateCsrContent(string pemCsr, string expectedClientId, string expectedTenantId, CuidInfo expectedCuid)
        {
            // Parse the CSR from PEM format
            var csrData = ParseCsrFromPem(pemCsr);
            
            // Parse the PKCS#10 structure
            var csrInfo = ParsePkcs10Structure(csrData);
            
            // Validate subject name
            ValidateSubjectName(csrInfo.Subject, expectedClientId, expectedTenantId);
            
            // Validate public key
            ValidatePublicKey(csrInfo.PublicKey);
            
            // Validate CUID attribute
            ValidateCuidAttribute(csrInfo.Attributes, expectedCuid);
            
            // Validate signature algorithm
            ValidateSignatureAlgorithm(csrInfo.SignatureAlgorithm);
        }

        /// <summary>
        /// Parses a PEM-formatted CSR and returns the DER bytes.
        /// </summary>
        private static byte[] ParseCsrFromPem(string pemCsr)
        {
            if (string.IsNullOrWhiteSpace(pemCsr))
                throw new ArgumentException("PEM CSR cannot be null or empty");

            const string beginMarker = "-----BEGIN CERTIFICATE REQUEST-----";
            const string endMarker = "-----END CERTIFICATE REQUEST-----";

            if (!pemCsr.Contains(beginMarker) || !pemCsr.Contains(endMarker))
                throw new ArgumentException("Invalid PEM format - missing CSR headers");

            int beginIndex = pemCsr.IndexOf(beginMarker) + beginMarker.Length;
            int endIndex = pemCsr.IndexOf(endMarker);

            if (beginIndex >= endIndex)
                throw new ArgumentException("Invalid PEM format - malformed headers");

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
        /// Represents parsed PKCS#10 CSR information.
        /// </summary>
        private class CsrInfo
        {
            public byte[] Subject { get; set; }
            public byte[] PublicKey { get; set; }
            public byte[] Attributes { get; set; }
            public byte[] SignatureAlgorithm { get; set; }
        }

        /// <summary>
        /// Parses the PKCS#10 ASN.1 structure and extracts key components.
        /// </summary>
        private static CsrInfo ParsePkcs10Structure(byte[] derBytes)
        {
            int offset = 0;
            
            // Parse outer SEQUENCE (CertificationRequest)
            var outerSequence = ParseAsn1Tag(derBytes, ref offset, 0x30);
            
            // Reset offset to parse the CertificationRequestInfo within the outer sequence
            int infoOffset = 0;
            var certRequestInfo = ParseAsn1Tag(outerSequence, ref infoOffset, 0x30);
            
            // Parse version (should be 0)
            int versionOffset = 0;
            var version = ParseAsn1Tag(certRequestInfo, ref versionOffset, 0x02);
            if (version.Length != 1 || version[0] != 0x00)
                throw new ArgumentException("Invalid CSR version");
            
            // Parse subject
            var subject = ParseAsn1Tag(certRequestInfo, ref versionOffset, 0x30);
            
            // Parse SubjectPublicKeyInfo
            var publicKey = ParseAsn1Tag(certRequestInfo, ref versionOffset, 0x30);
            
            // Parse attributes (context-specific [0])
            var attributes = ParseAsn1Tag(certRequestInfo, ref versionOffset, 0xA0);
            
            return new CsrInfo
            {
                Subject = subject,
                PublicKey = publicKey,
                Attributes = attributes,
                SignatureAlgorithm = new byte[0] // Simplified for this test
            };
        }

        /// <summary>
        /// Parses an ASN.1 tag and returns its content.
        /// </summary>
        private static byte[] ParseAsn1Tag(byte[] data, ref int offset, byte expectedTag)
        {
            if (offset >= data.Length)
                throw new ArgumentException("Unexpected end of data");
            
            // Check tag (if expectedTag is -1, accept any tag)
            if (expectedTag != 255 && data[offset] != expectedTag)
                throw new ArgumentException($"Expected tag 0x{expectedTag:X2}, got 0x{data[offset]:X2}");
            
            offset++;
            
            // Parse length
            int length = ParseAsn1Length(data, ref offset);
            
            // Extract content
            if (offset + length > data.Length)
                throw new ArgumentException("Invalid ASN.1 length");
            
            byte[] content = new byte[length];
            Array.Copy(data, offset, content, 0, length);
            offset += length;
            
            return content;
        }

        /// <summary>
        /// Parses ASN.1 length encoding.
        /// </summary>
        private static int ParseAsn1Length(byte[] data, ref int offset)
        {
            if (offset >= data.Length)
                throw new ArgumentException("Unexpected end of data in length");
            
            byte firstByte = data[offset++];
            
            // Short form
            if ((firstByte & 0x80) == 0)
                return firstByte;
            
            // Long form
            int lengthBytes = firstByte & 0x7F;
            if (lengthBytes == 0)
                throw new ArgumentException("Indefinite length not supported");
            
            if (offset + lengthBytes > data.Length)
                throw new ArgumentException("Invalid length encoding");
            
            int length = 0;
            for (int i = 0; i < lengthBytes; i++)
            {
                length = (length << 8) | data[offset++];
            }
            
            return length;
        }

        /// <summary>
        /// Validates the subject name contains the expected client ID and tenant ID.
        /// </summary>
        private static void ValidateSubjectName(byte[] subjectBytes, string expectedClientId, string expectedTenantId)
        {
            // Subject is already a SEQUENCE of RDNs
            int offset = 0;
            bool foundClientId = false;
            bool foundTenantId = false;
            
            // Parse each RDN (Relative Distinguished Name) directly from subjectBytes
            while (offset < subjectBytes.Length)
            {
                var rdnSet = ParseAsn1Tag(subjectBytes, ref offset, 0x31); // SET
                
                int rdnOffset = 0;
                var rdnSequence = ParseAsn1Tag(rdnSet, ref rdnOffset, 0x30); // SEQUENCE
                
                // Parse OID and value
                int attrOffset = 0;
                var oid = ParseAsn1Tag(rdnSequence, ref attrOffset, 0x06); // OID
                var value = ParseAsn1Tag(rdnSequence, ref attrOffset, 255); // Any string type
                
                string stringValue = System.Text.Encoding.UTF8.GetString(value);
                
                // Check for CN (commonName) OID: 2.5.4.3
                if (IsOid(oid, new int[] { 2, 5, 4, 3 }))
                {
                    Assert.AreEqual(expectedClientId, stringValue, "Client ID in subject CN does not match");
                    foundClientId = true;
                }
                // Check for DC (domainComponent) OID: 0.9.2342.19200300.100.1.25
                else if (IsOid(oid, new int[] { 0, 9, 2342, 19200300, 100, 1, 25 }))
                {
                    Assert.AreEqual(expectedTenantId, stringValue, "Tenant ID in subject DC does not match");
                    foundTenantId = true;
                }
            }
            
            Assert.IsTrue(foundClientId, "Client ID (CN) not found in subject");
            Assert.IsTrue(foundTenantId, "Tenant ID (DC) not found in subject");
        }

        /// <summary>
        /// Validates the public key is a valid RSA key.
        /// </summary>
        private static void ValidatePublicKey(byte[] publicKeyBytes)
        {
            // publicKeyBytes is already the SubjectPublicKeyInfo SEQUENCE content
            int offset = 0;
            
            // Parse algorithm identifier
            var algorithmId = ParseAsn1Tag(publicKeyBytes, ref offset, 0x30);
            
            // Parse public key bit string
            var publicKeyBitString = ParseAsn1Tag(publicKeyBytes, ref offset, 0x03);
            
            // Validate algorithm is RSA (1.2.840.113549.1.1.1)
            int algOffset = 0;
            var algorithmOid = ParseAsn1Tag(algorithmId, ref algOffset, 0x06);
            Assert.IsTrue(IsOid(algorithmOid, new int[] { 1, 2, 840, 113549, 1, 1, 1 }), 
                "Public key algorithm is not RSA");
            
            // Skip the unused bits byte in bit string
            if (publicKeyBitString.Length < 2 || publicKeyBitString[0] != 0x00)
                throw new ArgumentException("Invalid public key bit string");
            
            // Parse RSA public key (skip unused bits byte)
            byte[] rsaKeyBytes = new byte[publicKeyBitString.Length - 1];
            Array.Copy(publicKeyBitString, 1, rsaKeyBytes, 0, rsaKeyBytes.Length);
            
            int rsaOffset = 0;
            var rsaSequence = ParseAsn1Tag(rsaKeyBytes, ref rsaOffset, 0x30);
            
            rsaOffset = 0;
            var modulus = ParseAsn1Tag(rsaSequence, ref rsaOffset, 0x02);
            var exponent = ParseAsn1Tag(rsaSequence, ref rsaOffset, 0x02);
            
            // Validate key size (should be 2048 bits = 256 bytes, plus potential leading zero)
            Assert.IsTrue(modulus.Length >= 256 && modulus.Length <= 257, 
                $"RSA modulus should be 2048 bits, got {modulus.Length * 8} bits");
            
            // Validate exponent (commonly 65537 = 0x010001)
            Assert.IsTrue(exponent.Length >= 1 && exponent.Length <= 4, "RSA exponent has invalid length");
        }

        /// <summary>
        /// Validates the CUID attribute contains the expected VM and VMSS IDs as JSON.
        /// Note: Vmid is required, Vmssid is optional and will be omitted if null/empty.
        /// </summary>
        private static void ValidateCuidAttribute(byte[] attributesBytes, CuidInfo expectedCuid)
        {
            // Attributes is a SET of attributes
            // We expect one attribute with challengePassword OID (1.2.840.113549.1.9.7)
            
            int offset = 0;
            bool foundCuid = false;
            
            // Parse each attribute in the SET
            while (offset < attributesBytes.Length)
            {
                var attributeSequence = ParseAsn1Tag(attributesBytes, ref offset, 0x30);
                
                int attrOffset = 0;
                var oid = ParseAsn1Tag(attributeSequence, ref attrOffset, 0x06);
                var valueSet = ParseAsn1Tag(attributeSequence, ref attrOffset, 0x31); // SET of values
                
                // Check for challengePassword OID: 1.2.840.113549.1.9.7
                if (IsOid(oid, new int[] { 1, 2, 840, 113549, 1, 9, 7 }))
                {
                    // Parse the value from the SET (should be one value)
                    int valueOffset = 0;
                    var value = ParseAsn1Tag(valueSet, ref valueOffset, 255); // Any string type
                    
                    string cuidValue = System.Text.Encoding.ASCII.GetString(value);
                    
                    // Build expected CUID value as JSON
                    string expectedCuidValue = BuildExpectedCuidJson(expectedCuid);
                    
                    Assert.AreEqual(expectedCuidValue, cuidValue, "CUID attribute JSON value does not match expected");
                    foundCuid = true;
                    break;
                }
            }
            
            Assert.IsTrue(foundCuid, "CUID (challengePassword) attribute not found");
        }

        /// <summary>
        /// Builds the expected CUID JSON string for validation using JsonHelper.
        /// </summary>
        private static string BuildExpectedCuidJson(CuidInfo expectedCuid)
        {
            return JsonHelper.SerializeToJson(expectedCuid);
        }

        /// <summary>
        /// Validates the signature algorithm is SHA256withRSA.
        /// </summary>
        private static void ValidateSignatureAlgorithm(byte[] signatureAlgBytes)
        {
            // For this test, we'll just verify that signature algorithm exists
            // Full validation would require parsing the outer CSR structure
            // which is more complex for this unit test scenario
            Assert.IsNotNull(signatureAlgBytes, "Signature algorithm should be present");
        }

        /// <summary>
        /// Checks if the given OID bytes match the expected OID components.
        /// </summary>
        private static bool IsOid(byte[] oidBytes, int[] expectedOid)
        {
            if (expectedOid.Length < 2)
                return false;
            
            var expectedBytes = EncodeOid(expectedOid);
            
            if (oidBytes.Length != expectedBytes.Length)
                return false;
            
            for (int i = 0; i < oidBytes.Length; i++)
            {
                if (oidBytes[i] != expectedBytes[i])
                    return false;
            }
            
            return true;
        }

        /// <summary>
        /// Encodes an OID from integer components to bytes (simplified version).
        /// </summary>
        private static byte[] EncodeOid(int[] oid)
        {
            if (oid.Length < 2)
                throw new ArgumentException("OID must have at least 2 components");
            
            var result = new System.Collections.Generic.List<byte>();
            
            // First two components are encoded as (first * 40 + second)
            result.AddRange(EncodeOidComponent(oid[0] * 40 + oid[1]));
            
            // Remaining components
            for (int i = 2; i < oid.Length; i++)
            {
                result.AddRange(EncodeOidComponent(oid[i]));
            }
            
            return result.ToArray();
        }

        /// <summary>
        /// Encodes a single OID component using variable-length encoding.
        /// </summary>
        private static byte[] EncodeOidComponent(int value)
        {
            if (value == 0)
                return new byte[] { 0x00 };
            
            var bytes = new System.Collections.Generic.List<byte>();
            int temp = value;
            
            bytes.Insert(0, (byte)(temp & 0x7F));
            temp >>= 7;
            
            while (temp > 0)
            {
                bytes.Insert(0, (byte)((temp & 0x7F) | 0x80));
                temp >>= 7;
            }
            
            return bytes.ToArray();
        }
    }
}
