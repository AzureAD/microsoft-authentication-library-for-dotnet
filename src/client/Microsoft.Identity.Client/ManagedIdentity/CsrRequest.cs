// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class Csr
    {
        public string Pem { get; }

        public Csr(string pem)
        {
            Pem = pem ?? throw new ArgumentNullException(nameof(pem));
        }

        /// <summary>
        /// Generates a CSR for the given client, tenant, and CUID info.
        /// </summary>
        /// <param name="clientId">Managed Identity client_id.</param>
        /// <param name="tenantId">AAD tenant_id.</param>
        /// <param name="cuid">CuidInfo object containing required VMID and optional VMSSID.</param>
        /// <returns>CsrRequest containing the PEM CSR.</returns>
        public static Csr Generate(string clientId, string tenantId, CuidInfo cuid)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("clientId must not be null or empty.", nameof(clientId));
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentException("tenantId must not be null or empty.", nameof(tenantId));
            if (cuid == null)
                throw new ArgumentNullException(nameof(cuid));
            if (string.IsNullOrEmpty(cuid.Vmid))
                throw new ArgumentException("cuid.Vmid must not be null or empty.", nameof(cuid.Vmid));

            string pemCsr = GeneratePkcs10Csr(clientId, tenantId, cuid);
            return new Csr(pemCsr);
        }

        /// <summary>
        /// Generates a PKCS#10 Certificate Signing Request in PEM format.
        /// </summary>
        private static string GeneratePkcs10Csr(string clientId, string tenantId, CuidInfo cuid)
        {
            // Generate RSA key pair (2048-bit)
            RSA rsa = CreateRsaKeyPair();

            try
            {
                // Build the CSR components
                byte[] certificationRequestInfo = BuildCertificationRequestInfo(clientId, tenantId, cuid, rsa);
                byte[] signatureAlgorithm = BuildSignatureAlgorithmIdentifier();
                byte[] signature = SignCertificationRequestInfo(certificationRequestInfo, rsa);

                // Combine into final CSR structure
                byte[] csrBytes = BuildFinalCsr(certificationRequestInfo, signatureAlgorithm, signature);

                // Convert to PEM format
                return ConvertToPem(csrBytes);
            }
            finally
            {
                rsa?.Dispose();
            }
        }

        /// <summary>
        /// Creates a 2048-bit RSA key pair compatible with all target frameworks.
        /// </summary>
        private static RSA CreateRsaKeyPair()
        {
#if NET462 || NET472
            var rsa = new RSACryptoServiceProvider(2048);
            return rsa;
#else
            var rsa = RSA.Create();
            rsa.KeySize = 2048;
            return rsa;
#endif
        }

        /// <summary>
        /// Builds the CertificationRequestInfo structure containing subject, public key, and attributes.
        /// </summary>
        private static byte[] BuildCertificationRequestInfo(string clientId, string tenantId, CuidInfo cuid, RSA rsa)
        {
            var components = new System.Collections.Generic.List<byte[]>();

            // Version (INTEGER 0)
            components.Add(EncodeAsn1Integer(new byte[] { 0x00 }));

            // Subject: CN=<clientId>, DC=<tenantId>
            components.Add(BuildSubjectName(clientId, tenantId));

            // SubjectPublicKeyInfo
            components.Add(BuildSubjectPublicKeyInfo(rsa));

            // Attributes (including CUID)
            components.Add(BuildAttributes(cuid));

            return EncodeAsn1Sequence(components.ToArray());
        }

        /// <summary>
        /// Builds the X.500 Distinguished Name for the subject.
        /// </summary>
        private static byte[] BuildSubjectName(string clientId, string tenantId)
        {
            var rdnSequence = new System.Collections.Generic.List<byte[]>();

            // CN=<clientId>
            byte[] cnOid = EncodeAsn1ObjectIdentifier(new int[] { 2, 5, 4, 3 }); // commonName OID
            byte[] cnValue = EncodeAsn1Utf8String(clientId);
            byte[] cnAttributeValue = EncodeAsn1Sequence(new[] { cnOid, cnValue });
            rdnSequence.Add(EncodeAsn1Set(new[] { cnAttributeValue }));

            // DC=<tenantId>
            byte[] dcOid = EncodeAsn1ObjectIdentifier(new int[] { 0, 9, 2342, 19200300, 100, 1, 25 }); // domainComponent OID
            byte[] dcValue = EncodeAsn1Utf8String(tenantId);
            byte[] dcAttributeValue = EncodeAsn1Sequence(new[] { dcOid, dcValue });
            rdnSequence.Add(EncodeAsn1Set(new[] { dcAttributeValue }));

            return EncodeAsn1Sequence(rdnSequence.ToArray());
        }

        /// <summary>
        /// Builds the SubjectPublicKeyInfo structure containing the RSA public key.
        /// </summary>
        private static byte[] BuildSubjectPublicKeyInfo(RSA rsa)
        {
            RSAParameters rsaParams = rsa.ExportParameters(false);

            // RSA Public Key structure
            byte[] modulus = EncodeAsn1Integer(rsaParams.Modulus);
            byte[] exponent = EncodeAsn1Integer(rsaParams.Exponent);
            byte[] rsaPublicKey = EncodeAsn1Sequence(new[] { modulus, exponent });

            // Algorithm identifier for RSA encryption
            byte[] rsaOid = EncodeAsn1ObjectIdentifier(new int[] { 1, 2, 840, 113549, 1, 1, 1 }); // RSA encryption OID
            byte[] nullParam = EncodeAsn1Null();
            byte[] algorithmIdentifier = EncodeAsn1Sequence(new[] { rsaOid, nullParam });

            // SubjectPublicKeyInfo
            byte[] publicKeyBitString = EncodeAsn1BitString(rsaPublicKey);
            return EncodeAsn1Sequence(new[] { algorithmIdentifier, publicKeyBitString });
        }

        /// <summary>
        /// Builds the attributes section including the CUID extension.
        /// </summary>
        private static byte[] BuildAttributes(CuidInfo cuid)
        {
            var attributes = new System.Collections.Generic.List<byte[]>();

            // CUID attribute (OID 1.2.840.113549.1.9.7)
            // Serialize CuidInfo as JSON object string using existing JSON serialization
            byte[] cuidOid = EncodeAsn1ObjectIdentifier(new int[] { 1, 2, 840, 113549, 1, 9, 7 });
            string cuidValue = JsonHelper.SerializeToJson(cuid);
            byte[] cuidData = EncodeAsn1PrintableString(cuidValue);
            byte[] cuidAttributeValue = EncodeAsn1Set(new[] { cuidData });
            byte[] cuidAttribute = EncodeAsn1Sequence(new[] { cuidOid, cuidAttributeValue });
            attributes.Add(cuidAttribute);

            return EncodeAsn1ContextSpecific(0, EncodeAsn1SequenceRaw(attributes.ToArray()));
        }

        /// <summary>
        /// Builds the signature algorithm identifier for SHA256withRSA.
        /// </summary>
        private static byte[] BuildSignatureAlgorithmIdentifier()
        {
            byte[] sha256WithRsaOid = EncodeAsn1ObjectIdentifier(new int[] { 1, 2, 840, 113549, 1, 1, 11 }); // SHA256withRSA OID
            byte[] nullParam = EncodeAsn1Null();
            return EncodeAsn1Sequence(new[] { sha256WithRsaOid, nullParam });
        }

        /// <summary>
        /// Signs the CertificationRequestInfo with SHA256withRSA.
        /// </summary>
        private static byte[] SignCertificationRequestInfo(byte[] certificationRequestInfo, RSA rsa)
        {
#if NET462 || NET472
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(certificationRequestInfo);
                var formatter = new RSAPKCS1SignatureFormatter(rsa);
                formatter.SetHashAlgorithm("SHA256");
                return formatter.CreateSignature(hash);
            }
