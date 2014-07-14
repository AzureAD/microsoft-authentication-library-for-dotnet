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
using System.Threading;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    class AcquireTokenInteractiveHandler : AcquireTokenHandlerBase
    {
        internal AuthorizationResult authorizationResult;

        private readonly Uri redirectUri;

        private readonly PromptBehavior promptBehavior;

        private readonly string extraQueryParameters;

        private readonly IWebUI webUi;

        private readonly UserIdentifier userId;

        public AcquireTokenInteractiveHandler(Authenticator authenticator, TokenCache tokenCache, string resource, string clientId, Uri redirectUri, PromptBehavior promptBehavior, UserIdentifier userId, string extraQueryParameters, IWebUI webUI, bool callSync)
            : base(authenticator, tokenCache, resource, new ClientKey(clientId), TokenSubjectType.User, callSync)
        {
            if (redirectUri == null)
            {
                throw new ArgumentNullException("redirectUri");
            }

            if (!string.IsNullOrWhiteSpace(redirectUri.Fragment))
            {
                throw new ArgumentException(AdalErrorMessage.RedirectUriContainsFragment, "redirectUri");
            }

            this.redirectUri = redirectUri;

            if (userId == null)
            {
                throw new ArgumentNullException("userId", AdalErrorMessage.SpecifyAnyUser);
            }

            this.userId = userId;

            this.promptBehavior = promptBehavior;

            if (extraQueryParameters != null && extraQueryParameters[0] == '&')
            {
                extraQueryParameters = extraQueryParameters.Substring(1);
            }

            this.extraQueryParameters = extraQueryParameters;

            this.webUi = webUI;

            this.UniqueId = userId.UniqueId;
            this.DisplayableId = userId.DisplayableId;
            this.UserIdentifierType = userId.Type;

            this.LoadFromCache = (tokenCache != null && promptBehavior != PromptBehavior.Always && promptBehavior != PromptBehavior.RefreshSession);

            this.SupportADFS = true;
        }

#if ADAL_WINRT
        protected override async Task PreTokenRequest()
        {
            await this.AcquireAuthorizationAsync();
            VerifyAuthorizationResult();
        }
#else
        protected override Task PreTokenRequest()
        {
            // We do not have async interactive API in .NET, so we call this synchronous method instead.
            this.AcquireAuthorization();
            VerifyAuthorizationResult();

            return CompletedTask;
        }
#endif

        protected override void AddAditionalRequestParameters(RequestParameters requestParameters)
        {
            requestParameters[OAuthParameter.GrantType] = OAuthGrantType.AuthorizationCode;
            requestParameters[OAuthParameter.Code] = this.authorizationResult.Code;
            requestParameters[OAuthParameter.RedirectUri] = redirectUri.AbsoluteUri;            
        }

        protected override void PostTokenRequest(AuthenticationResult result)
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

#if ADAL_WINRT
        private async Task AcquireAuthorizationAsync()
        {
            Uri authorizationUri = this.CreateAuthorizationUri(await IncludeFormsAuthParamsAsync());
            this.authorizationResult = await webUi.AuthenticateAsync(authorizationUri, this.redirectUri, this.CallState);
        }

        internal static async Task<bool> IncludeFormsAuthParamsAsync()
        {
            return PlatformSpecificHelper.IsDomainJoined() && await PlatformSpecificHelper.IsUserLocalAsync();
        }
#else

        internal void AcquireAuthorization()
        {
            var sendAuthorizeRequest = new Action(
                delegate
                {
                    Uri authorizationUri = this.CreateAuthorizationUri(IncludeFormsAuthParams());
                    string resultUri = this.webUi.Authenticate(authorizationUri, this.redirectUri);
                    this.authorizationResult = OAuth2Response.ParseAuthorizeResponse(resultUri, this.CallState);
                });

            // If the thread is MTA, it cannot create or communicate with WebBrowser which is a COM control.
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
        }

        internal static bool IncludeFormsAuthParams()
        {
            return PlatformSpecificHelper.IsUserLocal() && PlatformSpecificHelper.IsDomainJoined();
        }
#endif

        private Uri CreateAuthorizationUri(bool includeFormsAuthParam)
        {
            string loginHint = null;

            if (!userId.IsAnyUser
                && (userId.Type == UserIdentifierType.OptionalDisplayableId
                    || userId.Type == UserIdentifierType.RequiredDisplayableId))
            {
                loginHint = userId.Id;
            }

            RequestParameters requestParameters = this.CreateAuthorizationRequest(loginHint, includeFormsAuthParam);

            var authorizationUri = new Uri(new Uri(this.Authenticator.AuthorizationUri), "?" + requestParameters);
            authorizationUri = new Uri(HttpHelper.CheckForExtraQueryParameter(authorizationUri.AbsoluteUri));

            return authorizationUri;
        }

        private RequestParameters CreateAuthorizationRequest(string loginHint, bool includeFormsAuthParam)
        {
            RequestParameters authorizationRequestParameters = new RequestParameters(this.Resource, this.ClientKey, null);
            authorizationRequestParameters[OAuthParameter.ResponseType] = OAuthResponseType.Code;
            authorizationRequestParameters[OAuthParameter.RedirectUri] = redirectUri.AbsoluteUri;

            if (!string.IsNullOrWhiteSpace(loginHint))
            {
                authorizationRequestParameters[OAuthParameter.LoginHint] = loginHint;
            }

            if (this.CallState != null && this.CallState.CorrelationId != Guid.Empty)
            {
                authorizationRequestParameters[OAuthParameter.CorrelationId] = this.CallState.CorrelationId.ToString();
            }

            // ADFS currently ignores the parameter for now.
            if (promptBehavior == PromptBehavior.Always)
            {
                authorizationRequestParameters[OAuthParameter.Prompt] = OAuthValue.PromptLogin;
            }
            else if (promptBehavior == PromptBehavior.RefreshSession)
            {
                authorizationRequestParameters[OAuthParameter.Prompt] = OAuthValue.PromptRefreshSession;
            }

            if (!string.IsNullOrWhiteSpace(extraQueryParameters))
            {
                authorizationRequestParameters.ExtraQueryParameter = extraQueryParameters;
            }

            if (includeFormsAuthParam)
            {
                authorizationRequestParameters[OAuthParameter.FormsAuth] = OAuthValue.FormsAuth;
            }

            AdalIdHelper.AddAsQueryParameters(authorizationRequestParameters);

            return authorizationRequestParameters;
        }

        private void VerifyAuthorizationResult()
        {
            if (this.promptBehavior == PromptBehavior.Never && authorizationResult.Error == OAuthError.LoginRequired)
            {
                throw new AdalException(AdalError.UserInteractionRequired);
            }

            if (authorizationResult.Status != AuthorizationStatus.Success)
            {
                throw new AdalServiceException(authorizationResult.Error, authorizationResult.ErrorDescription);
            }
        }
    }
}
