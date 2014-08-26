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

using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal partial class AcquireTokenInteractiveHandler : AcquireTokenHandlerBase
    {
        internal AuthorizationResult authorizationResult;

        private Uri redirectUri;

        private string redirectUriRequestParameter;

        private PromptBehavior promptBehavior;

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

            this.SetRedirectUriRequestParameter();

            if (userId == null)
            {
                throw new ArgumentNullException("userId", AdalErrorMessage.SpecifyAnyUser);
            }

            this.userId = userId;

            this.promptBehavior = promptBehavior;

            if (!string.IsNullOrEmpty(extraQueryParameters) && extraQueryParameters[0] == '&')
            {
                extraQueryParameters = extraQueryParameters.Substring(1);
            }

            this.extraQueryParameters = extraQueryParameters;

            this.webUi = webUI;

            this.UniqueId = userId.UniqueId;
            this.DisplayableId = userId.DisplayableId;
            this.UserIdentifierType = userId.Type;

            this.LoadFromCache = (tokenCache != null && this.promptBehavior != PromptBehavior.Always && this.promptBehavior != PromptBehavior.RefreshSession);

            this.SupportADFS = true;
        }

        protected override void AddAditionalRequestParameters(RequestParameters requestParameters)
        {
            requestParameters[OAuthParameter.GrantType] = OAuthGrantType.AuthorizationCode;
            requestParameters[OAuthParameter.Code] = this.authorizationResult.Code;
            requestParameters[OAuthParameter.RedirectUri] = this.redirectUriRequestParameter;            
        }

        protected override void PostTokenRequest(AuthenticationResult result)
        {
            base.PostTokenRequest(result);
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
            RequestParameters authorizationRequestParameters = new RequestParameters(this.Resource, this.ClientKey);
            authorizationRequestParameters[OAuthParameter.ResponseType] = OAuthResponseType.Code;

            authorizationRequestParameters[OAuthParameter.RedirectUri] = this.redirectUriRequestParameter;

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
                authorizationRequestParameters[OAuthParameter.Prompt] = PromptValue.Login;
            }
            else if (promptBehavior == PromptBehavior.RefreshSession)
            {
                authorizationRequestParameters[OAuthParameter.Prompt] = PromptValue.RefreshSession;
            }
            else if (promptBehavior == PromptBehavior.Never)
            {
                authorizationRequestParameters[OAuthParameter.Prompt] = PromptValue.AttemptNone;
            }

            if (includeFormsAuthParam)
            {
                authorizationRequestParameters[OAuthParameter.FormsAuth] = OAuthValue.FormsAuth;
            }

            AdalIdHelper.AddAsQueryParameters(authorizationRequestParameters);

            if (!string.IsNullOrWhiteSpace(extraQueryParameters))
            {
                // Checks for extraQueryParameters duplicating standard parameters
                Dictionary<string, string> kvps = EncodingHelper.ParseKeyValueList(extraQueryParameters, '&', false, this.CallState);
                foreach (KeyValuePair<string, string> kvp in kvps)
                {
                    if (authorizationRequestParameters.ContainsKey(kvp.Key))
                    {
                        throw new AdalException(AdalError.DuplicateQueryParameter, string.Format(AdalErrorMessage.DuplicateQueryParameterTemplate, kvp.Key));
                    }
                }

                authorizationRequestParameters.ExtraQueryParameter = extraQueryParameters;
            }

            return authorizationRequestParameters;
        }

        private void VerifyAuthorizationResult()
        {
            if (this.promptBehavior == PromptBehavior.Never && this.authorizationResult.Error == OAuthError.LoginRequired)
            {
                throw new AdalException(AdalError.UserInteractionRequired);
            }

            if (this.authorizationResult.Status != AuthorizationStatus.Success)
            {
                throw new AdalServiceException(this.authorizationResult.Error, this.authorizationResult.ErrorDescription);
            }
        }
    }
}
