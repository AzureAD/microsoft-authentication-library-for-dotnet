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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Certificates;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

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
            
            Certificate certificate = await FindCertificate(challengeData);
            DeviceAuthJWTResponse response = new DeviceAuthJWTResponse(challengeData["SubmitUrl"],
                challengeData["nonce"], Convert.ToBase64String(certificate.GetCertificateBlob().ToArray()));
            IBuffer input = CryptographicBuffer.ConvertStringToBinary(response.GetResponseToSign(),
                BinaryStringEncoding.Utf8);
            CryptographicKey keyPair = await 
                    PersistedKeyProvider.OpenKeyPairFromCertificateAsync(certificate, HashAlgorithmNames.Sha256,
                        CryptographicPadding.RsaPkcs1V15);

            IBuffer signed = await CryptographicEngine.SignAsync(keyPair, input);

            string signedJwt = string.Format("{0}.{1}", response.GetResponseToSign(),
                Base64UrlEncoder.Encode(signed.ToArray()));
            string authToken = string.Format("AuthToken=\"{0}\"", signedJwt);
            return string.Format(authHeaderTemplate, authToken, challengeData["Context"], challengeData["Version"]);
        }

        private async Task<Certificate> FindCertificate(IDictionary<string, string> challengeData)
        {
            CertificateQuery query = new CertificateQuery();
            IReadOnlyList<Certificate> certificates = null;

            if (challengeData.ContainsKey("CertAuthorities"))
            {
                PlatformPlugin.Logger.Verbose(null, "Looking up certificate matching authorities:" + challengeData["CertAuthorities"]);
                string[] certAuthorities = challengeData["CertAuthorities"].Split(';');
                foreach (var certAuthority in certAuthorities)
                {
                    //reverse the tokenized string and replace "," with " + "
                    string[] dNames = certAuthority.Split(new[] { "," }, StringSplitOptions.None);
                    string distinguishedIssuerName = dNames[dNames.Length - 1];
                    for (int i = dNames.Length - 2; i >= 0; i--)
                    {
                        distinguishedIssuerName = distinguishedIssuerName.Insert(0, dNames[i].Trim() + " + ");
                    }

                    query.IssuerName = distinguishedIssuerName;
                    certificates = await CertificateStores.FindAllAsync(query);
                    if (certificates.Count > 0)
                    {
                        break;
                    }
                }
            }
            else
            {
                PlatformPlugin.Logger.Verbose(null, "Looking up certificate matching thumbprint:" + challengeData["CertThumbprint"]);
                query.Thumbprint = HexStringToByteArray(challengeData["CertThumbprint"]);
                certificates = await CertificateStores.FindAllAsync(query);
            }

            if (certificates == null || certificates.Count == 0)
            {
                throw new FileNotFoundException("Certificate not found in local machine cert store");
            }

            return certificates[0];
        }

        private byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        private int GetHexVal(char hex)
        {
            int val = (int)hex;
            return val - (val < 58 ? 48 : 55);
        }
    }
}
