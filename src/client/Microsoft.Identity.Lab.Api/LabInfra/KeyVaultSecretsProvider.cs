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
    /// <summary>
    /// KeyVaultInstance defines the available Key Vault instances that can be used for retrieving secrets and certificates in lab tests.
    /// </summary>
    public class KeyVaultInstance
    {
        /// <summary>
        /// This Key Vault is generally used for frequently rotated credentials and other sensitive configuration.
        /// </summary>
        public const string MSIDLab = "https://msidlabs.vault.azure.net";

        /// <summary>
        /// This KeyVault is generally used for static user/app/etc. info and other low-risk configuration.
        /// </summary>
        public const string MsalTeam = "https://id4skeyvault.vault.azure.net/";
    }

    /// <summary>
    /// KeyVaultSecretsProvider provides methods for retrieving secrets and certificates from Azure Key Vault instances used in lab tests.
    /// </summary>
    public class KeyVaultSecretsProvider : IDisposable
    {
        private CertificateClient _certificateClient;
        private SecretClient _secretClient;

        
        /// <summary>
        /// Initializes a new instance of the KeyVaultSecretsProvider class using the specified Azure Key Vault address.
        /// </summary>
        /// <remarks>This constructor establishes connections to both the certificate and secret clients
        /// for the specified Key Vault. Ensure that the provided address is accessible and that the application has
        /// appropriate permissions to access the Key Vault resources.</remarks>
        /// <param name="keyVaultAddress">The address of the Azure Key Vault to connect to. If not specified, the default address defined by
        /// KeyVaultInstance.MSIDLab is used. Must be a valid URI.</param>
        public KeyVaultSecretsProvider(string keyVaultAddress = KeyVaultInstance.MSIDLab)
        {
            var credentials = GetKeyVaultCredentialAsync().GetAwaiter().GetResult();
            var keyVaultAddressUri = new Uri(keyVaultAddress);
            _certificateClient = new CertificateClient(keyVaultAddressUri, credentials);
            _secretClient = new SecretClient(keyVaultAddressUri, credentials);
        }

        /// <summary>
        /// KeyVaultSecretsProvider finalizer to ensure that resources are released if Dispose is not called explicitly.
        /// </summary>
        ~KeyVaultSecretsProvider()
        {
            Dispose();
        }

        /// <summary>
        /// Gets a secret from Azure Key Vault by its name. This method retrieves the latest version of the secret with the specified name.
        /// </summary>
        /// <param name="secretName">The name of the secret to retrieve.</param>
        /// <returns>The secret with the specified name.</returns>
        public KeyVaultSecret GetSecretByName(string secretName)
        {
            return _secretClient.GetSecret(secretName).Value;
        }

        /// <summary>
        /// Gets a secret from Azure Key Vault by its name and version. This method retrieves the specified version of the secret with the given name.
        /// </summary>
        /// <param name="secretName">The name of the secret to retrieve.</param>
        /// <param name="secretVersion">The version of the secret to retrieve.</param>
        /// <returns>The secret with the specified name and version.</returns>
        public KeyVaultSecret GetSecretByName(string secretName, string secretVersion)
        {
            return _secretClient.GetSecret(secretName, secretVersion).Value;
        }

        /// <summary>
        /// Gets a secret from Azure Key Vault by its name asynchronously. This method retrieves the latest version of the secret with the specified name.
        /// </summary>
        /// <param name="secretName"></param>
        /// <returns></returns>
        public async Task<KeyVaultSecret> GetSecretByNameAsync(string secretName)
        {
            var response = await _secretClient.GetSecretAsync(secretName).ConfigureAwait(false);
            return response.Value;
        }

        /// <summary>
        /// Gets a secret from Azure Key Vault by its name and version asynchronously. This method retrieves the specified version of the secret with the given name.
        /// </summary>
        /// <param name="secretName">The name of the secret to retrieve.</param>
        /// <param name="secretVersion">The version of the secret to retrieve.</param>
        /// <returns>The secret with the specified name and version.</returns>
        public async Task<KeyVaultSecret> GetSecretByNameAsync(string secretName, string secretVersion)
        {
            var response = await _secretClient.GetSecretAsync(secretName, secretVersion).ConfigureAwait(false);
            return response.Value;
        }

        /// <summary>
        /// Gets a certificate with its private material from Azure Key Vault by its name asynchronously. This method retrieves the latest version of the certificate with the specified name, including its private key material.
        /// </summary>
        /// <param name="certName">The name of the certificate to retrieve.</param>
        /// <returns>The certificate with the specified name, including its private key material.</returns>
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

        /// <summary>
        /// Disposes of the KeyVaultSecretsProvider instance and releases any resources used by the certificate and secret clients. This method should be called when the KeyVaultSecretsProvider is no longer needed to ensure that all resources are properly released.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