#else
            return rsa.SignData(certificationRequestInfo, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
#endif
        }

        /// <summary>
        /// Combines all components into the final CSR structure.
        /// </summary>
        private static byte[] BuildFinalCsr(byte[] certificationRequestInfo, byte[] signatureAlgorithm, byte[] signature)
        {
            byte[] signatureBitString = EncodeAsn1BitString(signature);
            return EncodeAsn1Sequence(new[] { certificationRequestInfo, signatureAlgorithm, signatureBitString });
        }

        /// <summary>
        /// Converts DER-encoded bytes to PEM format.
        /// </summary>
        private static string ConvertToPem(byte[] derBytes)
        {
            string base64 = Convert.ToBase64String(derBytes);
            var sb = new StringBuilder();
            sb.AppendLine("-----BEGIN CERTIFICATE REQUEST-----");

            // Split into 64-character lines
            for (int i = 0; i < base64.Length; i += 64)
            {
                int length = Math.Min(64, base64.Length - i);
                sb.AppendLine(base64.Substring(i, length));
            }

            sb.AppendLine("-----END CERTIFICATE REQUEST-----");
            return sb.ToString();
        }

        #region ASN.1 Encoding Helpers

        /// <summary>
        /// Encodes an ASN.1 SEQUENCE.
        /// </summary>
        private static byte[] EncodeAsn1Sequence(byte[][] components)
        {
            return EncodeAsn1Tag(0x30, ConcatenateByteArrays(components));
        }

        /// <summary>
        /// Encodes an ASN.1 SEQUENCE without the outer tag (for raw concatenation).
        /// </summary>
        private static byte[] EncodeAsn1SequenceRaw(byte[][] components)
        {
            return ConcatenateByteArrays(components);
        }

        /// <summary>
        /// Encodes an ASN.1 SET.
        /// </summary>
        private static byte[] EncodeAsn1Set(byte[][] components)
        {
            return EncodeAsn1Tag(0x31, ConcatenateByteArrays(components));
        }

        /// <summary>
        /// Encodes an ASN.1 INTEGER.
        /// </summary>
        private static byte[] EncodeAsn1Integer(byte[] value)
        {
            // Ensure positive integer (prepend 0x00 if high bit is set)
            if (value != null && value.Length > 0 && (value[0] & 0x80) != 0)
            {
                byte[] paddedValue = new byte[value.Length + 1];
                paddedValue[0] = 0x00;
                Array.Copy(value, 0, paddedValue, 1, value.Length);
                value = paddedValue;
            }
            return EncodeAsn1Tag(0x02, value ?? new byte[0]);
        }

        /// <summary>
        /// Encodes an ASN.1 INTEGER from an integer value.
        /// </summary>
        private static byte[] EncodeAsn1Integer(int value)
        {
            if (value == 0)
                return EncodeAsn1Tag(0x02, new byte[] { 0x00 });

            var bytes = new System.Collections.Generic.List<byte>();
            int temp = value;
            while (temp > 0)
            {
                bytes.Insert(0, (byte)(temp & 0xFF));
                temp >>= 8;
            }

            return EncodeAsn1Integer(bytes.ToArray());
        }

        /// <summary>
        /// Encodes an ASN.1 BIT STRING.
        /// </summary>
        private static byte[] EncodeAsn1BitString(byte[] value)
        {
            byte[] bitStringValue = new byte[value.Length + 1];
            bitStringValue[0] = 0x00; // No unused bits
            Array.Copy(value, 0, bitStringValue, 1, value.Length);
            return EncodeAsn1Tag(0x03, bitStringValue);
        }

        /// <summary>
        /// Encodes an ASN.1 UTF8String.
        /// </summary>
        private static byte[] EncodeAsn1Utf8String(string value)
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(value);
            return EncodeAsn1Tag(0x0C, utf8Bytes);
        }

        /// <summary>
        /// Encodes an ASN.1 PrintableString.
        /// </summary>
        private static byte[] EncodeAsn1PrintableString(string value)
        {
            byte[] asciiBytes = Encoding.ASCII.GetBytes(value);
            return EncodeAsn1Tag(0x13, asciiBytes);
        }

        /// <summary>
        /// Encodes an ASN.1 NULL.
        /// </summary>
        private static byte[] EncodeAsn1Null()
        {
            return new byte[] { 0x05, 0x00 };
        }

        /// <summary>
        /// Encodes an ASN.1 OBJECT IDENTIFIER.
        /// </summary>
        private static byte[] EncodeAsn1ObjectIdentifier(int[] oid)
        {
            if (oid == null || oid.Length < 2)
                throw new ArgumentException("OID must have at least 2 components");

            var bytes = new System.Collections.Generic.List<byte>();

            // First two components are encoded as (first * 40 + second)
            bytes.AddRange(EncodeOidComponent(oid[0] * 40 + oid[1]));

            // Remaining components
            for (int i = 2; i < oid.Length; i++)
            {
                bytes.AddRange(EncodeOidComponent(oid[i]));
            }

            return EncodeAsn1Tag(0x06, bytes.ToArray());
        }

        /// <summary>
        /// Encodes an ASN.1 context-specific tag.
        /// </summary>
        private static byte[] EncodeAsn1ContextSpecific(int tagNumber, byte[] content)
        {
            byte tag = (byte)(0xA0 | tagNumber); // Context-specific, constructed
            return EncodeAsn1Tag(tag, content);
        }

        /// <summary>
        /// Encodes an ASN.1 tag with length and content.
        /// </summary>
        private static byte[] EncodeAsn1Tag(byte tag, byte[] content)
        {
            byte[] lengthBytes = EncodeAsn1Length(content.Length);
            byte[] result = new byte[1 + lengthBytes.Length + content.Length];
            result[0] = tag;
            Array.Copy(lengthBytes, 0, result, 1, lengthBytes.Length);
            Array.Copy(content, 0, result, 1 + lengthBytes.Length, content.Length);
            return result;
        }

        /// <summary>
        /// Encodes ASN.1 length field.
        /// </summary>
        private static byte[] EncodeAsn1Length(int length)
        {
            if (length < 0x80)
            {
                return new byte[] { (byte)length };
            }

            var lengthBytes = new System.Collections.Generic.List<byte>();
            int temp = length;
            while (temp > 0)
            {
                lengthBytes.Insert(0, (byte)(temp & 0xFF));
                temp >>= 8;
            }

            byte[] result = new byte[lengthBytes.Count + 1];
            result[0] = (byte)(0x80 | lengthBytes.Count);
            lengthBytes.CopyTo(result, 1);
            return result;
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

        /// <summary>
        /// Concatenates multiple byte arrays.
        /// </summary>
        private static byte[] ConcatenateByteArrays(byte[][] arrays)
        {
            int totalLength = 0;
            foreach (byte[] array in arrays)
            {
                totalLength += array.Length;
            }

            byte[] result = new byte[totalLength];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Array.Copy(array, 0, result, offset, array.Length);
                offset += array.Length;
            }

            return result;
        }

        #endregion
    }
}
