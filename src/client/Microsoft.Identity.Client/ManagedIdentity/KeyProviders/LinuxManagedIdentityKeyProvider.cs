// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using static Microsoft.Identity.Client.ManagedIdentity.KeyProviders.KmppNativeMethods;

namespace Microsoft.Identity.Client.ManagedIdentity.KeyProviders
{
    /// <summary>
    /// Linux managed identity key provider. Creates a <b>non-exportable</b> RSA-PSS key inside the
    /// KMPP (KeyIso / OP-TEE / TrustZone) enclave — the Linux equivalent of a Windows CredentialGuard /
    /// KeyGuard key — and reports it as <see cref="ManagedIdentityKeyType.KeyGuard"/> so the MSI v2
    /// mTLS-PoP flow treats it as attestable. Falls back to an in-memory software RSA key when the
    /// KMPP enclave is unavailable (e.g. OP-TEE not initialized on the host).
    /// </summary>
    internal sealed class LinuxManagedIdentityKeyProvider : IManagedIdentityKeyProvider
    {
        private static readonly SemaphoreSlim s_once = new(1, 1);
        private volatile ManagedIdentityKeyInfo _cachedKey;

        // OpenSSL-style KMPP configuration for a non-exportable RSA-PSS 2048 signing key.
        // Parsed by KMPP's KeyIso_conf_load; every value is read from the "[self_sign]" section.
        // rsa_padding = 6 selects RSASSA-PSS, making the self-signed certificate RSA-PSS, and
        // sign_digest = sha256 selects SHA-256 — matching MSAL's MSI v2 mTLS-PoP CSR profile.
        // (Empirically validated against libkmpp on Azure Linux 3.) Can be overridden at runtime via
        // the MSAL_KMPP_KEYGEN_CONF environment variable.
        private const string DefaultRsaKeyConf =
            "[self_sign]\n" +
            "key_type = rsa\n" +
            "rsa_bits = 2048\n" +
            "rsa_exp = 65537\n" +
            "rsa_padding = 6\n" +
            "sign_digest = sha256\n" +
            "key_usage = digitalSignature\n" +
            "days = 365\n" +
            "distinguished_name = dn_sect\n" +
            "\n" +
            "[dn_sect]\n" +
            "CN = MSAL-ManagedIdentity-KMPP\n";

        public async Task<ManagedIdentityKeyInfo> GetOrCreateKeyAsync(ILoggerAdapter logger, CancellationToken ct)
        {
            if (_cachedKey is not null)
            {
                logger?.Info("[MI][LinuxKeyProvider] Returning cached key.");
                return _cachedKey;
            }

            logger?.Info(() => "[MI][LinuxKeyProvider] Waiting on creation semaphore.");
            await s_once.WaitAsync(ct).ConfigureAwait(false);

            try
            {
                if (_cachedKey is not null)
                {
                    return _cachedKey;
                }

                ct.ThrowIfCancellationRequested();

                // 1) Try the KMPP/OP-TEE enclave key (non-exportable, KeyGuard-equivalent).
                try
                {
                    logger?.Info(() => "[MI][LinuxKeyProvider] Trying KMPP/OP-TEE enclave key.");
                    if (TryCreateKmppKey(logger, out KmppRsa kmppRsa))
                    {
                        _cachedKey = new ManagedIdentityKeyInfo(
                            kmppRsa,
                            ManagedIdentityKeyType.KeyGuard,
                            "KMPP/OP-TEE enclave RSA-PSS key (non-exportable) created for Managed Identity.");
                        logger?.Info("[MI][LinuxKeyProvider] Using KMPP/OP-TEE enclave key (non-exportable).");
                        return _cachedKey;
                    }

                    logger?.Info(() => "[MI][LinuxKeyProvider] KMPP enclave key not available.");
                }
                catch (Exception ex)
                {
                    logger?.WarningPii(
                        $"[MI][LinuxKeyProvider] Exception creating KMPP key: {ex}",
                        $"[MI][LinuxKeyProvider] Exception creating KMPP key: {ex.GetType().Name}");
                }

                // 2) Fallback: in-memory software RSA.
                logger?.Info("[MI][LinuxKeyProvider] Falling back to in-memory RSA key (software).");
                ct.ThrowIfCancellationRequested();
                var fallback = new InMemoryManagedIdentityKeyProvider();
                _cachedKey = await fallback.GetOrCreateKeyAsync(logger, ct).ConfigureAwait(false);
                return _cachedKey;
            }
            finally
            {
                s_once.Release();
            }
        }

        private static bool TryCreateKmppKey(ILoggerAdapter logger, out KmppRsa kmppRsa)
        {
            kmppRsa = null;

            string conf = Environment.GetEnvironmentVariable("MSAL_KMPP_KEYGEN_CONF");
            if (string.IsNullOrEmpty(conf))
            {
                conf = DefaultRsaKeyConf;
            }

            byte[] correlationId = new byte[SizeOfCorrelationId];
            RandomNumberGenerator.Fill(correlationId);

            int rc = KeyIso_create_self_sign_pfx_to_key_id(
                correlationId,
                KEYISO_KEY_FLAG_TZ_ISOLATION,
                conf,
                out IntPtr keyIdPtr);

            if (rc == 0 || keyIdPtr == IntPtr.Zero)
            {
                logger?.Info(() => $"[MI][LinuxKeyProvider] KMPP keygen (KeyIso_create_self_sign_pfx_to_key_id) returned rc={rc}.");
                return false;
            }

            string keyId;
            try
            {
                keyId = Marshal.PtrToStringAnsi(keyIdPtr);
            }
            finally
            {
                KeyIso_clear_free_string(keyIdPtr);
            }

            if (string.IsNullOrEmpty(keyId))
            {
                logger?.Info(() => "[MI][LinuxKeyProvider] KMPP keygen returned an empty keyId.");
                return false;
            }

            var rsa = new KmppRsa(keyId);

            // Validate the key is usable by reading its public parameters from the enclave certificate.
            _ = rsa.ExportParameters(false);

            logger?.Info(() => $"[MI][LinuxKeyProvider] KMPP enclave key created ({rsa.KeySize}-bit, non-exportable).");
            kmppRsa = rsa;
            return true;
        }
    }
}
