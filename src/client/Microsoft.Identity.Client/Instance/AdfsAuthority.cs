// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    internal class AdfsAuthority : Authority
    {
        private const string TokenEndpointTemplate = "{0}oauth2/token";
        private const string AuthorizationEndpointTemplate = "{0}oauth2/authorize";
        private const string DeviceCodeEndpointTemplate = "{0}oauth2/devicecode";

        public AdfsAuthority(AuthorityInfo authorityInfo)
            : base(authorityInfo)
        {
        }

        //ADFS does not have a concept of a tenant ID. This prevents ADFS from supporting multiple tenants

        internal override string GetTenantedAuthority(string tenantId, bool forceTenantless = false)
        {
            return AuthorityInfo.CanonicalAuthority.ToString();
        }

        internal override Task<string> GetTokenEndpointAsync(RequestContext requestContext)
        {
            string tokenEndpoint = string.Format(
                              CultureInfo.InvariantCulture,
                              TokenEndpointTemplate,
                             AuthorityInfo.CanonicalAuthority);
            return Task.FromResult(tokenEndpoint);
        }

        internal override Task<string> GetAuthorizationEndpointAsync(RequestContext requestContext)
        {
            string authEndpoint = string.Format(CultureInfo.InvariantCulture,
                    AuthorizationEndpointTemplate,
                    AuthorityInfo.CanonicalAuthority);

            return Task.FromResult(authEndpoint);

        }

        internal override Task<string> GetDeviceCodeEndpointAsync(RequestContext requestContext)
        {
            string deviceEndpoint = string.Format(
                  CultureInfo.InvariantCulture,
                  DeviceCodeEndpointTemplate,
                  AuthorityInfo.CanonicalAuthority);

            return Task.FromResult(deviceEndpoint);  
        }

        internal override string TenantId => null;
    }
}
