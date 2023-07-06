// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Instance
{
    internal class B2CAuthority : AadAuthority
    {
        public const string Prefix = "tfp"; // The HTTP path of B2C authority looks like "/tfp/<your_tenant_name>/..."
        public const string B2CCanonicalAuthorityTemplate = "https://{0}/{1}/{2}/{3}/";

        internal B2CAuthority(AuthorityInfo authorityInfo)
            : base(authorityInfo)
        {
            TenantId = AuthorityInfo.CanonicalAuthority.Segments[2].TrimEnd('/');
        }

        internal override string TenantId { get; }

        // B2C doesn't allow tenant update, but ignores it
        internal override string GetTenantedAuthority(string tenantId, bool forceSpecifiedTenant = false)
        {
            return AuthorityInfo.CanonicalAuthority.AbsoluteUri;
        }

    }
}
