// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public class KeyVaultInstance
    {
        /// <summary>
        /// The KeyVault maintained by the MSID. It is recommended for use. 
        /// </summary>
        public const string MSIDLab = "https://msidlabs.vault.azure.net";

        /// <summary>
        /// The KeyVault maintained by the MSAL.NET team and have full control over. 
        /// Should be used temporarily - secrets should be stored and managed by MSID Lab.
        /// </summary>
        public const string MsalTeam = "https://buildautomation.vault.azure.net/";
    }

    public class KeyVaultSecretsProvider : IDisposable
    {
        private CertificateClient _certificateClient;
        private SecretClient _secretClient;

        /// <summary>Initialize the secrets provider with the "keyVault" configuration section.</summary>
        /// <remarks>
        /// <para>
        /// Authentication using <see cref="LabAccessAuthenticationType.ClientCertificate"/>
        ///     1. Register Azure AD application of "Web app / API" type.
        ///        To set up certificate based access to the application PowerShell should be used.
        ///     2. Add an access policy entry to target Key Vault instance for this application.
        ///
        ///     The "keyVault" configuration section should define:
        ///         "authType": "ClientCertificate"
        ///         "clientId": [client ID]
        ///         "certThumbprint": [certificate thumbprint]
        /// </para>
        /// <para>
        /// Authentication using <see cref="LabAccessAuthenticationType.UserCredential"/>
        ///     1. Register Azure AD application of "Native" type.
        ///     2. Add to 'Required permissions' access to 'Azure Key Vault (AzureKeyVault)' API.
        ///     3. When you run your native client application, it will automatically prompt user to enter Azure AD credentials.
        ///     4. To successfully access keys/secrets in the Key Vault, the user must have specific permissions to perform those operations.
        ///        This could be achieved by directly adding an access policy entry to target Key Vault instance for this user
        ///        or an access policy entry for an Azure AD security group of which this user is a member of.
        ///
        ///     The "keyVault" configuration section should define:
        ///         "authType": "UserCredential"
        ///         "clientId": [client ID]
        /// </para>
        /// </remarks>
        public KeyVaultSecretsProvider(string keyVaultAddress = KeyVaultInstance.MSIDLab)
        {
            var credentials = GetKeyVaultCredentialAsync().GetAwaiter().GetResult();
            var keyVaultAddressUri = new Uri(keyVaultAddress);
            _certificateClient = new CertificateClient(keyVaultAddressUri, credentials);
            _secretClient = new SecretClient(keyVaultAddressUri, credentials);
        }

        ~KeyVaultSecretsProvider()
        {
            Dispose();
        }
      
        public KeyVaultSecret GetSecretByName(string secretName)
        {
            return _secretClient.GetSecret(secretName).Value;
        }

        public KeyVaultSecret GetSecretByName(string secretName, string secretVersion)
        {
            return _secretClient.GetSecret(secretName, secretVersion).Value;
        }

        public async Task<X509Certificate2> GetCertificateWithPrivateMaterialAsync(string certName)
        {
            return await _certificateClient.DownloadCertificateAsync(certName).ConfigureAwait(false);
        }

        private async Task<TokenCredential> GetKeyVaultCredentialAsync()
        {
            var accessToken = await LabAuthenticationHelper.GetLabAccessTokenAsync(
                "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/",
                new[] { "https://vault.azure.net/.default" }).ConfigureAwait(false);
            return DelegatedTokenCredential.Create((_, __) => accessToken);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
