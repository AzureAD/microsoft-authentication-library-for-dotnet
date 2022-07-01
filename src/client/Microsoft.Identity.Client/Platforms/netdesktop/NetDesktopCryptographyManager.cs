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
using Microsoft.Identity.Client.Platforms.net461.Native;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Utils;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.Platforms.net461
{
    internal class NetDesktopCryptographyManager : ICryptographyManager
    {
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
            return CryptographyManager.SignWithCertificate(message, certificate);
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
