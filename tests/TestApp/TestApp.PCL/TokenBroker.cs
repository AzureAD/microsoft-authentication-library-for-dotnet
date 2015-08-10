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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Test.ADAL.Common;

namespace TestApp.PCL
{
    class MobileAppSts : Sts
    {
        public MobileAppSts()
        {
            this.InvalidAuthority = "https://invalid_address.com/path";
            this.InvalidClientId = "87002806-c87a-41cd-896b-84ca5690d29e";
            this.InvalidResource = "00000003-0000-0ff1-ce00-000000000001";
            this.ValidateAuthority = true;
            this.ValidExistingRedirectUri = new Uri("https://login.live.com/");
            this.ValidExpiresIn = 28800;
            this.ValidNonExistingRedirectUri = new Uri("adaliosapp://com.your-company.adaliostestapp");
            this.ValidLoggedInFederatedUserName = "dummy\\dummy";
            string[] segments = this.ValidLoggedInFederatedUserName.Split(new[] { '\\' });
            this.ValidLoggedInFederatedUserId = string.Format("{0}@microsoft.com", (segments.Length == 2) ? segments[1] : segments[0]);

            this.TenantName = "adalobjc.onmicrosoft.com";
            this.Authority = string.Format("https://login.windows.net/{0}", this.TenantName);
            this.TenantlessAuthority = "https://login.windows.net/Common";
            this.Type = StsType.AAD;
            this.ValidClientId = "dd9caee2-38bd-484e-998c-7529bdef220f";
            this.ValidNonExistentRedirectUriClientId = this.ValidClientId;
            this.ValidClientIdWithExistingRedirectUri = this.ValidClientId;
            this.ValidUserName = @"e2e@adalobjc.onmicrosoft.com";
            this.ValidDefaultRedirectUri = new Uri("https://login.live.com/");
            this.ValidExistingRedirectUri = new Uri("https://login.live.com/");
            this.ValidRedirectUriForConfidentialClient = new Uri("https://confidential.foobar.com");
            this.ValidPassword = "<REPLACE>";
            this.ValidResource = "https://graph.windows.net";

            this.MsaUserName = "adaltest@outlook.com";
            this.MsaPassword = "<REPLACE>";
        }

        public string TenantName { get; protected set; }
    }
    public class TokenBroker
    {
        private AuthenticationContext context;

        private Sts sts = new MobileAppSts();

        public TokenBroker()
        {
            context = new AuthenticationContext(sts.Authority, true);
        }

        public async Task<string> GetTokenInteractiveAsync(IPlatformParameters parameters)
        {
            try
            {
                var result = await context.AcquireTokenAsync(sts.ValidResource, sts.ValidClientId, sts.ValidNonExistingRedirectUri, parameters, new UserIdentifier(sts.ValidUserName, UserIdentifierType.OptionalDisplayableId));

                return result.AccessToken;
            }
            catch (Exception ex)
            {
                string msg = ex.Message +"\n"+ex.StackTrace;
                
                return msg;
            }
        }

        public async Task<string> GetTokenInteractiveWithMsAppAsync(IPlatformParameters parameters)
        {
            try
            {
                var result = await context.AcquireTokenAsync(sts.ValidResource, sts.ValidClientId, null, parameters, new UserIdentifier(sts.ValidUserName, UserIdentifierType.OptionalDisplayableId));

                return result.AccessToken;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> GetTokenWithUsernamePasswordAsync()
        {
            try
            {
                var result = await context.AcquireTokenAsync(sts.ValidResource, sts.ValidClientId, new UserCredential(sts.ValidUserName, sts.ValidPassword));

                return result.AccessToken;
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
                var result = await context.AcquireTokenAsync(sts.ValidResource, new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret));

                return result.AccessToken;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public void ClearTokenCache()
        {
            this.context.TokenCache.Clear();
        }
    }
}
