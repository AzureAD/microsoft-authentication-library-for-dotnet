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
using System.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class CryptographyHelper
    {
        public static byte[] SignWithCertificate(string message, X509Certificate2 x509Certificate)
        {
            X509AsymmetricSecurityKey x509Key = new X509AsymmetricSecurityKey(x509Certificate);
            RSACryptoServiceProvider rsa = x509Key.GetAsymmetricAlgorithm(SecurityAlgorithms.RsaSha256Signature, true) as RSACryptoServiceProvider;

            RSACryptoServiceProvider newRsa = null;
            try
            {
                newRsa = GetCryptoProviderForSha256(rsa);
                using (SHA256Cng sha = new SHA256Cng())
                {
                    return newRsa.SignData(Encoding.UTF8.GetBytes(message), sha);
                }
            }
            finally
            {
                if (newRsa != null && !object.ReferenceEquals(rsa, newRsa))
                {
                    newRsa.Dispose();
                }
            }
        }

        public static byte[] SignWithSymmetricKey(string message, byte[] key)
        {
            using (HMAC hmac = HMAC.Create("HMACSHA256"))
            {
                hmac.Key = key;
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            }
        }

        // Copied from ACS code
        // This method returns an AsymmetricSignatureFormatter capable of supporting Sha256 signatures. 
        private static RSACryptoServiceProvider GetCryptoProviderForSha256(RSACryptoServiceProvider rsaProvider)
        {
            const int PROV_RSA_AES = 24;    // CryptoApi provider type for an RSA provider supporting sha-256 digital signatures
            if (rsaProvider.CspKeyContainerInfo.ProviderType == PROV_RSA_AES)
            {
                return rsaProvider;
            }

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
            return new RSACryptoServiceProvider(csp);
        }
    }
}
