// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.Instance
{
    internal class AdfsAuthority : Authority
    {
        public AdfsAuthority(AuthorityInfo authorityInfo)
            : base(authorityInfo)
        {
        }

        //ADFS does not have a concept of a tenant ID. This prevents ADFS from supporting multiple tenants

        internal override string GetTenantedAuthority(string tenantId)
        {
            return AuthorityInfo.CanonicalAuthority;
        }

        internal override string GetTenantId()
        {
            return null;
        }
    }
}
