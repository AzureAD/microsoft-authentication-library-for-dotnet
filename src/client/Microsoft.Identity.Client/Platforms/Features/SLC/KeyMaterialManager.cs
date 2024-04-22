// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Platforms.Windows;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Platforms.netcore
{
    /// <summary>
    /// Provides X509_2 certificates and cryptographic key information for a Credential based 
    /// Managed Identity-supported Azure resource. 
    /// This class handles the retrieval or creation of X.509_2 certificates for authentication purposes,
    /// including the determination of the cryptographic key type.
    /// For more details, see https://aka.ms/msal-net-managed-identity.
    /// </summary>
    internal class ManagedIdentityCertificateProvider : IKeyMaterialManager
    {
        // Field to store the current crypto key type
        private static CryptoKeyType s_cryptoKeyType = CryptoKeyType.None;

        // Name for the key storage provider and key names on Windows
        private const string KeyProviderName = "Microsoft Software Key Storage Provider";

        // Subject name for the binding certificate
        private const string CertSubjectname = "ManagedIdentitySlcCertificate";

        // Cache the binding certificate across instances
        private static X509Certificate2 s_bindingCertificate;

        // Lock object for ensuring thread safety when accessing key information
        private readonly object _keyInfoLock = new();

        // Logger instance for capturing log information
        private readonly ILoggerAdapter _logger;

        private readonly KeyGuardManager _keyGuardManager; // Use KeyGuardManager

        private bool _isInitialized = false;

        // Property to get or create the binding certificate from crypto key information
        public X509Certificate2 BindingCertificate
        {
            get
            {
                if (!_isInitialized)
                {
                    s_bindingCertificate = GetOrCreateCertificateFromCryptoKeyInfo();
                    _isInitialized = true;
                }

                return s_bindingCertificate;
            }
        }

        // Property to expose the current crypto key type
        public CryptoKeyType CryptoKeyType
        {
            get
            {
                if (!_isInitialized)
                {
                    throw new InvalidOperationException("CryptoKeyType cannot be accessed before initialization.");
                }

                return s_cryptoKeyType;
            }
        }

        public ManagedIdentityCertificateProvider(ILoggerAdapter logger)
        {
            _logger = logger;
            _keyGuardManager = new KeyGuardManager(logger); 
        }

        /// <summary>
        /// Retrieves or creates an X.509 certificate from crypto key information.
        /// </summary>
        /// <returns>
        /// The X.509 certificate if available and still valid in the cache; otherwise, a new certificate is created.
        /// </returns>
        public X509Certificate2 GetOrCreateCertificateFromCryptoKeyInfo()
        {
            if (s_bindingCertificate != null && !CertificateNeedsRotation(s_bindingCertificate))
            {
                _logger.Verbose(() => "[Managed Identity] A non-expired cached binding certificate is available.");
                return s_bindingCertificate;
            }

            lock (_keyInfoLock) // Lock to ensure thread safety
            {
                if (s_bindingCertificate != null && !CertificateNeedsRotation(s_bindingCertificate))
                {
                    _logger.Verbose(() => "[Managed Identity] Another thread created the certificate while waiting for the lock.");
                    _isInitialized = true;
                    return s_bindingCertificate;
                }

                // The cached certificate needs to be rotated or does not exist
                ECDsa eCDsaKey = _keyGuardManager.LoadCngKeyWithProvider(KeyProviderName);
                s_cryptoKeyType = _keyGuardManager.CryptoKeyType;

                if (eCDsaKey != null)
                {
                    s_bindingCertificate = CreateBindingCertificate(eCDsaKey);
                    _isInitialized = true;
                    return s_bindingCertificate;
                }
            }

            _isInitialized = false;
            return null;
        }

        /// <summary>
        /// Determines if a given X.509 certificate needs rotation based on a percentage threshold.
        /// </summary>
        /// <param name="certificate">The X.509 certificate to evaluate.</param>
        /// <param name="rotationPercentageThreshold">The threshold percentage for considering rotation (default is 70%).</param>
        /// <returns>
        /// True if the certificate needs rotation, false otherwise.
        /// </returns>
        public static bool CertificateNeedsRotation(X509Certificate2 certificate, double rotationPercentageThreshold = 70)
        {
            DateTime now = DateTime.UtcNow;

            // Calculate the total duration of the certificate's validity
            TimeSpan certificateLifetime = certificate.NotAfter - certificate.NotBefore;

            // Calculate how much time has passed since the certificate's issuance
            TimeSpan elapsedTime = now - certificate.NotBefore;

            // Calculate the current percentage of the certificate's lifetime that has passed
            double percentageElapsed = (elapsedTime.TotalMilliseconds / certificateLifetime.TotalMilliseconds) * 100.0;

            // Check if the percentage elapsed exceeds the rotation threshold
            return percentageElapsed >= rotationPercentageThreshold;
        }

        /// <summary>
        /// Creates a binding certificate with a the key material for use in Managed Identity scenarios.
        /// </summary>
        /// <param name="eCDsaKey">The key used for creating the certificate.</param>
        /// <returns>The created binding certificate.</returns>
        private X509Certificate2 CreateBindingCertificate(ECDsa eCDsaKey)
        {
            try
            {
                lock (_keyInfoLock) // Lock to ensure thread safety
                {
                    _logger.Verbose(() => "[Managed Identity] Creating binding certificate " +
                    "with CNG key for credential endpoint.");

                    // Create a certificate request
                    CertificateRequest request = CreateCertificateRequest(CertSubjectname, eCDsaKey);

                    // Create a self-signed X.509 certificate
                    DateTimeOffset startDate = DateTimeOffset.UtcNow;
                    DateTimeOffset endDate = startDate.AddYears(5); //expiry 

                    //Create the self signed cert
                    X509Certificate2 selfSigned = request.CreateSelfSigned(startDate, endDate);

                    if (!selfSigned.HasPrivateKey)
                    {
                        _logger.Error("[Managed Identity] The Certificate is missing the private key.");
                        throw new InvalidOperationException("The MTLS Certificate must include a private key.");
                    }

                    _logger.Verbose(() => $"[Managed Identity] Binding certificate (with cng key) created successfully. Has Private Key ? : {selfSigned.HasPrivateKey}");

                    return selfSigned;
                }
            }
            catch (CryptographicException ex)
            {
                // log the exception
                _logger.Error($"Error generating binding certificate: {ex.Message}");

                throw new MsalClientException(MsalError.CertificateCreationFailed,
                    $"Failed to create Managed Identity binding certificate. Error : {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a certificate request for the binding certificate using the specified subject name and ECDsa key.
        /// </summary>
        /// <param name="subjectName">The subject name for the certificate (e.g., Common Name).</param>
        /// <param name="ecdsaKey">The ECDsa key to be associated with the certificate request.</param>
        /// <returns>The certificate request for the binding certificate.</returns>
        private CertificateRequest CreateCertificateRequest(string subjectName, ECDsa ecdsaKey)
        {
            CertificateRequest certificateRequest = null;

            _logger.Verbose(() => "[Managed Identity] Creating certificate request for the binding certificate.");

            return certificateRequest = new CertificateRequest(
                    $"CN={subjectName}", // Common Name 
                    ecdsaKey, // ECDsa key
                    HashAlgorithmName.SHA256); // Hash algorithm for the certificate
        }
    }
}
