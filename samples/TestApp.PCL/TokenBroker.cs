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

using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Test.MSAL.Common;

namespace TestApp.PCL
{
    public class MobileAppSts : Sts
    {
        public MobileAppSts()
        {
            this.InvalidAuthority = "https://invalid_address.com/path";
            this.ValidateAuthority = true;
            this.ValidExistingRedirectUri = new Uri("https://login.live.com/");
            this.ValidExpiresIn = 28800;
            this.ValidNonExistingRedirectUri = new Uri("urn:ietf:wg:oauth:2.0:oob");
            this.ValidLoggedInFederatedUserName = "dummy\\dummy";
            string[] segments = this.ValidLoggedInFederatedUserName.Split(new[] { '\\' });
            this.ValidLoggedInFederatedUserId = string.Format(CultureInfo.InvariantCulture,"{0}@microsoft.com", (segments.Length == 2) ? segments[1] : segments[0]);
            this.TenantName = "Common";
            this.Authority = string.Format(CultureInfo.InvariantCulture,"https://login.windows.net/{0}", this.TenantName);
            this.TenantlessAuthority = "https://login.windows.net/Common";
            this.ValidClientId = "dd9caee2-38bd-484e-998c-7529bdef220f";
            this.ValidNonExistentRedirectUriClientId = this.ValidClientId;
            this.ValidClientIdWithExistingRedirectUri = this.ValidClientId;
            this.ValidUserName = @"<REPLACE>";
            this.ValidDefaultRedirectUri = new Uri("https://login.live.com/");
            this.ValidExistingRedirectUri = new Uri("https://login.live.com/");
            this.ValidRedirectUriForConfidentialClient = new Uri("https://confidential.clientredirecturi.com");
            this.ValidPassword = "<REPLACE>";
            this.ValidScope = new[] {"https://graph.microsoft.com/user.read"};

        }

        public string TenantName { get; protected set; }
    }

    public class TokenBroker
    {
        private PublicClientApplication app;
        public Sts Sts = new MobileAppSts();

        public async Task<string> GetTokenSilentAsync(IPlatformParameters parameters)
        {
            try
            {
                app = new PublicClientApplication("CLIENT_ID");
                var result = await app.AcquireTokenSilentAsync(Sts.ValidScope, null);

                return result.Token;
            }
            catch (Exception ex)
            {
                string msg = ex.Message + "\n" + ex.StackTrace;

                return msg;
            }
        }


        public string GetTokenInteractiveWithMsAppAsync(IPlatformParameters parameters)
        {
            try
            {
/* app = new AuthenticationContext(Sts.Authority, true);
                                var result = await app.AcquireTokenAsync(Sts.ValidScope, Sts.ValidClientId, null, parameters, new UserIdentifier(Sts.ValidUserName, UserIdentifierType.OptionalDisplayableId));

                                return result.Token;*/
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        

        public string GetTokenWithClientCredentialAsync()
        {
            try
            {
/* app = new AuthenticationContext(Sts.Authority, true);
                                var result = await app.AcquireTokenAsync(Sts.ValidScope, new ClientCredential(Sts.ValidConfidentialClientId, Sts.ValidConfidentialClientSecret));

                                return result.Token;*/
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public void ClearTokenCache()
        {
        }
    }
}
