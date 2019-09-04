// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    /// <summary>
    ///     Constants for environment variables, application settings and credential lookups
    /// </summary>
    public static class Constants
    {
        /// <summary>
        ///     AzureTenantIdEnvName is the name of the environment variable which holds a string representation for a GUID,
        ///     which is the ID of the tenant where the account resides
        /// </summary>
        public static readonly string AzureTenantIdEnvName = "AZURE_TENANT_ID";

        /// <summary>
        ///     AzureClientIdEnvName is the name of the environment variable which holds a string representation for a GUID
        ///     ClientId (application ID) of the application
        /// </summary>
        public static readonly string AzureClientIdEnvName = "AZURE_CLIENT_ID";

        /// <summary>
        ///     AzureClientSecretEnvName is the name of the environment variable which holds a string representation of the
        ///     secret for an application
        /// </summary>
        public static readonly string AzureClientSecretEnvName = "AZURE_CLIENT_SECRET";

        /// <summary>
        ///     AzurePreferredAccountUsernameEnvName is the username of an account in the Shared Token Cache. If there
        ///     have been multiple accounts logged in via Microsoft developer tools, this will filter the accounts to
        ///     use a specific username rather than selecting the first.
        /// </summary>
        public static readonly string AzurePreferredAccountUsernameEnvName = "AZURE_PREFERRED_ACCOUNT_USERNAME";

        /// <summary>
        ///     AzureCertificateEnvName is the name of the environment variable which holds a base64 encoded string
        ///     representation of an X509 certificate which is the secret for an application
        /// </summary>
        public static readonly string AzureCertificateEnvName = "AZURE_CERTIFICATE";

        /// <summary>
        ///     AzureCertificateThumbprintEnvName is the name of the environment variable which holds a string
        ///     representation of the thumbprint of a certificate stored in the Windows Certificate Store
        /// </summary>
        public static readonly string AzureCertificateThumbprintEnvName = "AZURE_CERTIFICATE_THUMBPRINT";

        /// <summary>
        /// AzureCertificateSubjectDistinguishedNameEnvName is the name of the environment variable which holds the
        /// subject distinguished name of a certificate stored in the Windows Certificate Store
        /// </summary>
        public static readonly string AzureCertificateSubjectDistinguishedNameEnvName = "AZURE_CERTIFICATE_SUBJECT_NAME";

        /// <summary>
        ///     AzureCertificateStoreEnvName is the name of the environment variable which holds the name of the certificate
        ///     store
        /// </summary>
        public static readonly string AzureCertificateStoreEnvName = "AZURE_CERTIFICATE_STORE";

        /// <summary>
        ///     AzureCertificateStoreLocationEnvName is the name of the environment variable which holds the location of the
        ///     certificate store (CurrentUser or LocalMachine)
        /// </summary>
        public static readonly string AzureCertificateStoreLocationEnvName = "AZURE_CERTIFICATE_STORE_LOCATION";

        /// <summary>
        /// API Version used for the Managed Identity metadata endpoint on AppService
        /// </summary>
        public static readonly string ManagedIdentityAppServiceApiVersion = "2017-09-01";

        /// <summary>
        /// API Version used for the Managed Identity metadata endpoint on Virtual Machines
        /// </summary>
        public static readonly string ManagedIdentityVMApiVersion = "2018-02-01";

        /// <summary>
        /// Managed Identity loopback IP address
        /// </summary>
        public static readonly string ManagedIdentityLoopbackAddress = "169.254.169.254";

        /// <summary>
        /// Managed Identity metadata endpoint
        /// </summary>
        public static readonly string ManagedIdentityMetadataEndpoint = $"http://{ManagedIdentityLoopbackAddress}/metadata";

        /// <summary>
        /// Managed Identity
        /// </summary>
        public static readonly string ManagedIdentityTokenEndpoint = $"{ManagedIdentityMetadataEndpoint}/identity/oauth2/token";

        /// <summary>
        /// ManagedIdentityEndpointEnvName is the name of the environment variable which holds the endpoint for the
        /// managed identity service running in Azure AppService
        /// </summary>
        public static readonly string ManagedIdentityEndpointEnvName = "MSI_ENDPOINT";

        /// <summary>
        /// ManagedIdentitySecretEnvName is the name of the environment variable which holds the secret for use in
        /// calling the managed identity service running in Azure AppService
        /// </summary>
        public static readonly string ManagedIdentitySecretEnvName = "MSI_SECRET";

        /// <summary>
        /// AadAuthorityEnvName is the name of the environment variable which holds the hostname of the service token service (STS) from which MSAL.NET will acquire the tokens. Ex. login.microsoftonline.com
        /// </summary>
        public static readonly string AadAuthorityEnvName = "AAD_AUTHORITY";

        /// <summary>
        /// AzureResourceManagerDefaultScope is the default scope for Azure Resource Manager
        /// </summary>
        public static readonly string AzureResourceManagerResourceUri = @"https://management.azure.com/";

        /// <summary>
        /// AzureResourceManagerDefaultScope is the default scope for Azure Resource Manager
        /// </summary>
        public static readonly string AzureResourceManagerDefaultScope = $"{AzureResourceManagerResourceUri}.default";
    }
}
