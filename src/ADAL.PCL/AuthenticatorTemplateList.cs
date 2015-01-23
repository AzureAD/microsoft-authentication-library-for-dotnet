//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class AuthenticatorTemplateList : List<AuthenticatorTemplate>
    {
        public AuthenticatorTemplateList()
        {
            string[] trustedHostList = { "login.windows.net", "login.chinacloudapi.cn", "login.cloudgovapi.us", "login.microsoftonline.com" };

            string customAuthorityHost = PlatformPlugin.PlatformInformation.GetEnvironmentVariable("customTrustedHost");
            if (string.IsNullOrWhiteSpace(customAuthorityHost))
            {
                foreach (string host in trustedHostList)
                {
                    this.Add(AuthenticatorTemplate.CreateFromHost(host));
                }
            }
            else
            {
                this.Add(AuthenticatorTemplate.CreateFromHost(customAuthorityHost));
            }
        }

        public async Task<AuthenticatorTemplate> FindMatchingItemAsync(bool validateAuthority, string host, string tenant, CallState callState)
        {
            AuthenticatorTemplate matchingAuthenticatorTemplate = null;
            if (validateAuthority)
            {
                matchingAuthenticatorTemplate = this.FirstOrDefault(a => string.Compare(host, a.Host, StringComparison.OrdinalIgnoreCase) == 0);
                if (matchingAuthenticatorTemplate == null)
                {
                    // We only check with the first trusted authority (login.windows.net) for instance discovery
                    await this.First().VerifyAnotherHostByInstanceDiscoveryAsync(host, tenant, callState);
                }
            }

            return matchingAuthenticatorTemplate ?? AuthenticatorTemplate.CreateFromHost(host);
        }
    }
}
