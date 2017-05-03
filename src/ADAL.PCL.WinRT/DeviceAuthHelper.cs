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
using System.Collections.Generic;
using System.Globalization;
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
            
            Certificate certificate = await FindCertificate(challengeData).ConfigureAwait(false);
            DeviceAuthJWTResponse response = new DeviceAuthJWTResponse(challengeData["SubmitUrl"],
                challengeData["nonce"], Convert.ToBase64String(certificate.GetCertificateBlob().ToArray()));
            IBuffer input = CryptographicBuffer.ConvertStringToBinary(response.GetResponseToSign(),
                BinaryStringEncoding.Utf8);
            CryptographicKey keyPair = await
                PersistedKeyProvider.OpenKeyPairFromCertificateAsync(certificate, HashAlgorithmNames.Sha256,
                    CryptographicPadding.RsaPkcs1V15).AsTask().ConfigureAwait(false);

            IBuffer signed = await CryptographicEngine.SignAsync(keyPair, input).AsTask().ConfigureAwait(false);

            string signedJwt = string.Format(CultureInfo.CurrentCulture, "{0}.{1}", response.GetResponseToSign(),
                Base64UrlEncoder.Encode(signed.ToArray()));
            string authToken = string.Format(CultureInfo.CurrentCulture, " AuthToken=\"{0}\"", signedJwt);
            return string.Format(authHeaderTemplate, authToken, challengeData["Context"], challengeData["Version"]);
        }

        private async Task<Certificate> FindCertificate(IDictionary<string, string> challengeData)
        {
            CertificateQuery query = new CertificateQuery();
            IReadOnlyList<Certificate> certificates = null;
            string errMessage = null;

            if (challengeData.ContainsKey("CertAuthorities"))
            {
                errMessage = "Cert Authorities:" + challengeData["CertAuthorities"];
                PlatformPlugin.Logger.Verbose(null, "Looking up certificate matching authorities:" + challengeData["CertAuthorities"]);
                string[] certAuthorities = challengeData["CertAuthorities"].Split(';');
                foreach (var certAuthority in certAuthorities)
                {
                    //reverse the tokenized string and replace "," with " + "
                    string[] dNames = certAuthority.Split(new[] { "," }, StringSplitOptions.None);
                    string distinguishedIssuerName = dNames[dNames.Length - 1];
                    for (int i = dNames.Length - 2; i >= 0; i--)
                    {
                        distinguishedIssuerName += " + " + dNames[i].Trim();
                    }

                    query.IssuerName = distinguishedIssuerName;
                    certificates = await CertificateStores.FindAllAsync(query).AsTask().ConfigureAwait(false);
                    if (certificates.Count > 0)
                    {
                        break;
                    }
                }
            }
            else
            {
                errMessage = "Cert Thumbprint:" + challengeData["CertThumbprint"];
                PlatformPlugin.Logger.Verbose(null, "Looking up certificate matching thumbprint:" + challengeData["CertThumbprint"]);
                query.Thumbprint = HexStringToByteArray(challengeData["CertThumbprint"]);
                certificates = await CertificateStores.FindAllAsync(query).AsTask().ConfigureAwait(false);
            }

            if (certificates == null || certificates.Count == 0)
            {
                throw new AdalException(AdalError.DeviceCertificateNotFound,
                    string.Format(AdalErrorMessage.DeviceCertificateNotFoundTemplate, errMessage));
            }

            return certificates[0];
        }

        private byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < (hex.Length >> 1); ++i)
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
