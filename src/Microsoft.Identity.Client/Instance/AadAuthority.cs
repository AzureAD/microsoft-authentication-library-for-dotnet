// ------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance
{
    internal class AadAuthority : Authority
    {
        internal static readonly HashSet<string> TrustedHostList = new HashSet<string>()
        {
            "login.windows.net", // Microsoft Azure Worldwide - Used in validation scenarios where host is not this list
            "login.chinacloudapi.cn", // Microsoft Azure China
            "login.microsoftonline.de", // Microsoft Azure Blackforest
            "login-us.microsoftonline.com", // Microsoft Azure US Government - Legacy
            "login.microsoftonline.us", // Microsoft Azure US Government
            "login.microsoftonline.com", // Microsoft Azure Worldwide
            "login.cloudgovapi.us" // Microsoft Azure US Government
        };

        internal static bool IsInTrustedHostList(string host)
        {
            return !string.IsNullOrEmpty(
                TrustedHostList.FirstOrDefault(a => string.Compare(host, a, StringComparison.OrdinalIgnoreCase) == 0));
        }

        public const string DefaultTrustedHost = "login.microsoftonline.com";
        public const string AADCanonicalAuthorityTemplate = "https://{0}/{1}/";

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
                                     //AuthorityInfo.ValidateAuthority,
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