// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Instance
{
    internal class AdfsAuthority : Authority
    {
        private readonly HashSet<string> _validForDomainsList = new HashSet<string>();

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
