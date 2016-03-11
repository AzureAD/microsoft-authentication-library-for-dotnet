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

using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
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
            this.ValidLoggedInFederatedUserId = string.Format("{0}@microsoft.com", (segments.Length == 2) ? segments[1] : segments[0]);

            this.TenantName = "Common";
            this.Authority = string.Format("https://login.windows.net/{0}", this.TenantName);
            this.TenantlessAuthority = "https://login.windows.net/Common";
            this.ValidClientId = "dd9caee2-38bd-484e-998c-7529bdef220f";
            this.ValidNonExistentRedirectUriClientId = this.ValidClientId;
            this.ValidClientIdWithExistingRedirectUri = this.ValidClientId;
            this.ValidUserName = @"<REPLACE>";
            this.ValidDefaultRedirectUri = new Uri("https://login.live.com/");
            this.ValidExistingRedirectUri = new Uri("https://login.live.com/");
            this.ValidRedirectUriForConfidentialClient = new Uri("https://confidential.foo-bar.com");
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
                app = new PublicClientApplication("https://login.windows.net/common", "CLIENT_ID");
                var result = await app.AcquireTokenSilentAsync(Sts.ValidScope, Sts.ValidUserName);

                return result.Token;
            }
            catch (Exception ex)
            {
                string msg = ex.Message + "\n" + ex.StackTrace;

                return msg;
            }
        }


        public async Task<string> GetTokenInteractiveWithMsAppAsync(IPlatformParameters parameters)
        {
            try
            {
/*                app = new AuthenticationContext(Sts.Authority, true);
                var result = await app.AcquireTokenAsync(Sts.ValidScope, Sts.ValidClientId, null, parameters, new UserIdentifier(Sts.ValidUserName, UserIdentifierType.OptionalDisplayableId));

                return result.Token;*/
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        

        public async Task<string> GetTokenWithClientCredentialAsync()
        {
            try
            {
/*                app = new AuthenticationContext(Sts.Authority, true);
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
            TokenCache.DefaultSharedUserTokenCache.Clear("CLIENT_ID");
        }
    }
}
