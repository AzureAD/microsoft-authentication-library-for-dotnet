// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.net45
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
            using (RNGCryptoServiceProvider randomSource = new RNGCryptoServiceProvider())
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
            using (var sha = new SHA256Cng())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }

        public string Encrypt(string message)
        {
            throw new NotImplementedException();
        }

        public string Decrypt(string encryptedMessage)
        {
            throw new NotImplementedException();
        }

        public byte[] Encrypt(byte[] message)
        {
            throw new NotImplementedException();
        }

        public byte[] Decrypt(byte[] encryptedMessage)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public byte[] SignWithCertificate(string message, X509Certificate2 certificate)
        {
            if (certificate.PublicKey.Key.KeySize < ClientCredentialWrapper.MinKeySizeInBits)
            {
                throw new ArgumentOutOfRangeException(nameof(certificate),
                    string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.CertificateKeySizeTooSmallTemplate,
                        ClientCredentialWrapper.MinKeySizeInBits));
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
                }
                else
                {
                    // For .NET Framework 4.7 and onwards the RSACng implementation is the default.
                    // Since we're targeting .NET Framework 4.5, we cannot actually use this type as it was
                    // only introduced with .NET Framework 4.6.
                    // Instead we try and create an RSACryptoServiceProvider based on the private key from the
                    // certificate.
                    newRsa = GetCryptoProviderForSha256(certificate);
                }

                using (var sha = new SHA256Cng())
                {
                    return newRsa.SignData(messageBytes, sha);
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

        /// <summary>
        /// Create a <see cref="RSACryptoServiceProvider"/> using the private key from the given <see cref="X509Certificate2"/>.
        /// </summary>
        /// <param name="certificate">Certificate including private key with which to initialize the <see cref="RSACryptoServiceProvider"/> with</param>
        /// <returns><see cref="RSACryptoServiceProvider"/> initialized with private key from <paramref name="certificate"/></returns>
        private static RSACryptoServiceProvider GetCryptoProviderForSha256(X509Certificate2 certificate)
        {
            // We need to call the other GetCryptoProviderForSha256 method in case certificate.PrivateKey is using
            // one of the providers that doesn't support SHA256
            return GetCryptoProviderForSha256((RSACryptoServiceProvider)certificate.PrivateKey);
        }

        // Copied from ACS code
        // This method returns an AsymmetricSignatureFormatter capable of supporting Sha256 signatures.
        private static RSACryptoServiceProvider GetCryptoProviderForSha256(RSACryptoServiceProvider rsaProvider)
        {
            const int PROV_RSA_AES = 24;    // CryptoApi provider type for an RSA provider supporting sha-256 digital signatures

            // ProviderType == 1(PROV_RSA_FULL) and providerType == 12(PROV_RSA_SCHANNEL) are provider types that only support SHA1.
            // Change them to PROV_RSA_AES=24 that supports SHA2 also. Only levels up if the associated key is not a hardware key.
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
