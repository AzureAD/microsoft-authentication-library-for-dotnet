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

        internal AadAuthority(AuthorityInfo authorityInfo) : base(authorityInfo)
        {
        }

        internal override string GetTenantId()
        {
            return GetFirstPathSegment(AuthorityInfo.CanonicalAuthority);
        }

        internal override string GetTenantedAuthority(string tenantId)
        {
            string currentTenantId = this.GetTenantId();

            if (!string.IsNullOrEmpty(tenantId) &&
                !string.IsNullOrEmpty(currentTenantId) &&
                TenantlessTenantNames.Contains(currentTenantId))
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
