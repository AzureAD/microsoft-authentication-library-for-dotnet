// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

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
        internal const string KeyProviderName = "Microsoft Software Key Storage Provider";
        internal const string KeyName = "ManagedIdentityCredentialKey";
        private readonly ECDsaCng _eCDsaCngKey;

        public KeyMaterialManager()
        {
            _eCDsaCngKey = GetCngKey(KeyProviderName, KeyName);
        }

        public CryptoKeyType CryptoKeyType => s_cryptoKeyType;

        public ECDsaCng ECDsaCngKey => _eCDsaCngKey;

        public ECDsaCng GetCngKey(string keyProviderName, string keyName)
        {
            // Try to get the key material from machine key
            if (TryGetKeyMaterial(keyProviderName, keyName, CngKeyOpenOptions.MachineKey, out ECDsaCng eCDsaCng))
            {
                return eCDsaCng;
            }

            // If machine key is not available, fall back to software key
            if (TryGetKeyMaterial(keyProviderName, keyName, CngKeyOpenOptions.None, out eCDsaCng))
            {
                return eCDsaCng;
            }

            // Both attempts failed, return null
            return null;
        }

        private bool TryGetKeyMaterial(
            string keyProviderName, string keyName, 
            CngKeyOpenOptions cngKeyOpenOptions, out ECDsaCng eCDsaCng)
        {
            try
            {
                // Specify the optional flags for opening the key
                CngKeyOpenOptions options = cngKeyOpenOptions;
                options |= CngKeyOpenOptions.Silent;

                // Open the key with the specified options
                using (CngKey cngKey = CngKey.Open(keyName, new CngProvider(keyProviderName), options))
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

                    eCDsaCng = new ECDsaCng(cngKey);

                    return true;
                }
            }
            catch (CryptographicException ex)
            {

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
                    s_cryptoKeyType = CryptoKeyType.KeyGuard;
                    return true;
                }
            }

            return false;
        }
    }
}
