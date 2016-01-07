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
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class CryptographyHelper : ICryptographyHelper
    {
        private RSA _rsa;
        private HashAlgorithmName _hashAlgorithm = HashAlgorithmName.SHA256;
        private ECDsa _ecdsaCng;

        public string CreateSha256Hash(string input)
        {
            using (SHA256 sha = SHA256.Create())
            {
                UTF8Encoding encoding = new UTF8Encoding();
                return Convert.ToBase64String(sha.ComputeHash(encoding.GetBytes(input)));
            }
        }

        public byte[] SignWithCertificate(string message, byte[] rawData, string password)
        {
            X509Certificate2 x509Certificate = new X509Certificate2(rawData, password);

            if (x509Certificate.GetPublicKey().Length < ClientAssertionCertificate.MinKeySizeInBits/8)
            {
                throw new ArgumentOutOfRangeException("rawData",
                    string.Format(CultureInfo.InvariantCulture, AdalErrorMessage.CertificateKeySizeTooSmallTemplate, ClientAssertionCertificate.MinKeySizeInBits));
            }

            _rsa = x509Certificate.GetRSAPrivateKey();
            return _rsa.SignData(message.ToByteArray(), _hashAlgorithm, RSASignaturePadding.Pkcs1);
        }


        public string GetX509CertificateThumbprint(ClientAssertionCertificate credential)
        {
            X509Certificate2 x509Certificate = new X509Certificate2(credential.Certificate, credential.Password);
            
            // Thumbprint should be url encoded
            return Base64UrlEncoder.Encode(x509Certificate.GetCertHash());
        }
    }
}
