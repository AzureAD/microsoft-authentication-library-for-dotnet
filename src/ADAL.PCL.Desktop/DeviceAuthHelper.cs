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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    class DeviceAuthHelper : IDeviceAuthHelper
    {
        public bool CanHandleDeviceAuthChallenge { get { return true; } }

        public async Task<string> CreateDeviceAuthChallengeResponse(IDictionary<string, string> challengeData)
        {
            string authHeaderTemplate = "PKeyAuth {0}, Context=\"{1}\", Version=\"{2}\"";

            X509Certificate2 certificate = FindCertificate(challengeData);
                DeviceAuthJWTResponse response = new DeviceAuthJWTResponse(challengeData["SubmitUrl"], challengeData["nonce"], Convert.ToBase64String(certificate.GetRawCertData()));

            //TODO - CNG signing
            CngKey key = null;// certificate.getC
                byte[] sig = null;
                using (RSACng rsa = new RSACng(key))
                {
                    sig = rsa.SignData(response.GetResponseToSign().ToByteArray(), HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);
                }
                
                string signedJwt = string.Format("{0}.{1}", response.GetResponseToSign(),
                    Base64UrlEncoder.Encode(sig));
                string authToken = string.Format("AuthToken=\"{0}\"", signedJwt);
                Task<string> resultTask = Task.Factory.StartNew(() =>
                {
                    return string.Format(authHeaderTemplate, authToken, challengeData["Context"], challengeData["Version"]);
                });

                return await resultTask;
        }

        public bool CanUseBroker { get { return false; } }

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
                        throw new MsalException(MsalError.DeviceCertificateNotFound,
                            string.Format(MsalErrorMessage.DeviceCertificateNotFoundTemplate, "Cert Authorities:" + challengeData["CertAuthorities"]));
                    }
                }
                else
                {
                    signingCert = certCollection.Find(X509FindType.FindByThumbprint, challengeData["CertThumbprint"],
                        false);
                    if (signingCert.Count == 0)
                    {
                        throw new MsalException(MsalError.DeviceCertificateNotFound,
                            string.Format(MsalErrorMessage.DeviceCertificateNotFoundTemplate, "Cert thumbprint:" + challengeData["CertThumbprint"]));
                    }
                }

                return signingCert[0];
            }
            finally
            {
                store.Close();
            }
        }
    }
}
