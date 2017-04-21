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

using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace DesktopTestApp
{
    class ConfidentialClientHandler
    {
        public ConfidentialClientHandler(string clientId)
        {
            ApplicationId = clientId;
        }

        #region Properties
        public string ApplicationId { get; set; }

        public ClientCredential ClientCredential { get; set; }

        public TokenCache UserTokenCache { get; set; }

        public TokenCache AppTokenCache { get; set; }

        public string[] ConfClientScopes { get; set; }

        public bool ForceRefresh { get; set; }

        public string ConfClientOverriddenAuthority { get; set; }

        public IUser CurrentUser { get; set; }

        public ConfidentialClientApplication ConfidentialClientApplication { get; set; }

        #endregion

        public async Task<AuthenticationResult> AcquireTokenForClientAsync(string[] scopes, bool forceRefresh, string overriddenAuthority, string applicationId)
        {
            ConfidentialClientApplication = CreateConfidentialClientApplication(
                overriddenAuthority, applicationId, ClientCredential, UserTokenCache, AppTokenCache);

            AuthenticationResult result;
            if (forceRefresh)
            {
                result = await ConfidentialClientApplication.AcquireTokenForClientAsync(scopes);
            }
            else
            {
                result = await ConfidentialClientApplication.AcquireTokenForClientAsync(scopes, false);
            }
            return (result);
        }

        /*public async Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(string[] scopes, UserAssertion userAssertion, string authority)
        {
            
        }*/

        private ConfidentialClientApplication CreateConfidentialClientApplication(string overriddenAuthority, string applicationId, ClientCredential clientCredential,
            TokenCache userTokenCache, TokenCache appTokenCache)
        {
            string redirectUri = "urn:ietf:wg:oauth:2.0:oob";

            if (string.IsNullOrEmpty(overriddenAuthority))
            {
                // Use default authority
                ConfidentialClientApplication = new ConfidentialClientApplication(applicationId, redirectUri, clientCredential,
                    userTokenCache, appTokenCache);
            }
            else
            {
                // Use the override authority provided
                ConfidentialClientApplication = new ConfidentialClientApplication(applicationId, overriddenAuthority, redirectUri,
                    clientCredential, userTokenCache, appTokenCache);
            }
            return ConfidentialClientApplication;
        }
    }
}
