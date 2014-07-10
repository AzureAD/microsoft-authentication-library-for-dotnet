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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    class AcquireTokenInteractiveHandler : AcquireTokenHandlerBase
    {

        private readonly Uri redirectUri;

        private readonly PromptBehavior promptBehavior;

        private readonly string extraQueryParameters;

        private readonly IWebUI webUI;

        private readonly UserIdentifier userId;

        public AcquireTokenInteractiveHandler(Authenticator authenticator, TokenCache tokenCache, string resource, string clientId, Uri redirectUri, PromptBehavior promptBehavior, UserIdentifier userId, string extraQueryParameters, IWebUI webUI, bool callSync)
            : base(authenticator, tokenCache, resource, new ClientKey(clientId), TokenSubjectType.User, callSync)
        {
            if (redirectUri == null)
            {
                throw new ArgumentNullException("redirectUri");
            }

            this.redirectUri = redirectUri;

            if (userId == null)
            {
                throw new ArgumentNullException("userId", AdalErrorMessage.SpecifyAnyUser);
            }

            this.promptBehavior = promptBehavior;

            this.extraQueryParameters = extraQueryParameters;

            this.webUI = webUI;

            this.userId = userId;

            this.UniqueId = userId.UniqueId;
            this.DisplayableId = userId.DisplayableId;
            this.UserIdentifierType = userId.Type;

            this.LoadFromCache = (promptBehavior != PromptBehavior.Always && promptBehavior != PromptBehavior.RefreshSession);

            this.SupportADFS = true;
        }

        protected override async Task<AuthenticationResult> SendTokenRequestAsync()
        {
            AuthenticationResult result;
#if ADAL_WINRT
            AuthorizationResult authorizationResult = await this.AcquireAuthorizationAsync();
#else
            // We do not have async interactive API in .NET, so we call this synchronous method instead.
            AuthorizationResult authorizationResult = this.AcquireAuthorization();
#endif

            if (promptBehavior == PromptBehavior.Never && authorizationResult.Error == OAuthError.LoginRequired)
            {
                throw new AdalException(AdalError.UserInteractionRequired);
            }

            if (authorizationResult.Status == AuthorizationStatus.Success)
            {
                string uri = this.Authenticator.TokenUri;
                result = await OAuth2Request.SendTokenRequestAsync(uri, authorizationResult.Code, redirectUri, this.Resource, this.ClientKey.ClientId, this.CallState);
                VerifyUserMatch(result);
            }
            else
            {
                result = PlatformSpecificHelper.ProcessServiceError(authorizationResult.Error, authorizationResult.ErrorDescription);
            }

            return result;
        }

#if ADAL_WINRT
        internal async Task<AuthorizationResult> AcquireAuthorizationAsync()
        {
            return await OAuth2Request.SendAuthorizeRequestAsync(this.Authenticator, this.Resource, this.redirectUri, this.ClientKey.ClientId, this.userId, this.promptBehavior, this.extraQueryParameters, webUI, this.CallState);
        }
       
#else
        internal AuthorizationResult AcquireAuthorization()
        {
            AuthorizationResult authorizationResult = null;

            var sendAuthorizeRequest = new Action(
                delegate
                {
                    authorizationResult = this.SendAuthorizeRequest();
                });

            // If the thread is MTA, it cannot create or comunicate with WebBrowser which is a COM control.
            // In this case, we have to create the browser in an STA thread via StaTaskScheduler object.
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
            {
                using (var staTaskScheduler = new StaTaskScheduler(1))
                {
                    Task.Factory.StartNew(sendAuthorizeRequest, CancellationToken.None, TaskCreationOptions.None, staTaskScheduler).Wait();
                }
            }
            else
            {
                sendAuthorizeRequest();
            }

            return authorizationResult;
        }

        private AuthorizationResult SendAuthorizeRequest()
        {
            return OAuth2Request.SendAuthorizeRequest(this.Authenticator, this.Resource, this.redirectUri, this.ClientKey.ClientId, this.userId, this.promptBehavior, this.extraQueryParameters, this.webUI, this.CallState);
        }
#endif

        private void VerifyUserMatch(AuthenticationResult result)
        {
            if ((this.DisplayableId == null && this.UniqueId == null) || this.UserIdentifierType == UserIdentifierType.OptionalDisplayableId)
            {
                return;
            }

            string uniqueId = (result.UserInfo != null && result.UserInfo.UniqueId != null) ? result.UserInfo.UniqueId : "NULL";
            string displayableId = (result.UserInfo != null) ? result.UserInfo.DisplayableId : "NULL";

            if (this.UserIdentifierType == UserIdentifierType.UniqueId && string.Compare(uniqueId, this.UniqueId, StringComparison.Ordinal) != 0)
            {
                throw new AdalUserMismatchException(this.UniqueId, uniqueId);
            }

            if (this.UserIdentifierType == UserIdentifierType.RequiredDisplayableId && string.Compare(displayableId, this.DisplayableId, StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new AdalUserMismatchException(this.DisplayableId, displayableId);
            }
        }
    }
}
