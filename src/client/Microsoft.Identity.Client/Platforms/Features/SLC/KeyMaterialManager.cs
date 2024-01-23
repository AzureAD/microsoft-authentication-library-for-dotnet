// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;
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
        // The name of the key guard isolation property
        private const string IsKeyGuardEnabledProperty = "Virtual Iso";

        // Field to store the current crypto key type
        private static CryptoKeyType s_cryptoKeyType = CryptoKeyType.None;

        // Constants specifying the names for the key storage provider and key names
        private const string KeyProviderName = "Microsoft Software Key Storage Provider";
        private const string MachineKeyName = "ManagedIdentityCredentialKey";
        private const string SoftwareKeyName = "ResourceBindingKey";

        // Cache the binding certificate across instances
        private static X509Certificate2 s_bindingCertificate;

        // Lock object for ensuring thread safety when accessing key information
        private readonly object _keyInfoLock = new();

        // Logger instance for capturing log information
        private readonly ILoggerAdapter _logger;

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
                ECDsaCng cngkey = GetCngKey();

                if (cngkey != null)
                {
                    s_bindingCertificate = CreateCngCertificate(cngkey);
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
        /// Initializes and retrieves the ECDsaCng key for Managed Identity.
        /// </summary>
        /// <returns>
        /// The initialized ECDsaCng key if successful, otherwise null.
        /// </returns>
        public ECDsaCng GetCngKey()
        {
            _logger.Verbose(() => "[Managed Identity] Initializing Cng Key.");

            // Try to get the key material from machine key
            if (TryGetKeyMaterial(KeyProviderName, MachineKeyName, CngKeyOpenOptions.MachineKey, out ECDsaCng eCDsaCng))
            {
                _logger.Verbose(() => $"[Managed Identity] A machine key was found. Key Name : {MachineKeyName}. ");
                return eCDsaCng;
            }

            // If machine key is not available, fall back to software key
            if (TryGetKeyMaterial(KeyProviderName, SoftwareKeyName, CngKeyOpenOptions.None, out eCDsaCng))
            {
                _logger.Verbose(() => $"[Managed Identity] A non-machine key was found. Key Name : {SoftwareKeyName}. ");
                return eCDsaCng;
            }

            s_cryptoKeyType = CryptoKeyType.None;

            // Both attempts failed, return null and do not alter the crypto key so it remains as none
            // Now we should follow the legacy managed identity flow
            _logger.Info("[Managed Identity] Machine / Software keys are not setup. " +
                "Proceed to check for legacy managed identity sources.");
            return null;
        }

        /// <summary>
        /// Attempts to retrieve cryptographic key material for a specified key name and provider.
        /// </summary>
        /// <param name="keyProviderName">The name of the key provider.</param>
        /// <param name="keyName">The name of the key.</param>
        /// <param name="cngKeyOpenOptions">The options for opening the CNG key.</param>
        /// <param name="eCDsaCng">The resulting ECDsaCng instance containing the key material.</param>
        /// <returns>
        ///   <c>true</c> if the key material is successfully retrieved; otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetKeyMaterial(
            string keyProviderName,
            string keyName,
            CngKeyOpenOptions cngKeyOpenOptions,
            out ECDsaCng eCDsaCng)
        {
            try
            {
                // Specify the optional flags for opening the key
                CngKeyOpenOptions options = cngKeyOpenOptions;
                options |= CngKeyOpenOptions.Silent;

                // Open the key with the specified options
                using (CngKey cngKey = CngKey.Open(keyName, new CngProvider(keyProviderName), options))
                {
                    DetermineKeyType(cngKey);

                    eCDsaCng = new ECDsaCng(cngKey);

                    return true;
                }
            }
            catch (CryptographicException ex)
            {
                // Check if the error message contains "Keyset does not exist"
                if (ex.Message.IndexOf("Keyset does not exist", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _logger.Info($"[Managed Identity] Key with name : {keyName} does not exist.");
                }
                else
                {
                    // Handle other cryptographic errors
                    _logger.Verbose(() => $"[Managed Identity] Exception caught during key operations. " +
                    $"Error Mesage : {ex.Message}.");
                }
            }

            eCDsaCng = null;
            return false;
        }

        /// <summary>
        /// Checks if the specified CNG key is protected by KeyGuard.
        /// </summary>
        /// <param name="cngKey">The CNG key to check for KeyGuard protection.</param>
        /// <returns>
        ///   <c>true</c> if the key is protected by KeyGuard; otherwise, <c>false</c>.
        /// </returns>
        public bool IsKeyGuardProtected(CngKey cngKey)
        {
            //Check to see if the KeyGuard Isolation flag was set in the key
            if (!cngKey.HasProperty(IsKeyGuardEnabledProperty, CngPropertyOptions.None))
            {
                return false;
            }

            //if key guard isolation flag exist, check for the key guard property value existence
            CngProperty property = cngKey.GetProperty(IsKeyGuardEnabledProperty, CngPropertyOptions.None);

            // Retrieve the key guard property value
            var keyGuardProperty = property.GetValue();

            // Check if the key guard property exists and has a non-zero value
            if (keyGuardProperty != null && keyGuardProperty.Length > 0)
            {
                if (keyGuardProperty[0] != 0)
                {
                    // KeyGuard key is available; set the cryptographic key type accordingly
                    _logger.Info("[Managed Identity] KeyGuard key is available. ");
                    s_cryptoKeyType = CryptoKeyType.KeyGuard;
                    return true;
                }
            }

            // KeyGuard key is not available
            return false;
        }

        /// <summary>
        /// Determines the cryptographic key type based on the characteristics of the specified CNG key.
        /// </summary>
        /// <param name="cngKey">The CNG key for which to determine the cryptographic key type.</param>
        private void DetermineKeyType(CngKey cngKey)
        {
            switch (true)
            {
                case var _ when cngKey.IsMachineKey:
                    s_cryptoKeyType = CryptoKeyType.Machine;
                    // Determine whether the key is KeyGuard protected
                    _ = IsKeyGuardProtected(cngKey);
                    break;

                case var _ when !cngKey.IsEphemeral && !cngKey.IsMachineKey:
                    s_cryptoKeyType = CryptoKeyType.User;
                    break;

                case var _ when cngKey.IsEphemeral:
                    s_cryptoKeyType = CryptoKeyType.Ephemeral;
                    break;

                default:
                    // Handle other cases if needed
                    s_cryptoKeyType = CryptoKeyType.InMemory;
                    break;
            }
        }

        /// <summary>
        /// Creates a binding certificate with a CNG key for use in Managed Identity scenarios.
        /// </summary>
        /// <param name="eCDsaCngKey">The CNG key used for creating the certificate.</param>
        /// <returns>The created binding certificate.</returns>
        private X509Certificate2 CreateCngCertificate(ECDsaCng eCDsaCngKey)
        {
            string certSubjectname = eCDsaCngKey.Key.KeyName;

            try
            {
                lock (_keyInfoLock) // Lock to ensure thread safety
                {
                    _logger.Verbose(() => "[Managed Identity] Creating binding certificate " +
                    "with CNG key for credential endpoint.");

                    // Create a certificate request
                    CertificateRequest request = CreateCertificateRequest(certSubjectname, eCDsaCngKey);

                    // Create a self-signed X.509 certificate
                    DateTimeOffset startDate = DateTimeOffset.UtcNow;
                    DateTimeOffset endDate = startDate.AddYears(5); //expiry 

                    //Create the self signed cert
                    X509Certificate2 selfSigned = request.CreateSelfSigned(startDate, endDate);

                    //create the cert with just the public key
                    X509Certificate2 publicKeyOnlyCertificate = new X509Certificate2(selfSigned.Export(X509ContentType.Cert));

                    //now copy the private key to the cert
                    //this is needed for mtls schannel to work with in-memory certificates
                    X509Certificate2 authCertificate = AssociatePrivateKeyInfo(publicKeyOnlyCertificate, eCDsaCngKey);

                    _logger.Verbose(() => $"[Managed Identity] Binding certificate (with cng key) created successfully. Has Private Key ? : {authCertificate.HasPrivateKey}");

                    return authCertificate;
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
        private CertificateRequest CreateCertificateRequest(string subjectName, ECDsaCng ecdsaKey)
        {
            CertificateRequest certificateRequest = null;

            _logger.Verbose(() => "[Managed Identity] Creating certificate request for the binding certificate.");

            return certificateRequest = new CertificateRequest(
                    $"CN={subjectName}", // Common Name 
                    ecdsaKey, // ECDsa key
                    HashAlgorithmName.SHA256); // Hash algorithm for the certificate
        }

        /// <summary>
        /// Associates the private key information with the provided public key-only certificate.
        /// </summary>
        /// <param name="publicKeyOnlyCertificate">The public key-only certificate.</param>
        /// <param name="eCDsaCngKey">The ECDsa key used for associating the private key.</param>
        /// <returns>The certificate with the private key information associated.</returns>
        private X509Certificate2 AssociatePrivateKeyInfo(X509Certificate2 publicKeyOnlyCertificate, ECDsaCng eCDsaCngKey)
        {
            _logger.Verbose(() => "[Managed Identity] Associating private key with the binding certificate.");

            // Copy the private key information to the public key-only certificate
            return publicKeyOnlyCertificate.CopyWithPrivateKey(eCDsaCngKey);
        }
    }
}
