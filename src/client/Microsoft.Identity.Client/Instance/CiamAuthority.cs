// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    internal class CiamAuthority : AadAuthority
    {
        internal CiamAuthority(AuthorityInfo authorityInfo) : 
            base(authorityInfo)
        { }

        // CIAM authorities are always fully-qualified (e.g. https://contoso.ciamlogin.com/contoso.onmicrosoft.com).
        // Tenant overrides are not applicable; return the canonical authority unchanged, matching B2C's behavior.
        internal override string GetTenantedAuthority(string tenantId, bool forceSpecifiedTenant = false)
        {
            return AuthorityInfo.CanonicalAuthority.AbsoluteUri;
        }

        /// <summary>
        /// Translates CIAM authorities into a usable form. This is needed only until ESTS is updated to support the north star format
        /// North star format: https://idgciamdemo.ciamlogin.com
        /// Transformed format: https://idgciamdemo.ciamlogin.com/idgciamdemo.onmicrosoft.com
        /// </summary>
        internal static Uri TransformAuthority(Uri ciamAuthority)
        {
            string transformedInstance;
            string transformedTenant;

            string host = ciamAuthority.Host + ciamAuthority.AbsolutePath;
            if (string.Equals(ciamAuthority.AbsolutePath, "/"))
            {
                string ciamTenant = host.Substring(0, host.IndexOf(Constants.CiamAuthorityHostSuffix, StringComparison.OrdinalIgnoreCase));
                transformedInstance = $"https://{ciamTenant}{Constants.CiamAuthorityHostSuffix}/";
                transformedTenant = ciamTenant + ".onmicrosoft.com";
                return new Uri(transformedInstance + transformedTenant);
            }
            else
            {
                return ciamAuthority;
            }
        }
    }

}
