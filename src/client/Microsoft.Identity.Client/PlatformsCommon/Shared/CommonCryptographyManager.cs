// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{

#if ANDROID && !MAUI
    [global::Android.Runtime.Preserve(AllMembers = true)]
#endif
    [Preserve(AllMembers = true)]
    internal class CommonCryptographyManager : ICryptographyManager
    {
        private static readonly ConcurrentDictionary<string, RSA> s_certificateToRsaMap = new ConcurrentDictionary<string, RSA>();
        private static readonly int s_maximumMapSize = 1000;

        protected ILoggerAdapter Logger { get; }

        public CommonCryptographyManager(ILoggerAdapter logger = null)
        {
            Logger = logger;
        }

        public string CreateBase64UrlEncodedSha256Hash(string input)
        {
            return string.IsNullOrEmpty(input) ? null : Base64UrlHelpers.Encode(CreateSha256HashBytes(input));
        }

        public string GenerateCodeVerifier()
        {
            byte[] buffer = new byte[Constants.CodeVerifierByteSize];
            using (var randomSource = RandomNumberGenerator.Create())
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
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }

        /// <remarks>AAD only supports RSA certs for client credentials </remarks>
        public virtual byte[] SignWithCertificate(string message, X509Certificate2 certificate)
        {
            // MSAL used to check min key size by looking at certificate.GetRSAPublicKey().KeySize
            // but this causes sporadic failures in the crypto stack. Rely on AAD to perform key size validations.
            if (!s_certificateToRsaMap.TryGetValue(certificate.Thumbprint, out RSA rsa))
            {
                if (s_certificateToRsaMap.Count >= s_maximumMapSize)
                    s_certificateToRsaMap.Clear();

                rsa = certificate.GetRSAPrivateKey();
            }

            //Ensure certificate is of type RSA.
            if (rsa == null)
            {
                throw new MsalClientException(MsalError.CertificateNotRsa, MsalErrorMessage.CertMustBeRsa(certificate.PublicKey?.Oid?.FriendlyName));
            }

            try
            {
                return SignDataAndCacheProvider(message);
            }
            catch (Exception ex)
            {
                Logger?.Warning($"Exception occurred when signing data with a certificate. {ex}");

                rsa = certificate.GetRSAPrivateKey();

                return SignDataAndCacheProvider(message);
            }

            byte[] SignDataAndCacheProvider(string message)
            {
                var signedData = rsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                
                // Cache only valid RSA crypto providers, which are able to sign data successfully
                s_certificateToRsaMap[certificate.Thumbprint] = rsa;
                return signedData;
            }
        }
    }
}
