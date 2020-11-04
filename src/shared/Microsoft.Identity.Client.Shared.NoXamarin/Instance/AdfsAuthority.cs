// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Globalization;

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

        internal override string GetTenantedAuthority(string tenantId)
        {
            return AuthorityInfo.CanonicalAuthority;
        }

        internal override AuthorityEndpoints GetHardcodedEndpoints()
        {
            string tokenEndpoint = string.Format(
                   CultureInfo.InvariantCulture,
                   TokenEndpointTemplate,
                   AuthorityInfo.CanonicalAuthority);

            string deviceEndpoint = string.Format(
                  CultureInfo.InvariantCulture,
                  DeviceCodeEndpointTemplate,
                  AuthorityInfo.CanonicalAuthority);

            string authEndpoint = string.Format(CultureInfo.InvariantCulture,
                    AuthorizationEndpointTemplate,
                    AuthorityInfo.CanonicalAuthority);

            return new AuthorityEndpoints(authEndpoint, tokenEndpoint, deviceEndpoint);
        }

        internal override string TenantId => null;
    }
}
