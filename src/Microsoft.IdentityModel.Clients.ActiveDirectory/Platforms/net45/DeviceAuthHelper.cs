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

using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Helpers;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Native;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform
{
    internal static class DeviceAuthHelper
    {
        public static bool CanHandleDeviceAuthChallenge
        {
            get { return true; }
        }

        public static async Task<string> CreateDeviceAuthChallengeResponse(IDictionary<string, string> challengeData)
        {
            string authHeaderTemplate = "PKeyAuth {0}, Context=\"{1}\", Version=\"{2}\"";

            X509Certificate2 certificate = FindCertificate(challengeData);
            DeviceAuthJWTResponse response = new DeviceAuthJWTResponse(challengeData["SubmitUrl"],
                challengeData["nonce"], Convert.ToBase64String(certificate.GetRawCertData()));
            CngKey key = SigningHelper.GetCngPrivateKey(certificate);
            byte[] sig = null;
            using (RSACng rsa = new RSACng(key))
            {
                rsa.SignatureHashAlgorithm = CngAlgorithm.Sha256;
                sig = rsa.SignData(response.GetResponseToSign().ToByteArray());
            }

            string signedJwt = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", response.GetResponseToSign(),
                Base64UrlEncoder.Encode(sig));
            string authToken = string.Format(CultureInfo.InvariantCulture, " AuthToken=\"{0}\"", signedJwt);
            Task<string> resultTask =
                Task.Factory.StartNew(
                    () =>
                    {
                        return string.Format(CultureInfo.InvariantCulture, authHeaderTemplate, authToken,
                            challengeData["Context"],
                            challengeData["Version"]);
                    });

            return await resultTask.ConfigureAwait(false);
        }

        private static X509Certificate2 FindCertificate(IDictionary<string, string> challengeData)
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates;
                if (challengeData.ContainsKey("CertAuthorities"))
                {
                   return FindCertificateByCertAuthorities(challengeData, certCollection);
                }

                X509Certificate2Collection signingCert = null;
                signingCert = certCollection.Find(X509FindType.FindByThumbprint, challengeData["CertThumbprint"],
                    false);
                if (signingCert.Count == 0)
                {
                    throw new AdalException(AdalError.DeviceCertificateNotFound,
                        string.Format(CultureInfo.CurrentCulture, AdalErrorMessage.DeviceCertificateNotFoundTemplate,
                            "Cert thumbprint:" + challengeData["CertThumbprint"]));
                }

                return signingCert[0];
            }
            finally
            {
                store.Close();
            }
        }

        private static X509Certificate2 FindCertificateByCertAuthorities(IDictionary<string, string> challengeData, X509Certificate2Collection certCollection)
        {
            X509Certificate2Collection signingCert = null;
            string[] certAuthorities = challengeData["CertAuthorities"].Split(new[] { ";" },
                StringSplitOptions.None);
            foreach (var certAuthority in certAuthorities)
            {
                //reverse the tokenized string and replace "," with " + "
                string[] dNames = certAuthority.Split(new[] { "," }, StringSplitOptions.None);
                string distinguishedIssuerName = dNames[dNames.Length - 1];
                for (int i = dNames.Length - 2; i >= 0; i--)
                {
                    distinguishedIssuerName += " + " + dNames[i].Trim();
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
                    string.Format(CultureInfo.CurrentCulture,
                        AdalErrorMessage.DeviceCertificateNotFoundTemplate,
                        "Cert Authorities:" + challengeData["CertAuthorities"]));
            }

            return signingCert[0];
        }
    }
}