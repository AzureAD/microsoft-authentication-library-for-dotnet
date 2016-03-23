//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Native;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class DeviceAuthHelper : IDeviceAuthHelper
    {
        public bool CanHandleDeviceAuthChallenge
        {
            get { return true; }
        }

        public async Task<string> CreateDeviceAuthChallengeResponse(IDictionary<string, string> challengeData)
        {
            string authHeaderTemplate = "PKeyAuth {0}, Context=\"{1}\", Version=\"{2}\"";

            X509Certificate2 certificate = FindCertificate(challengeData);
            DeviceAuthJWTResponse response = new DeviceAuthJWTResponse(challengeData["SubmitUrl"],
                challengeData["nonce"], Convert.ToBase64String(certificate.GetRawCertData()));
            CngKey key = GetCngPrivateKey(certificate);
            byte[] sig = null;
            using (RSACng rsa = new RSACng(key))
            {
                rsa.SignatureHashAlgorithm = CngAlgorithm.Sha256;
                sig = rsa.SignData(response.GetResponseToSign().ToByteArray());
            }

            string signedJwt = string.Format(CultureInfo.CurrentCulture, " {0}.{1}", response.GetResponseToSign(),
                Base64UrlEncoder.Encode(sig));
            string authToken = string.Format(CultureInfo.CurrentCulture, " AuthToken=\"{0}\"", signedJwt);
            Task<string> resultTask =
                Task.Factory.StartNew(
                    () =>
                    {
                        return string.Format(authHeaderTemplate, authToken, challengeData["Context"],
                            challengeData["Version"]);
                    });

            return await resultTask;
        }

        public bool CanUseBroker
        {
            get { return false; }
        }

        private X509Certificate2 FindCertificate(IDictionary<string, string> challengeData)
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates;
                X509Certificate2Collection signingCert = null;
                if (challengeData.ContainsKey("CertAuthorities"))
                {
                    string[] certAuthorities = challengeData["CertAuthorities"].Split(new[] {";"},
                        StringSplitOptions.None);
                    foreach (var certAuthority in certAuthorities)
                    {
                        //reverse the tokenized string and replace "," with " + "
                        string[] dNames = certAuthority.Split(new[] {","}, StringSplitOptions.None);
                        string distinguishedIssuerName = dNames[dNames.Length - 1];
                        for (int i = dNames.Length - 2; i >= 0; i--)
                        {
                            distinguishedIssuerName = distinguishedIssuerName.Insert(0, dNames[i].Trim() + " + ");
                        }

                        signingCert = certCollection.Find(X509FindType.FindByIssuerDistinguishedName,
                            distinguishedIssuerName, false);
                        if (signingCert.Count > 0)
                        {
                            break;
                        }
                    }

                    if (signingCert == null || signingCert.Count == 0)
                    {
                        throw new AdalException(AdalError.DeviceCertificateNotFound,
                            string.Format(CultureInfo.CurrentCulture, AdalErrorMessage.DeviceCertificateNotFoundTemplate,
                                "Cert Authorities:" + challengeData["CertAuthorities"]));
                    }
                }
                else
                {
                    signingCert = certCollection.Find(X509FindType.FindByThumbprint, challengeData["CertThumbprint"],
                        false);
                    if (signingCert.Count == 0)
                    {
                        throw new AdalException(AdalError.DeviceCertificateNotFound,
                            string.Format(CultureInfo.CurrentCulture, AdalErrorMessage.DeviceCertificateNotFoundTemplate,
                                "Cert thumbprint:" + challengeData["CertThumbprint"]));
                    }
                }

                return signingCert[0];
            }
            finally
            {
                store.Close();
            }
        }


        /// <summary>
        ///     <para>
        ///         The GetCngPrivateKey method will return a <see cref="CngKey"/> representing the private
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
            using (SafeCertContextHandle certContext = GetCertificateContext(certificate))
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
    public sealed class SafeCertContextHandle : SafeHandleZeroOrMinusOneIsInvalid
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
