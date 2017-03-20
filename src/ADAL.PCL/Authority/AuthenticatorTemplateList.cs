//----------------------------------------------------------------------
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
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class AuthenticatorTemplateList : List<AuthenticatorTemplate>
    {
        public AuthenticatorTemplateList()
        {
            string[] trustedHostList =
                {
                    "login.windows.net",            // Microsoft Azure Worldwide - Used in validation scenarios where host is not this list 
                    "login.chinacloudapi.cn",       // Microsoft Azure China
                    "login.microsoftonline.de",     // Microsoft Azure Blackforest
                    "login-us.microsoftonline.com", // Microsoft Azure US Government - Legacy
                    "login.microsoftonline.us",     // Microsoft Azure US Government
                    "login.microsoftonline.com"     // Microsoft Azure Worldwide
                };

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
                    await this.First().VerifyAnotherHostByInstanceDiscoveryAsync(host, tenant, callState).ConfigureAwait(false);
                }
            }

            return matchingAuthenticatorTemplate ?? AuthenticatorTemplate.CreateFromHost(host);
        }
    }
}
