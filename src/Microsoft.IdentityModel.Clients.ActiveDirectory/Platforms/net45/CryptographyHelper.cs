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
using System.Globalization;
using System.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Diagnostics.CodeAnalysis;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Native;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform
{
    internal class CryptographyHelper
    {
        public static string CreateSha256Hash(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            using (SHA256Cng sha = new SHA256Cng())
            {
                UTF8Encoding encoding = new UTF8Encoding();
                return Convert.ToBase64String(sha.ComputeHash(encoding.GetBytes(input)));
            }
        }

        public byte[] SignWithCertificate(string message, X509Certificate2 certificate)
        {
            if (certificate.PublicKey.Key.KeySize < ClientAssertionCertificate.MinKeySizeInBits)
            {
                throw new ArgumentOutOfRangeException(nameof(certificate),
                    string.Format(CultureInfo.InvariantCulture, AdalErrorMessage.CertificateKeySizeTooSmallTemplate,
                        ClientAssertionCertificate.MinKeySizeInBits));
            }

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            var x509Key = new X509AsymmetricSecurityKey(certificate);

            RSA rsa = x509Key.GetAsymmetricAlgorithm(SecurityAlgorithms.RsaSha256Signature, true) as RSA;

            RSACryptoServiceProvider newRsa = null;
            try
            {
                if (rsa is RSACryptoServiceProvider cspRsa)
                {
                    // For .NET 4.6 and below we get the old RSACryptoServiceProvider implementation as the default.
                    // Try and get an instance of RSACryptoServiceProvider which supports SHA256
                    newRsa = GetCryptoProviderForSha256(cspRsa);

                    using (var sha = new SHA256Cng())
                    {
                        return newRsa.SignData(messageBytes, sha);
                    }
                }
                else
                {
                    CngKey key = GetCngPrivateKey(certificate);
                    using (RSACng rsaCng = new RSACng(key))
                    {
                        rsaCng.SignatureHashAlgorithm = CngAlgorithm.Sha256;
                        return rsaCng.SignData(messageBytes);
                    }
                }
            }
            finally
            {
                // We only want to dispose of the 'newRsa' instance if it is a *different instance*
                // from the original one that was used to create it.
                if (newRsa != null && !ReferenceEquals(rsa, newRsa))
                {
                    newRsa.Dispose();
                }
            }
        }

        // Copied from ACS code
        // This method returns an AsymmetricSignatureFormatter capable of supporting Sha256 signatures.
        private static RSACryptoServiceProvider GetCryptoProviderForSha256(RSACryptoServiceProvider rsaProvider)
        {
            const int PROV_RSA_AES = 24;    // CryptoApi provider type for an RSA provider supporting sha-256 digital signatures

            // ProviderType == 1(PROV_RSA_FULL) and providerType == 12(PROV_RSA_SCHANNEL) are provider types that only support SHA1. Change them to PROV_RSA_AES=24 that supports SHA2 also.
            // Only levels up if the associated key is not a hardware key.
            // Another provider type related to rsa, PROV_RSA_SIG == 2 that only supports Sha1 is no longer supported
            if ((rsaProvider.CspKeyContainerInfo.ProviderType == 1 || rsaProvider.CspKeyContainerInfo.ProviderType == 12) && !rsaProvider.CspKeyContainerInfo.HardwareDevice)
            {
                CspParameters csp = new CspParameters
                {
                    ProviderType = PROV_RSA_AES,
                    KeyContainerName = rsaProvider.CspKeyContainerInfo.KeyContainerName,
                    KeyNumber = (int) rsaProvider.CspKeyContainerInfo.KeyNumber
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
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "Safe use of LinkDemand methods")]
        public static CngKey GetCngPrivateKey(X509Certificate2 certificate)
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
        ///     Get a <see cref="SafeCertContextHandle" /> for the X509 certificate.  The caller of this
        ///     method owns the returned safe handle, and should dispose of it when they no longer need it.
        ///     This handle can be used independently of the lifetime of the original X509 certificate.
        /// </summary>
        /// <permission cref="SecurityPermission">
        ///     The immediate caller must have SecurityPermission/UnmanagedCode to use this method
        /// </permission>
        [SecurityCritical]
        [SuppressMessage("Microsoft.Reliability", "CA2004:RemoveCallsToGCKeepAlive",
            Justification =
                "This method is used to create the safe handle, and KeepAlive is needed to prevent racing the GC while doing so"
        )]
        public static SafeCertContextHandle GetCertificateContext(X509Certificate certificate)
        {
            SafeCertContextHandle certContext = X509Native.DuplicateCertContext(certificate.Handle);

            // Make sure to keep the X509Certificate object alive until after its certificate context is
            // duplicated, otherwise it could end up being closed out from underneath us before we get a
            // chance to duplicate the handle.
            GC.KeepAlive(certificate);

            return certContext;
        }
    }

    [SecurityCritical]
    internal sealed class SafeCertContextHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeCertContextHandle()
            : base(true)
        {
        }

        [DllImport("crypt32.dll")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass",
            Justification = "SafeHandle release method")]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CertFreeCertificateContext(IntPtr pCertContext);

        protected override bool ReleaseHandle()
        {
            return CertFreeCertificateContext(handle);
        }
    }
}
