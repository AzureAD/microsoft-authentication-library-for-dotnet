// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Platforms.Features.KeyMaterial
{
    /// <summary>
    /// Class to store crypto key information for a Managed Identity supported Azure resource.
    /// For more details see https://aka.ms/msal-net-managed-identity
    /// </summary>
    internal class KeyMaterialManager : IKeyMaterialManager
    {
        private const string IsKeyGuardEnabledProperty = "Virtual Iso";
        private static CryptoKeyType s_cryptoKeyType = CryptoKeyType.None;
        private const string KeyProviderName = "Microsoft Software Key Storage Provider";
        private const string MachineKeyName = "ManagedIdentityCredentialKey";
        private const string SoftwareKeyName = "ResourceBindingKey";
        private readonly ECDsaCng _eCDsaCngKey;
        private readonly ILoggerAdapter _logger;

        public KeyMaterialManager(ILoggerAdapter logger)
        {
            _logger = logger;
            _eCDsaCngKey = GetCngKey();
        }

        public CryptoKeyType CryptoKeyType => s_cryptoKeyType;

        public ECDsaCng CredentialKey => _eCDsaCngKey;

        public ECDsaCng GetCngKey()
        {
            _logger.Info("[Managed Identity] Trying to get a key from the key providers. ");
            return InitializeCngKey();
        }

        private ECDsaCng InitializeCngKey()
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

        public bool IsKeyGuardProtected(CngKey cngKey)
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
                    s_cryptoKeyType = CryptoKeyType.KeyGuard;
                    return true;
                }
            }

            return false;
        }

        public void DetermineKeyType(CngKey cngKey)
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
    }
}
