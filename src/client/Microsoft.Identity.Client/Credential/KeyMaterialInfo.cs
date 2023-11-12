// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.Identity.Client.Credential
{
    /// <summary>
    /// Class to store crypto key information for a Managed Identity supported Azure resource.
    /// For more details see https://aka.ms/msal-net-managed-identity
    /// </summary>
    internal class KeyMaterialInfo : IKeyMaterialInfo
    {
        private const string IsKeyGuardEnabledProperty = "Virtual Iso";
        internal static readonly string s_credentialEndpoint = "http://169.254.169.254/metadata/identity/credential?cred-api-version=1.0";
        private static CryptoKeyType s_cryptoKeyType = CryptoKeyType.None;
        internal const string KeyProviderName = "Microsoft Software Key Storage Provider";
        internal const string KeyName = "ManagedIdentityCredentialKey";
        private readonly ECDsaCng _eCDsaCngKey;

        public KeyMaterialInfo()
        {
            _eCDsaCngKey = GetMachineKey(KeyProviderName, KeyName);
        }

        public CryptoKeyType CryptoKeyType => s_cryptoKeyType;

        public ECDsaCng ECDsaCngKey => _eCDsaCngKey;

        public string CredentialEndpoint = s_credentialEndpoint;

        public ECDsaCng GetMachineKey(string keyProviderName, string keyName)
        {
            try
            {
                // Specify the optional flags for opening the key
                CngKeyOpenOptions options = CngKeyOpenOptions.MachineKey;
                options |= CngKeyOpenOptions.Silent;

                // Open the key with the specified options
                using (CngKey cngKey = CngKey.Open(keyName, new CngProvider(keyProviderName), options))
                {
                    s_cryptoKeyType = CryptoKeyType.Machine;

                    // Determine whether the key is KeyGuard protected
                    _ = IsKeyGuardProtected(cngKey);

                    return new ECDsaCng(cngKey);
                }
            }
            catch (CryptographicException ex)
            {

            }

            return null;
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
                    s_cryptoKeyType = CryptoKeyType.KeyGuard;
                    return true;
                }
            }

            return false;
        }
    }
}
