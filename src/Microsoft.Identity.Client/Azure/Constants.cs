using System;

namespace Microsoft.Identity.Client.Azure
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
        public static readonly string AzureCertificateStoreLocation = "AZURE_CERTIFICATE_STORE_LOCATION";

        /// <summary>
        /// API Version used for the Managed Identity metadata endpoint on AppService
        /// </summary>
        public static readonly string ManagedIdentityAppServiceApiVersion = "2017-08-01";

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
        public static readonly string ManagedIdentityMetadataEndpoint = "http://" + ManagedIdentityLoopbackAddress + "/metadata";

        /// <summary>
        /// Managed Identity
        /// </summary>
        public static readonly string ManagedIdentityTokenEndpoint = ManagedIdentityMetadataEndpoint + "/identity/oauth2/token";

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
    }

    /// <summary>
    ///     Env provides access to well known environment variables
    /// </summary>
    public static class Env
    {
        /// <summary>
        ///     A string representation for a GUID, which is the ID of the tenant where the account resides environment variable
        /// </summary>
        public static string TenantId => Environment.GetEnvironmentVariable(Constants.AzureTenantIdEnvName);

        /// <summary>
        ///     A string representation for a GUID ClientId (application ID) of the application environment variable
        /// </summary>
        public static string ClientId => Environment.GetEnvironmentVariable(Constants.AzureClientIdEnvName);

        /// <summary>
        ///     A string representation of the secret for an application environment variable
        /// </summary>
        public static string ClientSecret => Environment.GetEnvironmentVariable(Constants.AzureClientSecretEnvName);

        /// <summary>
        ///     A base64 encoded string representation of an X509 certificate which is the secret for an application environment variable
        /// </summary>
        public static string CertificateBase64 => Environment.GetEnvironmentVariable(Constants.AzureCertificateEnvName);

        /// <summary>
        ///     A string representation of the thumbprint of a certificate stored in the Windows Certificate Store
        /// </summary>
        public static string CertificateThumbprint =>
            Environment.GetEnvironmentVariable(Constants.AzureCertificateThumbprintEnvName);

        /// <summary>
        /// The subject distinguished name of a certificate stored in the Windows Certificate Store
        /// </summary>
        public static string CertificateSubjectDistinguishedName =>
            Environment.GetEnvironmentVariable(Constants.AzureCertificateSubjectDistinguishedNameEnvName);

        /// <summary>
        ///     The name of the certificate store
        /// </summary>
        public static string CertificateStoreName =>
            Environment.GetEnvironmentVariable(Constants.AzureCertificateStoreEnvName);

        /// <summary>
        ///     The location of the certificate store (CurrentUser or LocalMachine) environment variable
        /// </summary>
        public static string CertificateStoreLocation =>
            Environment.GetEnvironmentVariable(Constants.AzureCertificateStoreLocation);

        /// <summary>
        /// ManagedIdentityEndpoint is the value of the managed identity endpoint environment variable
        /// </summary>
        public static string ManagedIdentityEndpoint =>
            Environment.GetEnvironmentVariable(Constants.ManagedIdentityEndpointEnvName);

        /// <summary>
        /// ManagedIdentitySecret is the value of the managed identity secret environment variable
        /// </summary>
        public static string ManagedIdentitySecret =>
            Environment.GetEnvironmentVariable(Constants.ManagedIdentitySecretEnvName);

        /// <summary>
        /// AadAuthority is the hostname of the security token service (STS) from which MSAL.NET will acquire the tokens. Ex. login.microsoftonline.com
        /// </summary>
        public static string AadAuthority => Environment.GetEnvironmentVariable(Constants.AadAuthorityEnvName);
    }
}
