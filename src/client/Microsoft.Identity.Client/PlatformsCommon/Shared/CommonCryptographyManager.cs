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

        public string CreateSha256HashHex(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            byte[] hashBytes = CreateSha256HashBytes(input);

            // Convert to hex using BitConverter, removing dashes and forcing lowercase
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        /// <remarks>AAD only supports RSA certs for client credentials </remarks>
        public virtual byte[] SignWithCertificate(string message, X509Certificate2 certificate, RSASignaturePadding signaturePadding)
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
                // CodeQL [SM03799] PKCS1 padding is for Identity Providers not supporting PSS (older ADFS, dSTS)
                var signedData = rsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256, signaturePadding);

                // Cache only valid RSA crypto providers, which are able to sign data successfully
                s_certificateToRsaMap[certificate.Thumbprint] = rsa;
                return signedData;
            }
        }

        /// <summary>
        /// Attaches a private key to a certificate for use in mTLS authentication.
        /// </summary>
        /// <param name="rawCertificate">The certificate received from the Imds server</param>
        /// <param name="privateKey">The RSA private key to attach</param>
        /// <returns>An X509Certificate2 with the private key attached</returns>
        /// <exception cref="ArgumentNullException">Thrown when rawCertificate or privateKey is null</exception>
        /// <exception cref="FormatException">Thrown when rawCertificate is empty, invalid, and cannot be parsed</exception>
        internal static X509Certificate2 AttachPrivateKeyToCert(string rawCertificate, RSA privateKey)
        {
            if (string.IsNullOrEmpty(rawCertificate))
                throw new MsalServiceException(MsalError.InvalidCertificate, MsalErrorMessage.InvalidCertificate);
            if (privateKey == null)
                throw new ArgumentNullException(nameof(privateKey));

            X509Certificate2 certificate = null;

            try
            {
                byte[] certBytes = Convert.FromBase64String(rawCertificate);
                certificate = new X509Certificate2(certBytes);
            }
            catch (FormatException ex)
            {
                throw new MsalServiceException(MsalError.InvalidCertificate, MsalErrorMessage.InvalidCertificate, ex);
            }

            try
            {
#if NET8_0_OR_GREATER
                // Attach the private key and return a new certificate instance
                return certificate.CopyWithPrivateKey(privateKey);
#else
                // .NET Framework 4.7.2 and .NET Standard 2.0 - manual private key attachment
                return AttachPrivateKeyToOlderFrameworks(certificate, privateKey);
#endif
            }
            catch (Exception ex)
            {
                throw new MsalServiceException(MsalError.InvalidCertificate, MsalErrorMessage.InvalidCertificate, ex);
            }
        }

#if !NET8_0_OR_GREATER
        /// <summary>
        /// Attaches a private key to a certificate for older .NET Framework versions.
        /// This method uses the older RSACng approach for .NET Framework 4.7.2 and .NET Standard 2.0.
        /// </summary>
        /// <param name="certificate">The certificate without private key</param>
        /// <param name="privateKey">The RSA private key to attach</param>
        /// <returns>An X509Certificate2 with the private key attached</returns>
        /// <exception cref="NotSupportedException">Thrown when private key attachment fails</exception>
        private static X509Certificate2 AttachPrivateKeyToOlderFrameworks(X509Certificate2 certificate, RSA privateKey)
        {
            // For older frameworks, we need to use the legacy approach with RSACryptoServiceProvider
            // First, export the RSA parameters from the provided private key
            var parameters = privateKey.ExportParameters(includePrivateParameters: true);

            // Create a new RSACryptoServiceProvider with the correct key size
            int keySize = parameters.Modulus.Length * 8;

            // Do NOT dispose rsaProvider here: the certificate takes ownership of
            // the key container.  Disposing the provider destroys the ephemeral
            // key container and makes the certificate's private key inaccessible.
            var rsaProvider = new RSACryptoServiceProvider(keySize);
            rsaProvider.ImportParameters(parameters);

            var certWithPrivateKey = new X509Certificate2(certificate.RawData);
            certWithPrivateKey.PrivateKey = rsaProvider;

            return certWithPrivateKey;
        }
#endif
    }
}
