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

        // TODO: bogavril consolidate well known / trusted hosts
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
