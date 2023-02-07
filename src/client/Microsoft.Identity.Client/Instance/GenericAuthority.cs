// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.Identity.Client.Instance
{
    internal class GenericAuthority : Authority
    {
        internal const string OpenIdConfigurationEndpointSuffix = ".well-known/openid-configuration";
        private static readonly ConcurrentDictionary<string, OpenIdConnectConfiguration> s_openIdConnectConfigurations = new();
           
        internal GenericAuthority(AuthorityInfo authorityInfo)
            : base(authorityInfo)
        {

        }

        internal override string TenantId => null;
    
        internal IDocumentRetriever DocumentRetriever { get; set; } = new HttpDocumentRetriever();

        internal override string GetTenantedAuthority(string tenantId, bool forceTenantless = false)
        {
            throw new NotImplementedException();
        }

        internal override string GetTokenEndpoint()
        {
            string cacheKey = AuthorityInfo.CanonicalAuthority.ToString();
            var configuration = s_openIdConnectConfigurations.GetOrAdd(cacheKey, RetrieveOpenIdConnectConfiguration);
            return configuration.TokenEndpoint;
        }

        private OpenIdConnectConfiguration RetrieveOpenIdConnectConfiguration(string canonicalizedAuthority)
        {
            var configuration = OpenIdConnectConfigurationRetriever.GetAsync(AuthorityInfo.DiscoveryDocumentAddress, DocumentRetriever, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            return configuration;
        }

        internal override string GetAuthorizationEndpoint()
        {
            // prevents authorization_code flow which requires knowledge of the authorization_endpoint. 
            throw new NotImplementedException();
        }

        internal override string GetDeviceCodeEndpoint()
        {
            // prevents device_code flow which requires knowledge of the device_authorization_endpoint.
            throw new NotImplementedException();
        }
    }
}
