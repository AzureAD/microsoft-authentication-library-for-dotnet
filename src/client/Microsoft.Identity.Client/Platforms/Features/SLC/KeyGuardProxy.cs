using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Platforms.Features.SLC
{
    /// <summary>
    /// Platform / OS specific logic to manage KeyGuard keys.
    /// </summary>
    internal class KeyGuardProxy : IKeyGuardProxy
    {
        // The name of the key guard isolation property
        private const string IsKeyGuardEnabledProperty = "Virtual Iso";

        // The flag for using virtual isolation with CNG keys
        private const CngKeyCreationOptions NCryptUseVirtualIsolationFlag = (CngKeyCreationOptions)0x00020000;

        // Constants specifying the names for the key storage provider and key names
        private const string MachineKeyName = "ResourceBindingMachineCredentialKey";
        private const string SoftwareKeyName = "ResourceBindingUserCredentialKey";

        // Logger instance for capturing log information
        private readonly ILoggerAdapter _logger;

        /// <summary>
        /// cryptographic key type
        /// </summary>
        public CryptoKeyType CryptoKeyType { get; private set; } = CryptoKeyType.Undefined;

        internal KeyGuardProxy(ILoggerAdapter logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Loads a CngKey with the given key provider.
        /// </summary>
        /// <param name="keyProvider"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public ECDsa LoadCngKeyWithProvider(string keyProvider)
        {
            try
            {
                _logger.Verbose(() => "[Managed Identity] Initializing Cng Key.");

                // Try to get the key material from machine key
                if (TryGetCryptoKey(keyProvider, MachineKeyName, CngKeyOpenOptions.MachineKey, out ECDsa ecdsaKey))
                {
                    _logger.Verbose(() => $"[Managed Identity] A machine key was found. Key Name : {MachineKeyName}. ");
                    return ecdsaKey;
                }

                // If machine key is not available, fall back to software key
                if (TryGetCryptoKey(keyProvider, SoftwareKeyName, CngKeyOpenOptions.None, out ecdsaKey))
                {
                    _logger.Verbose(() => $"[Managed Identity] A software key was found. Key Name : {SoftwareKeyName}. ");
                    return ecdsaKey;
                }

                _logger.Info("[Managed Identity] Machine / Software keys are not setup. " +
                    "Attempting to create a new key for Managed Identity.");

                // Attempt to create a new key if none are available
                if (TryCreateKeyMaterial(SoftwareKeyName, out ecdsaKey))
                {
                    return ecdsaKey;
                }

                // All attempts for getting keys failed
                // Now we should follow the legacy managed identity flow
                _logger.Info("[Managed Identity] Machine / Software keys are not setup. " +
                    "Proceed to check for legacy managed identity sources.");

                return null;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it according to your error policy
                throw new InvalidOperationException("Failed to load CngKey.", ex);
            }
        }

        /// <summary>
        /// Attempts to retrieve cryptographic key material for a specified key name and provider.
        /// </summary>
        /// <param name="keyProviderName">The name of the key provider.</param>
        /// <param name="keyName">The name of the key.</param>
        /// <param name="cngKeyOpenOptions">The options for opening the CNG key.</param>
        /// <param name="ecdsaKey">The resulting key material.</param>
        /// <returns>
        ///   <c>true</c> if the key material is successfully retrieved; otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetCryptoKey(
            string keyProviderName,
            string keyName,
            CngKeyOpenOptions cngKeyOpenOptions,
            out ECDsa ecdsaKey)
        {
            try
            {
                // Specify the optional flags for opening the key
                CngKeyOpenOptions options = cngKeyOpenOptions;
                options |= CngKeyOpenOptions.Silent;

                // Open the key with the specified options
                var cngKey = CngKey.Open(keyName, new CngProvider(keyProviderName), options);
                ecdsaKey = new ECDsaCng(cngKey);

                //check if the key is protected by KeyGuard
                if (IsKeyGuardProtectedKey(cngKey))
                {
                    // Check if the key name indicates user-specific key
                    if (keyName.Equals(SoftwareKeyName, StringComparison.OrdinalIgnoreCase))
                    {
                        CryptoKeyType = CryptoKeyType.KeyGuardUser;
                    }
                    else
                    {
                        CryptoKeyType = CryptoKeyType.KeyGuardMachine;
                    }

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

            ecdsaKey = null;
            return false;
        }

        /// <summary>
        /// Checks if the specified CNG key is protected by KeyGuard.
        /// </summary>
        /// <param name="cngKey">The CNG key to check for KeyGuard protection.</param>
        /// <returns>
        ///   <c>true</c> if the key is protected by KeyGuard; otherwise, <c>false</c>.
        /// </returns>
        public bool IsKeyGuardProtectedKey(CngKey cngKey)
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
                    return true;
                }
            }

            // KeyGuard key is not available
            return false;
        }

        /// <summary>
        /// Attempts to create a new cryptographic key and load it into a CngKey with the specified options.
        /// </summary>
        /// <param name="keyName">The name of the key to create.</param>
        /// <param name="ecdsaKey">Output parameter that returns the created ECDsa key, if successful.</param>
        /// <returns>True if the key was created and loaded successfully, false otherwise.</returns>
        public bool TryCreateKeyMaterial(string keyName, out ECDsa ecdsaKey)
        {
            ecdsaKey = null;

            try
            {
                var keyParams = new CngKeyCreationParameters
                {
                    KeyUsage = CngKeyUsages.AllUsages,
                    Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                    KeyCreationOptions = NCryptUseVirtualIsolationFlag | CngKeyCreationOptions.OverwriteExistingKey,
                    ExportPolicy = CngExportPolicies.None
                };

                using var cngKey = CngKey.Create(CngAlgorithm.ECDsaP256, keyName, keyParams);
                ecdsaKey = new ECDsaCng(cngKey);
                _logger.Info($"[Managed Identity] Key '{keyName}' created successfully with Virtual Isolation.");
                CryptoKeyType = CryptoKeyType.KeyGuardUser;
                return true; // Key creation was successful
            }
            catch (Exception ex)
            {
                _logger.Error($"[Managed Identity] Failed to create user key '{keyName}': {ex.Message}");
                return false; // Key creation failed
            }
        }
    }
}
