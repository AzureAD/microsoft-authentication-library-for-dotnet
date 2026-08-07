// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.ManagedIdentity.KeyProviders
{
    /// <summary>
    /// P/Invoke declarations for the Linux KMPP (Key Management and Protection Platform) client
    /// library <c>libkmpp.so</c> (a.k.a. KeyIso).
    /// <para>
    /// These are used to create and use a <b>non-exportable</b> RSA-PSS key that is isolated in the
    /// OP-TEE / TrustZone enclave ("KeyGuard" equivalent) on Azure Linux VMs. The private key material
    /// never leaves the enclave; only the public certificate and in-enclave signatures are returned.
    /// </para>
    /// <para>
    /// Signatures mirror <c>/usr/include/keyisoclient.h</c> and <c>/usr/include/keyisopfxclient.h</c>.
    /// All functions return <c>1</c> for success and <c>0</c> for error unless noted otherwise, and any
    /// returned buffer must be released with <see cref="KeyIso_free"/> / <see cref="KeyIso_clear_free"/> /
    /// <see cref="KeyIso_clear_free_string"/>.
    /// </para>
    /// </summary>
    internal static class KmppNativeMethods
    {
        // libkmpp.so (SONAME libkmpp.so.1). The runtime resolves "kmpp" -> libkmpp.so on Linux.
        internal const string KmppLib = "kmpp";

        // keyisoFlags: TrustZone / OP-TEE isolation (non-exportable, KeyGuard-equivalent).
        // Matches kmpptest's "-f0x10000".
        internal const int KEYISO_KEY_FLAG_TZ_ISOLATION = 0x10000;

        // RSA-PSS padding id (6) passed to KeyIso_CLIENT_pkey_rsa_sign. Id 6 selects RSASSA-PSS - the
        // only RSA signature scheme wired into the SymCrypt-only enclave path, and the only
        // CryptoBoard-approved RSA padding. PSS is the sole padding this provider ever uses.
        internal const int RSA_PSS_PADDING = 6;

        // NID_sha256.
        internal const int NID_sha256 = 672;

        // RSA_PSS_SALTLEN_DIGEST (-1): salt length equals the digest length. Matches MSAL's CSR
        // AlgorithmIdentifier (RSASSA-PSS, SHA-256, MGF1-SHA256, salt = digest length).
        internal const int RSA_PSS_SALTLEN_DIGEST = -1;

        internal const int SizeOfCorrelationId = 16; // uuid_t

        /// <summary>
        /// Generates a new key inside the enclave from an OpenSSL-style configuration string and returns
        /// its opaque KMPP <c>keyId</c>. The key material is sealed in the enclave and is non-exportable.
        /// </summary>
        /// <returns>1 on success, 0 on error.</returns>
        [DllImport(KmppLib, CharSet = CharSet.Ansi)]
        internal static extern int KeyIso_create_self_sign_pfx_to_key_id(
            byte[] correlationId,
            int keyisoFlags,
            string confStr,
            out IntPtr keyId); // KeyIso_clear_free_string()

        /// <summary>
        /// Parses a KMPP <c>keyId</c> string into the encrypted PFX bytes and (base64) client data.
        /// </summary>
        /// <returns>1 on success, 0 on error.</returns>
        [DllImport(KmppLib, CharSet = CharSet.Ansi)]
        internal static extern int KeyIso_parse_pfx_engine_key_id(
            byte[] correlationId,
            string keyId,
            out int pfxLength,
            out IntPtr pfxBytes,     // KeyIso_clear_free(pfxBytes, pfxLength)
            out IntPtr clientData);  // KeyIso_clear_free_string()

        /// <summary>Returns <c>true</c> when the key blob uses the PBES2 ("P8") format.</summary>
        [DllImport(KmppLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool KeyIso_is_oid_pbe2(
            byte[] correlationId,
            IntPtr keyBytes,
            int keyLength);

        /// <summary>
        /// Opens the enclave private key (routing the request to the KMPP / KeyGuard service) and returns
        /// an opaque key context used for signing.
        /// </summary>
        /// <returns>1 on success, 0 on error.</returns>
        [DllImport(KmppLib)]
        internal static extern int KeyIso_open_key_by_compatibility(
            byte[] correlationId,
            out IntPtr keyContext,
            IntPtr pfxBytes,
            int pfxLength,
            IntPtr clientData,
            [MarshalAs(UnmanagedType.I1)] bool keyIsP8,
            [MarshalAs(UnmanagedType.I1)] bool serviceIsP8);

        /// <summary>
        /// Serializes a digest into the buffer layout the enclave RSA-PSS sign expects
        /// (fixed-size header describing salt length / digest / MGF, followed by the digest bytes).
        /// </summary>
        [DllImport(KmppLib)]
        internal static extern void KeyIso_CLIENT_pkey_rsa_sign_serialization(
            byte[] serializedInput,
            byte[] toBeSigned,
            UIntPtr toBeSignedLength,
            int saltLength,
            int mdType,
            int mgfType,
            UIntPtr signatureLength,
            int getMaxLength);

        /// <summary>Performs the RSA sign operation inside the enclave.</summary>
        /// <returns>The signature length in bytes, or a negative value on error.</returns>
        [DllImport(KmppLib)]
        internal static extern int KeyIso_CLIENT_pkey_rsa_sign(
            IntPtr keyContext,
            int fromLength,
            byte[] from,
            int toLength,
            byte[] to,
            int padding);

        /// <summary>Releases the enclave key context created by <see cref="KeyIso_open_key_by_compatibility"/>.</summary>
        [DllImport(KmppLib)]
        internal static extern void KeyIso_CLIENT_pfx_close(IntPtr keyContext);

        /// <summary>
        /// Returns the public certificate chain (PEM) for a <c>keyId</c>. The end-entity certificate's
        /// public key is used to expose <see cref="System.Security.Cryptography.RSAParameters"/> without
        /// ever touching the private key.
        /// </summary>
        /// <returns>+1 complete chain, -1 chain error (still usable), 0 error.</returns>
        [DllImport(KmppLib, CharSet = CharSet.Ansi)]
        internal static extern int KeyIso_build_cert_chain_from_key_id(
            byte[] correlationId,
            int keyisoFlags,
            string keyId,
            out int verifyChainError,
            out int pemCertLength,
            out IntPtr pemCert); // KeyIso_free()

        [DllImport(KmppLib)]
        internal static extern void KeyIso_free(IntPtr ptr);

        [DllImport(KmppLib)]
        internal static extern void KeyIso_clear_free(IntPtr ptr, int length);

        [DllImport(KmppLib)]
        internal static extern void KeyIso_clear_free_string(IntPtr ptr);
    }
}
