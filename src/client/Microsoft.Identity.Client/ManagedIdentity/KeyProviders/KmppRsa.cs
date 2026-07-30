// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using static Microsoft.Identity.Client.ManagedIdentity.KeyProviders.KmppNativeMethods;

namespace Microsoft.Identity.Client.ManagedIdentity.KeyProviders
{
    /// <summary>
    /// An <see cref="RSA"/> implementation whose private key lives inside the Linux KMPP
    /// (KeyIso / OP-TEE / TrustZone) enclave and is <b>non-exportable</b>. This is the Linux
    /// equivalent of a Windows CredentialGuard / KeyGuard key.
    /// <para>
    /// Only public parameters (read from the enclave's self-signed certificate) can be exported.
    /// Signing (<see cref="SignHash"/>) is RSA-PSS / SHA-256 and is executed <b>inside</b> the enclave
    /// via <c>libkmpp.so</c>; the private key never crosses the process boundary. This matches MSAL's
    /// MSI v2 mTLS-PoP CSR profile (RSASSA-PSS, SHA-256, MGF1-SHA256, salt length = digest length).
    /// </para>
    /// </summary>
    internal sealed class KmppRsa : RSA
    {
        // Size of KEYISO_EVP_PKEY_SIGN (tbsLen[8] + saltLen[4] + sigmdType[4] + mgfmdType[4] +
        // getMaxLen[4] + sigLen[8], 8-byte aligned) that prefixes the digest in the serialized
        // sign input. See KeyIso_CLIENT_pkey_rsa_sign_serialization in keyisoclient.c.
        private const int SignHeaderSize = 32;

        private readonly string _keyId;
        private RSAParameters _publicParameters;
        private bool _publicLoaded;

        /// <summary>The opaque KMPP keyId identifying the sealed enclave key.</summary>
        internal string KeyId => _keyId;

        internal KmppRsa(string keyId)
        {
            _keyId = keyId ?? throw new ArgumentNullException(nameof(keyId));
            KeySizeValue = 2048;
        }

        private static byte[] NewCorrelationId()
        {
            byte[] correlationId = new byte[SizeOfCorrelationId];
            RandomNumberGenerator.Fill(correlationId);
            return correlationId;
        }

        private void EnsurePublic()
        {
            if (_publicLoaded)
            {
                return;
            }

            byte[] correlationId = NewCorrelationId();
            int rc = KeyIso_build_cert_chain_from_key_id(
                correlationId,
                KEYISO_KEY_FLAG_TZ_ISOLATION,
                _keyId,
                out int _,
                out int pemCertLength,
                out IntPtr pemCert);

            if (rc == 0 || pemCert == IntPtr.Zero)
            {
                throw new CryptographicException("KMPP: failed to read the enclave public certificate for the keyId.");
            }

            try
            {
                string pem = Marshal.PtrToStringAnsi(pemCert, pemCertLength);
                using X509Certificate2 cert = X509Certificate2.CreateFromPem(pem);
                using RSA publicKey = cert.GetRSAPublicKey()
                    ?? throw new CryptographicException("KMPP: enclave certificate does not contain an RSA public key.");

                _publicParameters = publicKey.ExportParameters(false);
                KeySizeValue = _publicParameters.Modulus!.Length * 8;
                _publicLoaded = true;
            }
            finally
            {
                KeyIso_free(pemCert);
            }
        }

        public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            if (includePrivateParameters)
            {
                throw new CryptographicException(
                    "KMPP enclave key is non-exportable; the private key never leaves the OP-TEE/TrustZone enclave.");
            }

            EnsurePublic();
            return _publicParameters;
        }

        public override void ImportParameters(RSAParameters parameters) =>
            throw new NotSupportedException("KMPP enclave key material cannot be imported.");

        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
            hashAlgorithm == HashAlgorithmName.SHA256
                ? SHA256.HashData(new ReadOnlySpan<byte>(data, offset, count))
                : throw new NotSupportedException("KMPP enclave supports SHA-256 only.");

        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
            hashAlgorithm == HashAlgorithmName.SHA256
                ? SHA256.HashData(data)
                : throw new NotSupportedException("KMPP enclave supports SHA-256 only.");

        public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (hash is null)
            {
                throw new ArgumentNullException(nameof(hash));
            }

            if (hashAlgorithm != HashAlgorithmName.SHA256)
            {
                throw new NotSupportedException("KMPP enclave supports SHA-256 only.");
            }

            if (padding != RSASignaturePadding.Pss)
            {
                throw new NotSupportedException(
                    "KMPP enclave supports RSA-PSS only (SymCrypt/FIPS enclave path).");
            }

            EnsurePublic();
            int signatureSize = KeySize / 8; // 256 for RSA-2048

            // Serialize: 32-byte header (salt/digest/MGF metadata) followed by the digest bytes.
            byte[] serializedInput = new byte[SignHeaderSize + hash.Length];
            KeyIso_CLIENT_pkey_rsa_sign_serialization(
                serializedInput,
                hash,
                (UIntPtr)hash.Length,
                RSA_PSS_SALTLEN_DIGEST,
                NID_sha256,
                NID_sha256,
                (UIntPtr)signatureSize,
                0);

            IntPtr keyContext = OpenKeyContext();
            try
            {
                byte[] signature = new byte[signatureSize];
                int rc = KeyIso_CLIENT_pkey_rsa_sign(
                    keyContext,
                    serializedInput.Length,
                    serializedInput,
                    signature.Length,
                    signature,
                    RSA_PSS_PADDING);

                if (rc <= 0)
                {
                    throw new CryptographicException($"KMPP enclave RSA-PSS sign failed (rc={rc}).");
                }

                // RSA-2048 PSS signatures are always 256 bytes; the enclave fills the full buffer.
                return signature;
            }
            finally
            {
                KeyIso_CLIENT_pfx_close(keyContext);
            }
        }

        public override bool VerifyHash(
            byte[] hash,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding)
        {
            EnsurePublic();
            using RSA publicKey = Create();
            publicKey.ImportParameters(_publicParameters);
            return publicKey.VerifyHash(hash, signature, hashAlgorithm, padding);
        }

        private IntPtr OpenKeyContext()
        {
            byte[] correlationId = NewCorrelationId();
            if (KeyIso_parse_pfx_engine_key_id(correlationId, _keyId, out int pfxLength, out IntPtr pfxBytes, out IntPtr clientData) == 0
                || pfxBytes == IntPtr.Zero)
            {
                throw new CryptographicException("KMPP: failed to parse the keyId into a PFX blob.");
            }

            try
            {
                // Keys created by LinuxManagedIdentityKeyProvider are modern P8 (PBES2) encrypted keys and
                // the KMPP service on current images is P8-capable, so both compatibility flags are true.
                // This routes to KeyIso_CLIENT_private_key_open_from_pfx (P8 path) rather than the legacy
                // salt-based path.
                if (KeyIso_open_key_by_compatibility(correlationId, out IntPtr keyContext, pfxBytes, pfxLength, clientData, keyIsP8: true, serviceIsP8: true) == 0
                    || keyContext == IntPtr.Zero)
                {
                    throw new CryptographicException("KMPP: failed to open the enclave key (KeyGuard/OP-TEE service).");
                }

                return keyContext;
            }
            finally
            {
                if (pfxBytes != IntPtr.Zero)
                {
                    KeyIso_clear_free(pfxBytes, pfxLength);
                }

                if (clientData != IntPtr.Zero)
                {
                    KeyIso_clear_free_string(clientData);
                }
            }
        }
    }
}
