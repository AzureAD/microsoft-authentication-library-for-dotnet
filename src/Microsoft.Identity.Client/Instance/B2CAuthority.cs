// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance
{
    internal class B2CAuthority : AadAuthority
    {
        public const string Prefix = "tfp"; // The http path of B2C authority looks like "/tfp/<your_tenant_name>/..."
        public const string B2CCanonicalAuthorityTemplate = "https://{0}/{1}/{2}/{3}/";

        internal B2CAuthority(IServiceBundle serviceBundle, AuthorityInfo authorityInfo)
            : base(serviceBundle, authorityInfo)
        {
        }

        internal override string GetTenantId()
        {
            return new Uri(AuthorityInfo.CanonicalAuthority).Segments[2].TrimEnd('/');
        }

        internal override string GetTenantedAuthority(string tenantId)
        {
            // For B2C, tenant is not changeble
            return AuthorityInfo.CanonicalAuthority;
        }
    }
}
