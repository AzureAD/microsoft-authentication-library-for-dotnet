// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    internal class DstsAuthority : Authority
    {
        public const string DstsCanonicalAuthorityTemplate = "https://{0}/dstsv2/{1}/";

        // updating token endpoints to include v2.0 so DSTS can troubleshoot the scopes issue
        private const string TokenEndpointTemplate = "{0}oauth2/v2.0/token";
        private const string AuthorizationEndpointTemplate = "{0}oauth2/v2.0/authorize";
        private const string DeviceCodeEndpointTemplate = "{0}oauth2/v2.0/devicecode";

        public DstsAuthority(AuthorityInfo authorityInfo)
            : base(authorityInfo)
        {
            TenantId = AuthorityInfo.GetSecondPathSegment(AuthorityInfo.CanonicalAuthority);
        }

        // DSTS authorities use their own URL template (dstsv2/{tenantId}/), not the AAD template.
        // Only honor tenant overrides when forceSpecifiedTenant=true (i.e., .WithTenantId() at request level).
        // The old non-forced path (IsCommonOrOrganizationsTenant) was dead because DSTS authorities
        // always carry a real tenant, never "common" or "organizations".
        internal override string GetTenantedAuthority(string tenantId, bool forceSpecifiedTenant = false)
        {
            if (!string.IsNullOrEmpty(tenantId) && forceSpecifiedTenant)
            {
                var authorityUri = AuthorityInfo.CanonicalAuthority;

                return string.Format(
                    CultureInfo.InvariantCulture,
                    DstsCanonicalAuthorityTemplate,
                    authorityUri.Authority,
                    tenantId);
            }

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
            string authEndpoint = string.Format(
                    CultureInfo.InvariantCulture,
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

        internal override string TenantId { get; }
    }
}
