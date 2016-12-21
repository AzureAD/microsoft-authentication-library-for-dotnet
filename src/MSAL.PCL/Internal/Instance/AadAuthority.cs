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
        private const string AadInstanceDiscoveryEndpoint = "https://login.windows.net/common/discovery/instance";

        private static readonly HashSet<string> TrustedHostList = new HashSet<string>()
        {
            "login.windows.net",
            "login.chinacloudapi.cn",
            "login.cloudgovapi.us",
            "login.microsoftonline.com",
            "login.microsoftonline.de"
        };

        public AadAuthority(string authority) : base(authority)
        {
            this.AuthorityType = AuthorityType.Aad;
        }

        protected override async Task<string> Validate(string host, string tenant, CallState callState)
        {
            if (ValidateAuthority && !IsInTrustedHostList(host))
            {
                OAuth2Client client = new OAuth2Client();
                client.AddQueryParameter("api-version", "1.0");
                client.AddQueryParameter("authorization_endpoint",
                    string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/oauth2/v2.0/authorize", host, tenant));

                try
                {
                    InstanceDiscoveryResponse discoveryResponse =
                        await
                            client.DiscoverAadInstance(new Uri(AadInstanceDiscoveryEndpoint), callState)
                                .ConfigureAwait(false);
                    if (discoveryResponse.TenantDiscoveryEndpoint == null)
                    {
                        throw new MsalServiceException(discoveryResponse.Error, discoveryResponse.ErrorDescription);
                    }

                    return discoveryResponse.TenantDiscoveryEndpoint;
                }
                catch (RetryableRequestException exc)
                {
                    throw exc.InnerException;
                }
            }

            return GetDefaultOpenIdConfigurationEndpoint();
        }

        internal bool IsInTrustedHostList(string host)
        {
            return
                !string.IsNullOrEmpty(
                    TrustedHostList.FirstOrDefault(a => string.Compare(host, a, StringComparison.OrdinalIgnoreCase) == 0));
        }
    }
}