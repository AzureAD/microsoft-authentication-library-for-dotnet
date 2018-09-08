//------------------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Core.OAuth2;

namespace Microsoft.Identity.Core.Instance
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

        public const string DefaultTrustedHost = "login.microsoftonline.com";

        private const string AadInstanceDiscoveryEndpoint = "https://login.microsoftonline.com/common/discovery/instance";

        public const string AADCanonicalAuthorityTemplate = "https://{0}/{1}/";

        internal AadAuthority(string authority, bool validateAuthority) : base(authority, validateAuthority)
        {
            AuthorityType = AuthorityType.Aad;
        }

        internal override async Task UpdateCanonicalAuthorityAsync(RequestContext requestContext)
        {
            var metadata = await AadInstanceDiscovery.Instance.
                GetMetadataEntryAsync(new Uri(CanonicalAuthority), this.ValidateAuthority, requestContext).ConfigureAwait(false);

            CanonicalAuthority = UpdateHost(CanonicalAuthority, metadata.PreferredNetwork);
        }

        protected override async Task<string> GetOpenIdConfigurationEndpointAsync(string userPrincipalName,
            RequestContext requestContext)
        {
            var authorityUri = new Uri(CanonicalAuthority);

            if (ValidateAuthority && !IsInTrustedHostList(authorityUri.Host))
            {
                InstanceDiscoveryResponse discoveryResponse =
                    await AadInstanceDiscovery.Instance.
                    DoInstanceDiscoveryAndCacheAsync(authorityUri, true, requestContext).ConfigureAwait(false);

                return discoveryResponse.TenantDiscoveryEndpoint;
            }
            return GetDefaultOpenIdConfigurationEndpoint();
        }


        protected override bool ExistsInValidatedAuthorityCache(string userPrincipalName)
        {
            return ValidatedAuthorities.ContainsKey(CanonicalAuthority);
        }

        protected override void AddToValidatedAuthorities(string userPrincipalName)
        {
            // add to the list of validated authorities so that we don't do openid configuration call
            ValidatedAuthorities[CanonicalAuthority] = this;
        }

        protected override string GetDefaultOpenIdConfigurationEndpoint()
        {
            return CanonicalAuthority + "v2.0/.well-known/openid-configuration";
        }

        internal static bool IsInTrustedHostList(string host)
        {
            return
                !string.IsNullOrEmpty(
                    TrustedHostList.FirstOrDefault(a => string.Compare(host, a, StringComparison.OrdinalIgnoreCase) == 0));
        }
        
        internal override string GetTenantId()
        {
            return GetFirstPathSegment(CanonicalAuthority);
        }

        internal override void UpdateTenantId(string tenantId)
        {
            Uri authorityUri = new Uri(CanonicalAuthority);

            CanonicalAuthority = 
                string.Format(CultureInfo.InvariantCulture, AADCanonicalAuthorityTemplate, authorityUri.Authority, tenantId);
        }
    }
}