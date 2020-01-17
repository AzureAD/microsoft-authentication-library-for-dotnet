// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Instance
{
    internal class AadAuthority : Authority
    {
        public const string DefaultTrustedHost = "login.microsoftonline.com";
        public const string AADCanonicalAuthorityTemplate = "https://{0}/{1}/";

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
        }

        internal override string GetTenantId()
        {
            return GetFirstPathSegment(AuthorityInfo.CanonicalAuthority);
        }

        internal bool IsCommonOrganizationsOrConsumersTenant()
        {
            string tenantId = this.GetTenantId();

            return !string.IsNullOrEmpty(tenantId) &&
                s_tenantlessTenantNames.Contains(tenantId);
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
    }
}
