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
using Microsoft.Identity.Client.Internal.OAuth2;

namespace Microsoft.Identity.Client.Internal.Instance
{
    internal class AadAuthority : Authority
    {
        private const string AadInstanceDiscoveryEndpoint = "https://login.microsoftonline.com/common/discovery/instance";
        private static readonly HashSet<string> TrustedHostList = new HashSet<string>()
        {
            "login.windows.net",
            "login.chinacloudapi.cn",
            "login.cloudgovapi.us",
            "login.microsoftonline.com",
            "login.microsoftonline.de"
        };

        public AadAuthority(string authority, bool validateAuthority) : base(authority, validateAuthority)
        {
            AuthorityType = AuthorityType.Aad;
            UpdateCanonicalAuthority();
        }

        protected void UpdateCanonicalAuthority()
        {
            UriBuilder uriBuilder = new UriBuilder(CanonicalAuthority);
            if (uriBuilder.Host.Equals("login.windows.net", StringComparison.OrdinalIgnoreCase))
            {
                uriBuilder.Host = "login.microsoftonline.com";
            }

            CanonicalAuthority = uriBuilder.Uri.AbsoluteUri;
        }

        protected override async Task<string> GetOpenIdConfigurationEndpoint(string userPrincipalName,
            RequestContext requestContext)
        {

            if (ValidateAuthority && !IsInTrustedHostList(new Uri(CanonicalAuthority).Host))
            {
                OAuth2Client client = new OAuth2Client();
                client.AddQueryParameter("api-version", "1.0");
                client.AddQueryParameter("authorization_endpoint", CanonicalAuthority + "oauth2/v2.0/authorize");

                InstanceDiscoveryResponse discoveryResponse =
                    await
                        client.DiscoverAadInstance(new Uri(AadInstanceDiscoveryEndpoint), requestContext)
                            .ConfigureAwait(false);
                if (discoveryResponse.TenantDiscoveryEndpoint == null)
                {
                    throw new MsalServiceException(discoveryResponse.Error, discoveryResponse.ErrorDescription);
                }

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
    }
}