// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Instance
{
    internal class AadAuthority : Authority
    {
        public const string DefaultTrustedHost = "login.microsoftonline.com";
        public const string AADCanonicalAuthorityTemplate = "https://{0}/{1}/";

        private static readonly HashSet<string> s_trustedHostList = new HashSet<string>()
        {
            "login.partner.microsoftonline.cn", // Microsoft Azure China
            "login.chinacloudapi.cn",
            "login.microsoftonline.de", // Microsoft Azure Blackforest
            "login-us.microsoftonline.com", // Microsoft Azure US Government - Legacy
            "login.microsoftonline.us", // Microsoft Azure US Government
             DefaultTrustedHost, // Microsoft Azure Worldwide
            "login.windows.net"
        };

        internal static bool IsInTrustedHostList(string host)
        {
            return s_trustedHostList.ContainsOrdinalIgnoreCase(host);
        }

        internal AadAuthority(
            IServiceBundle serviceBundle,
            AuthorityInfo authorityInfo) : base(serviceBundle, authorityInfo)
        {
        }

        internal override async Task UpdateCanonicalAuthorityAsync(
            RequestContext requestContext)
        {
            var metadata = await ServiceBundle.AadInstanceDiscovery
                                 .GetMetadataEntryAsync(
                                     new Uri(AuthorityInfo.CanonicalAuthority),
                                     requestContext)
                                 .ConfigureAwait(false);

            AuthorityInfo.CanonicalAuthority =
                CreateAuthorityUriWithHost(AuthorityInfo.CanonicalAuthority, metadata.PreferredNetwork);
        }

        internal override string GetTenantId()
        {
            return GetFirstPathSegment(AuthorityInfo.CanonicalAuthority);
        }

        internal override void UpdateTenantId(string tenantId)
        {
            var authorityUri = new Uri(AuthorityInfo.CanonicalAuthority);

            AuthorityInfo.CanonicalAuthority = string.Format(
                CultureInfo.InvariantCulture,
                AADCanonicalAuthorityTemplate,
                authorityUri.Authority,
                tenantId);
        }
    }
}
