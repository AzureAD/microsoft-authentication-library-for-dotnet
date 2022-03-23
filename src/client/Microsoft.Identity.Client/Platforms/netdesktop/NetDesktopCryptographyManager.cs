// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Text;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Client.Platforms.net45.Native;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Utils;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.Platforms.net45
{
    internal class NetDesktopCryptographyManager : ICryptographyManager
    {
        private static readonly ConcurrentDictionary<string, RSACryptoServiceProvider> s_certificateToRsaCspMap = new ConcurrentDictionary<string, RSACryptoServiceProvider>();
        private static readonly int s_maximumMapSize = 1000;

        public string CreateBase64UrlEncodedSha256Hash(string input)
        {
            return string.IsNullOrEmpty(input) ? null : Base64UrlHelpers.Encode(CreateSha256HashBytes(input));
        }

        public string GenerateCodeVerifier()
        {
            byte[] buffer = new byte[Constants.CodeVerifierByteSize];
            using (var randomSource = RandomNumberGenerator.Create())
            {
                randomSource.GetBytes(buffer);
            }

            return Base64UrlHelpers.Encode(buffer);
        }

        public string CreateSha256Hash(string input)
        {
            return string.IsNullOrEmpty(input) ? null : Convert.ToBase64String(CreateSha256HashBytes(input));
        }

        public byte[] CreateSha256HashBytes(string input)
        {
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }
      
        public byte[] SignWithCertificate(string message, X509Certificate2 certificate)
        {
            if (certificate.PublicKey.Key.KeySize < CertificateClientCredential.MinKeySizeInBits)
            {
                throw new ArgumentOutOfRangeException(nameof(certificate),
                    string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.CertificateKeySizeTooSmallTemplate,
                        CertificateClientCredential.MinKeySizeInBits));
            }

#if NET45
            var rsaCryptoProvider = GetCryptoProviderForSha256_Net45(certificate);
            using (var sha = new SHA256Cng())
            {
                var signedData = rsaCryptoProvider.SignData(Encoding.UTF8.GetBytes(message), sha);
                // Cache only valid RSA crypto providers, which are able to sign data successfully
                s_certificateToRsaCspMap[certificate.Thumbprint] = rsaCryptoProvider;
                return signedData;
            }
#else
            return CryptographyManager.SignWithCertificate(message, certificate);
#endif
        }

        /// <summary>
        /// Create a <see cref="RSACryptoServiceProvider"/> using the private key from the given <see cref="X509Certificate2"/>.
        /// </summary>
        /// <param name="certificate">Certificate including private key with which to initialize the <see cref="RSACryptoServiceProvider"/> with</param>
        /// <returns><see cref="RSACryptoServiceProvider"/> initialized with private key from <paramref name="certificate"/></returns>
        private static RSACryptoServiceProvider GetCryptoProviderForSha256_Net45(X509Certificate2 certificate)
        {
            RSACryptoServiceProvider rsaProvider;
            try
            {
                if (!s_certificateToRsaCspMap.TryGetValue(certificate.Thumbprint, out rsaProvider))
                    rsaProvider = certificate.PrivateKey as RSACryptoServiceProvider;
            }
            catch (CryptographicException e)
            {
                throw new MsalClientException(
                    MsalError.CryptoNet45,
                    MsalErrorMessage.CryptoNet45,
                    e);
            }

            if (rsaProvider == null)
            {
                throw new MsalClientException("The provided certificate has a key that is not accessible.");
            }

            const int PROV_RSA_AES = 24;    // CryptoApi provider type for an RSA provider supporting SHA-256 digital signatures

            // ProviderType == 1(PROV_RSA_FULL) and providerType == 12(PROV_RSA_SCHANNEL) are provider types that only support SHA1.
            // Change them to PROV_RSA_AES=24 that supports SHA2 also. Only levels up if the associated key is not a hardware key.
            // Another provider type related to RSA, PROV_RSA_SIG == 2 that only supports Sha1 is no longer supported
            if ((rsaProvider.CspKeyContainerInfo.ProviderType == 1 || rsaProvider.CspKeyContainerInfo.ProviderType == 12) && !rsaProvider.CspKeyContainerInfo.HardwareDevice)
            {
                if (s_certificateToRsaCspMap.TryGetValue(certificate.Thumbprint, out RSACryptoServiceProvider rsacsp))
                    return rsacsp;

                CspParameters csp = new CspParameters
                {
                    ProviderType = PROV_RSA_AES,
                    KeyContainerName = rsaProvider.CspKeyContainerInfo.KeyContainerName,
                    KeyNumber = (int)rsaProvider.CspKeyContainerInfo.KeyNumber
                };

                if (rsaProvider.CspKeyContainerInfo.MachineKeyStore)
                {
                    csp.Flags = CspProviderFlags.UseMachineKeyStore;
                }

                //
                // If UseExistingKey is not specified, the CLR will generate a key for a non-existent group.
                // With this flag, a CryptographicException is thrown instead.
                //
                csp.Flags |= CspProviderFlags.UseExistingKey;
                if (s_certificateToRsaCspMap.Count >= s_maximumMapSize)
                    s_certificateToRsaCspMap.Clear();

                return new RSACryptoServiceProvider(csp);
            }

            return rsaProvider;
        }

        /// <summary>
        ///     <para>
        ///         The GetCngPrivateKey method will return a <see cref="CngKey" /> representing the private
        ///         key of an X.509 certificate which has its private key stored with NCrypt rather than with
        ///         CAPI. If the key is not stored with NCrypt or if there is no private key available,
        ///         GetCngPrivateKey returns null.
        ///     </para>
        ///     <para>
        ///         The HasCngKey method can be used to test if the certificate does have its private key
        ///         stored with NCrypt.
        ///     </para>
        ///     <para>
        ///         The X509Certificate that is used to get the key must be kept alive for the lifetime of the
        ///         CngKey that is returned - otherwise the handle may be cleaned up when the certificate is
        ///         finalized.
        ///     </para>
        /// </summary>
        /// <permission cref="SecurityPermission">The caller of this method must have SecurityPermission/UnmanagedCode.</permission>
        [SecurityCritical]
        internal static CngKey GetCngPrivateKey(X509Certificate2 certificate)
        {
            using (var certContext = GetCertificateContext(certificate))
            using (SafeNCryptKeyHandle privateKeyHandle = X509Native.AcquireCngPrivateKey(certContext))
            {
                // We need to assert for full trust when opening the CNG key because
                // CngKey.Open(SafeNCryptKeyHandle) does a full demand for full trust, and we want to allow
                // access to a certificate's private key by anyone who has access to the certificate itself.
                new PermissionSet(PermissionState.Unrestricted).Assert();
                return CngKey.Open(privateKeyHandle, CngKeyHandleOpenOptions.None);
            }
        }

        /// <summary>
        ///     Get a <see cref="System.Security.Cryptography.X509Certificates.SafeCertContextHandle" /> for the X509 certificate.  The caller of this
        ///     method owns the returned safe handle, and should dispose of it when they no longer need it.
        ///     This handle can be used independently of the lifetime of the original X509 certificate.
        /// </summary>
        /// <permission cref="SecurityPermission">
        ///     The immediate caller must have SecurityPermission/UnmanagedCode to use this method
        /// </permission>
        [SecurityCritical]
        internal static SafeCertContextHandle GetCertificateContext(X509Certificate certificate)
        {
            SafeCertContextHandle certContext = X509Native.DuplicateCertContext(certificate.Handle);

            // Make sure to keep the X509Certificate object alive until after its certificate context is
            // duplicated, otherwise it could end up being closed out from underneath us before we get a
            // chance to duplicate the handle.
            GC.KeepAlive(certificate);

            return certContext;
        }
    }
}
