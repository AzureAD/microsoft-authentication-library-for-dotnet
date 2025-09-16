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
        /// <param name="certificatePem">The certificate in PEM format</param>
        /// <param name="privateKey">The RSA private key to attach</param>
        /// <returns>An X509Certificate2 with the private key attached</returns>
        /// <exception cref="ArgumentNullException">Thrown when certificatePem or privateKey is null</exception>
        /// <exception cref="ArgumentException">Thrown when certificatePem is not a valid PEM certificate</exception>
        /// <exception cref="FormatException">Thrown when the certificate cannot be parsed</exception>
        internal static X509Certificate2 AttachPrivateKeyToCert(string certificatePem, RSA privateKey)
        {
            if (string.IsNullOrEmpty(certificatePem))
                throw new ArgumentNullException(nameof(certificatePem));
            if (privateKey == null)
                throw new ArgumentNullException(nameof(privateKey));

            // .NET 8.0+ has direct PEM parsing support, but we still need to parse the PEM format properly
            X509Certificate2 certificate = ParseCertificateFromPem(certificatePem);

            try
            {
#if NET8_0_OR_GREATER
                return certificate.CopyWithPrivateKey(privateKey);
#else
                // .NET Framework 4.7.2 and .NET Standard 2.0 - manual private key attachment
                return AttachPrivateKeyToOlderFrameworks(certificate, privateKey);
#endif
            }
            catch (Exception e)
            {
                throw new MsalServiceException(MsalError.InvalidPemCertificate, MsalErrorMessage.InvalidPemCertificate, e);
            }
        }

        /// <summary>
        /// Parses a certificate from PEM format.
        /// </summary>
        /// <param name="certificatePem">The certificate in PEM format</param>
        /// <returns>An X509Certificate2 instance</returns>
        /// <exception cref="ArgumentException">Thrown when the PEM format is invalid</exception>
        /// <exception cref="FormatException">Thrown when the Base64 content cannot be decoded</exception>
        private static X509Certificate2 ParseCertificateFromPem(string certificatePem)
        {
            // Handle JSON-escaped newlines by converting them to actual newlines
            string normalizedPem = certificatePem.Replace("\\n", "\n").Replace("\\r", "\r");

            const string CertBeginMarker = "-----BEGIN CERTIFICATE-----";
            const string CertEndMarker = "-----END CERTIFICATE-----";

            int startIndex = normalizedPem.IndexOf(CertBeginMarker, StringComparison.Ordinal);
            if (startIndex == -1)
            {
                throw new ArgumentException("Invalid PEM format: missing BEGIN CERTIFICATE marker", nameof(certificatePem));
            }

            startIndex += CertBeginMarker.Length;
            int endIndex = normalizedPem.IndexOf(CertEndMarker, startIndex, StringComparison.Ordinal);
            if (endIndex == -1)
            {
                throw new ArgumentException("Invalid PEM format: missing END CERTIFICATE marker", nameof(certificatePem));
            }

            string base64Content = normalizedPem.Substring(startIndex, endIndex - startIndex)
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace(" ", "");

            if (string.IsNullOrEmpty(base64Content))
            {
                throw new ArgumentException("Invalid PEM format: no certificate content found", nameof(certificatePem));
            }

            try
            {
                byte[] certBytes = Convert.FromBase64String(base64Content);
                return new X509Certificate2(certBytes);
            }
            catch (FormatException ex)
            {
                throw new FormatException("Invalid PEM format: certificate content is not valid Base64", ex);
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
            using (var rsaProvider = new RSACryptoServiceProvider(keySize))
            {
                // Import the parameters into the new provider
                rsaProvider.ImportParameters(parameters);

                // Create a new certificate instance from the raw data
                var certWithPrivateKey = new X509Certificate2(certificate.RawData);

                // Assign the private key using the legacy property
                certWithPrivateKey.PrivateKey = rsaProvider;

                return certWithPrivateKey;
            }
        }
#endif
    }
}
