// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Threading.Tasks;
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
