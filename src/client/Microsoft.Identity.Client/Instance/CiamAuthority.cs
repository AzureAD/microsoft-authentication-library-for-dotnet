// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    internal class CiamAuthority : AadAuthority
    {
        internal CiamAuthority(AuthorityInfo authorityInfo) : 
            base(authorityInfo)
        { }

        internal override string GetTenantedAuthority(string tenantId, bool forceSpecifiedTenant = false)
        {
            if (!string.IsNullOrEmpty(tenantId))
            {
                var authorityUri = AuthorityInfo.CanonicalAuthority;

                return string.Format(
                    CultureInfo.InvariantCulture,
                    AADCanonicalAuthorityTemplate,
                    authorityUri.Authority,
                    tenantId);
            }

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
