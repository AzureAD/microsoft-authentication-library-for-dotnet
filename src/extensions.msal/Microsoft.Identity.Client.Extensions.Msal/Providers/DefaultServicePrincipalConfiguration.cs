// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    internal class DefaultServicePrincipalConfiguration : IServicePrincipalConfiguration
    {
        private readonly IConfiguration _config;

        public DefaultServicePrincipalConfiguration(IConfiguration config)
        {
            _config = config;
        }

        public string ClientId => _config.GetValue<string>(Constants.AzureClientIdEnvName);

        public string CertificateBase64 => _config.GetValue<string>(Constants.AzureCertificateEnvName);

        public string CertificateThumbprint => _config.GetValue<string>(Constants.AzureCertificateThumbprintEnvName);

        public string CertificateStoreName => _config.GetValue<string>(Constants.AzureCertificateStoreEnvName);

        public string TenantId => _config.GetValue<string>(Constants.AzureTenantIdEnvName);

        public string ClientSecret => _config.GetValue<string>(Constants.AzureClientSecretEnvName);

        public string CertificateStoreLocation => _config.GetValue<string>(Constants.AzureCertificateStoreLocationEnvName);

        public string CertificateSubjectDistinguishedName => _config.GetValue<string>(Constants.AzureCertificateSubjectDistinguishedNameEnvName);

        public string Authority => string.IsNullOrWhiteSpace(
            _config.GetValue<string>(Constants.AadAuthorityEnvName)) ?
            AadAuthority.DefaultTrustedHost :
            _config.GetValue<string>(Constants.AadAuthorityEnvName);
    }
}
