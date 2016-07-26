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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class CryptographyHelper : ICryptographyHelper
    {
        public string CreateSha256Hash(string input)
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
                throw new ArgumentOutOfRangeException("rawData",
                    string.Format(CultureInfo.InvariantCulture, AdalErrorMessage.CertificateKeySizeTooSmallTemplate, ClientAssertionCertificate.MinKeySizeInBits));
            }

            X509AsymmetricSecurityKey x509Key = new X509AsymmetricSecurityKey(certificate);
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

            return rsaProvider;
        }
    }
}
