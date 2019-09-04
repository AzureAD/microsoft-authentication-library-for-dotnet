// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    internal class DefaultManagedIdentityConfiguration : IManagedIdentityConfiguration
    {
        private readonly IConfiguration _config;

        public DefaultManagedIdentityConfiguration(IConfiguration config)
        {
            _config = config;
        }

        public string ManagedIdentitySecret => _config.GetValue<string>(Constants.ManagedIdentitySecretEnvName);

        public string ManagedIdentityEndpoint => _config.GetValue<string>(Constants.ManagedIdentityEndpointEnvName);

        public string ClientId => _config.GetValue<string>(Constants.AzureClientIdEnvName);
    }
}
