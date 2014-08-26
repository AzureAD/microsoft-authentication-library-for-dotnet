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
using System.Text;
using System.Threading.Tasks;

using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Security.Authentication.Web;
using Windows.System.UserProfile;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal partial class AcquireTokenInteractiveHandler
    {
        protected override async Task PreTokenRequest()
        {
            await base.PreTokenRequest();
            await this.AcquireAuthorizationAsync();
            this.VerifyAuthorizationResult();
        }

        private async Task AcquireAuthorizationAsync()
        {
            Uri authorizationUri = this.CreateAuthorizationUri(await IncludeFormsAuthParamsAsync(this.CallState));
            this.authorizationResult = await webUi.AuthenticateAsync(authorizationUri, this.redirectUri, this.CallState);
        }

        internal static async Task<bool> IncludeFormsAuthParamsAsync(CallState callState)
        {
            return IsDomainJoined() && await IsUserLocalAsync(callState);
        }

        private static bool IsDomainJoined()
        {
            return NetworkInformation.GetHostNames().Any(entry => entry.Type == HostNameType.DomainName);
        }

        private async static Task<bool> IsUserLocalAsync(CallState callState)
        {
            if (!UserInformation.NameAccessAllowed)
            {
                // The access is not allowed and we cannot determine whether this is a local user or not. So, we do NOT add form auth parameter.
                // This is the case where we can advise customers to add extra query parameter if they want.

                Logger.Information(callState, "Cannot access user information to determine whether it is a local user or not due to machine's privacy setting.");
                return false;   
            }

            try
            {
                return string.IsNullOrEmpty(await UserInformation.GetDomainNameAsync());
            }
            catch (UnauthorizedAccessException)
            {
                Logger.Information(callState, "Cannot try Windows Integrated Auth due to lack of Enterprise capability.");
                // This mostly means Enterprise capability is missing, so WIA cannot be used and
                // we return true to add form auth parameter in the caller.
                return true;
            }
        }

        private void SetRedirectUriRequestParameter()
        {
            this.redirectUriRequestParameter = ReferenceEquals(this.redirectUri, Constant.SsoPlaceHolderUri) ? 
                WebAuthenticationBroker.GetCurrentApplicationCallbackUri().AbsoluteUri : 
                this.redirectUri.AbsoluteUri;
        }
    }
}
