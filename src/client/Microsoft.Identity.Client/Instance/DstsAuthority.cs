// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Instance
{
    internal class DstsAuthority : Authority
    {
        // updating token endpoints to include v2.0 so DSTS can troubleshoot the scopes issue
        private const string TokenEndpointTemplate = "{0}dstsv2/token";
        private const string AuthorizationEndpointTemplate = "{0}dstsv2/authorize";
        private const string DeviceCodeEndpointTemplate = "{0}dstsv2/devicecode";

        public DstsAuthority(AuthorityInfo authorityInfo)
            : base(authorityInfo)
        {
            TenantId = AuthorityInfo.GetSecondPathSegment(AuthorityInfo.CanonicalAuthority);
        }

        internal override string GetTenantedAuthority(string tenantId, bool forceTenantless = false)
        {
            return AuthorityInfo.CanonicalAuthority;
        }

        internal override string GetTokenEndpoint()
        {
            string tokenEndpoint = string.Format(
                              CultureInfo.InvariantCulture,
                              TokenEndpointTemplate,
                             AuthorityInfo.CanonicalAuthority);
            return tokenEndpoint;
        }

        internal override string GetAuthorizationEndpoint()
        {
            string authEndpoint = string.Format(CultureInfo.InvariantCulture,
                    AuthorizationEndpointTemplate,
                    AuthorityInfo.CanonicalAuthority);

            return authEndpoint;

        }

        internal override string GetDeviceCodeEndpoint()
        {
            string deviceEndpoint = string.Format(
                  CultureInfo.InvariantCulture,
                  DeviceCodeEndpointTemplate,
                  AuthorityInfo.CanonicalAuthority);

            return deviceEndpoint;
        }

        internal override string TenantId { get; }
    }
}
