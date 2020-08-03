// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Identity.Client.Instance
{
    internal class AadAuthority : Authority
    {
        public const string DefaultTrustedHost = "login.microsoftonline.com";
        public const string AADCanonicalAuthorityTemplate = "https://{0}/{1}/";

        private const string TokenEndpointTemplate = "{0}oauth2/v2.0/token";
        private const string DeviceCodeEndpointTemplate = "{0}oauth2/v2.0/devicecode";
        private const string AuthorizationEndpointTemplate = "{0}oauth2/v2.0/authorize";

        private static readonly ISet<string> s_tenantlessTenantNames = new HashSet<string>(
          new[]
          {
                "common",
                "organizations",
                "consumers"
          },
          StringComparer.OrdinalIgnoreCase);

        internal AadAuthority(AuthorityInfo authorityInfo) : base(authorityInfo)
        {
            TenantId = GetFirstPathSegment(AuthorityInfo.CanonicalAuthority);
        }

        internal override string TenantId { get; }

        internal bool IsCommonOrganizationsOrConsumersTenant()
        {
            return !string.IsNullOrEmpty(TenantId) &&
                s_tenantlessTenantNames.Contains(TenantId);
        }

        internal override string GetTenantedAuthority(string tenantId)
        {
            if (!string.IsNullOrEmpty(tenantId) &&
                IsCommonOrganizationsOrConsumersTenant())
            {
                var authorityUri = new Uri(AuthorityInfo.CanonicalAuthority);

                return string.Format(
                    CultureInfo.InvariantCulture,
                    AADCanonicalAuthorityTemplate,
                    authorityUri.Authority,
                    tenantId);
            }

            return AuthorityInfo.CanonicalAuthority;
        }

        internal override AuthorityEndpoints GetHardcodedEndpoints()
        {
            string tokenEndpoint = string.Format(
                    CultureInfo.InvariantCulture,
                    TokenEndpointTemplate,
                    AuthorityInfo.CanonicalAuthority);

            string authorizationEndpoint = string.Format(CultureInfo.InvariantCulture,
                    AuthorizationEndpointTemplate,
                    AuthorityInfo.CanonicalAuthority);

            string deviceEndpoint = string.Format(
                CultureInfo.InvariantCulture,
                DeviceCodeEndpointTemplate,
                AuthorityInfo.CanonicalAuthority);

            return new AuthorityEndpoints(authorizationEndpoint, tokenEndpoint, deviceEndpoint);
        }
    }
}
