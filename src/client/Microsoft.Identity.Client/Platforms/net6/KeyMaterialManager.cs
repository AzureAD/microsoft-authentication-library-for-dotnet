// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.Platforms.netcore
{
    /// <summary>
    /// Class to store crypto key information for a Managed Identity supported Azure resource.
    /// For more details see https://aka.ms/msal-net-managed-identity
    /// </summary>
    internal class KeyMaterialManager(ILoggerAdapter logger) : IKeyMaterialManager
    {
        private const string IsKeyGuardEnabledProperty = "Virtual Iso";
        private CryptoKeyType _cryptoKeyType = CryptoKeyType.None;
        private const string KeyProviderName = "Microsoft Software Key Storage Provider";
        private const string MachineKeyName = "ManagedIdentityCredentialKey";
        private const string SoftwareKeyName = "ResourceBindingKey";
        private static X509Certificate2 s_bindingCertificate;
        private readonly object _keyInfoLock = new(); // Lock object
        private readonly ILoggerAdapter _logger = logger;

        public CryptoKeyType CryptoKeyType => _cryptoKeyType;

        public X509Certificate2 BindingCertificate => CreateCertificateFromCryptoKeyInfo();

        private static bool CertificateNeedsRotation(X509Certificate2 certificate, double rotationPercentageThreshold = 70)
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

        private X509Certificate2 CreateCertificateFromCryptoKeyInfo()
        {
            lock (_keyInfoLock) // Lock to ensure thread safety
            {
                //if cached cert exist and still valid return it
                if (s_bindingCertificate != null && !CertificateNeedsRotation(s_bindingCertificate))
                {
                    _logger.Verbose(() => "[Managed Identity] A cached binding certificate is available.");
                    return s_bindingCertificate;
                }

                //delete the cached cert if it needs to be rotated
                if (s_bindingCertificate != null && CertificateNeedsRotation(s_bindingCertificate))
                {
                    // Delete the cached certificate if it is expired
                    s_bindingCertificate.Dispose();
                    s_bindingCertificate = null;
                }
            }

            ECDsaCng cngkey = GetCngKey();

            if (cngkey != null)
            {
                return CreateCngCertificate(cngkey);
            }

            return null;
        }

        private ECDsaCng GetCngKey()
        {
            _logger.Verbose(() => "[Managed Identity] Initializing Cng Key.");

            // Try to get the key material from machine key
            if (TryGetKeyMaterial(KeyProviderName, MachineKeyName, CngKeyOpenOptions.MachineKey, out ECDsaCng eCDsaCng))
            {
                _logger.Verbose(() => "[Managed Identity] A machine key was found.");
                return eCDsaCng;
            }

            // If machine key is not available, fall back to software key
            if (TryGetKeyMaterial(KeyProviderName, SoftwareKeyName, CngKeyOpenOptions.None, out eCDsaCng))
            {
                _logger.Verbose(() => "[Managed Identity] A non-machine key was found.");
                return eCDsaCng;
            }

            // Both attempts failed, return null
            _logger.Info("[Managed Identity] Machine / Software keys are not setup. ");
            return null;
        }

        private bool TryGetKeyMaterial(
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
                _logger.Verbose(() => $"[Managed Identity] Exception caught during key operations. " +
                $"Error Mesage : { ex.Message }.");
            }

            eCDsaCng = null;
            return false;
        }

        private bool IsKeyGuardProtected(CngKey cngKey)
        {
            //Check to see if the KeyGuard Isolation flag was set in the key
            if (!cngKey.HasProperty(IsKeyGuardEnabledProperty, CngPropertyOptions.None))
            {
                return false;
            }

            //if key guard isolation flag exist, check for the key guard property value existence
            CngProperty property = cngKey.GetProperty(IsKeyGuardEnabledProperty, CngPropertyOptions.None);

            var keyGuardProperty = property.GetValue();

            if (keyGuardProperty != null && keyGuardProperty.Length > 0)
            {
                if (keyGuardProperty[0] != 0)
                {
                    _logger.Info("[Managed Identity] KeyGuard key is available. ");
                    _cryptoKeyType = CryptoKeyType.KeyGuard;
                    return true;
                }
            }

            return false;
        }

        private void DetermineKeyType(CngKey cngKey)
        {
            switch (true)
            {
                case var _ when cngKey.IsMachineKey:
                    _cryptoKeyType = CryptoKeyType.Machine;
                    // Determine whether the key is KeyGuard protected
                    _ = IsKeyGuardProtected(cngKey);
                    break;

                case var _ when !cngKey.IsEphemeral && !cngKey.IsMachineKey:
                    _cryptoKeyType = CryptoKeyType.User;
                    break;

                case var _ when cngKey.IsEphemeral:
                    _cryptoKeyType = CryptoKeyType.Ephemeral;
                    break;

                default:
                    // Handle other cases if needed
                    _cryptoKeyType = CryptoKeyType.InMemory;
                    break;
            }
        }

        private X509Certificate2 CreateCngCertificate(ECDsaCng eCDsaCngKey)
        {
            string certSubjectname = eCDsaCngKey.Key.KeyName;

            try
            {
                lock (_keyInfoLock) // Lock to ensure thread safety
                {
                    _logger.Verbose(() => "[Managed Identity] Creating binding certificate with CNG key for credential endpoint.");

                    // Create a certificate request
                    CertificateRequest request = CreateCertificateRequest(certSubjectname, eCDsaCngKey);

                    // Create a self-signed X.509 certificate
                    DateTimeOffset startDate = DateTimeOffset.UtcNow;
                    DateTimeOffset endDate = startDate.AddYears(2); //expiry 

                    //Create the self signed cert
                    X509Certificate2 selfSigned = request.CreateSelfSigned(startDate, endDate);

                    //create the cert with just the public key
                    X509Certificate2 publicKeyOnlyCertificate = new X509Certificate2(selfSigned.Export(X509ContentType.Cert));

                    //now copy the private key to the cert
                    //this is needed for mtls schannel to work with in-memory certificates
                    X509Certificate2 authCertificate = AssociatePrivateKeyInfo(publicKeyOnlyCertificate, eCDsaCngKey);

                    _logger.Verbose(() => "[Managed Identity] Binding certificate (with cng key) created successfully.");

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

        private CertificateRequest CreateCertificateRequest(string subjectName, ECDsaCng ecdsaKey)
        {
            CertificateRequest certificateRequest = null;

            _logger.Verbose(() => "[Managed Identity] Creating certificate request for the binding certificate.");

            return certificateRequest = new(
                    $"CN={subjectName}", // Common Name 
                    ecdsaKey, // ECDsa key
                    HashAlgorithmName.SHA256); // Hash algorithm for the certificate
        }

        private X509Certificate2 AssociatePrivateKeyInfo(X509Certificate2 publicKeyOnlyCertificate, ECDsaCng eCDsaCngKey)
        {
            _logger.Verbose(() => "[Managed Identity] Associating private key with the binding certificate.");
            return publicKeyOnlyCertificate.CopyWithPrivateKey(eCDsaCngKey);
        }
    }
}
