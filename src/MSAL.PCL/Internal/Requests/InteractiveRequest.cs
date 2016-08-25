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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Interfaces;
using Microsoft.Identity.Client.Internal.OAuth2;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class InteractiveRequest : BaseRequest
    {
        private readonly HashSet<string> _additionalScope;
        private readonly IPlatformParameters _authorizationParameters;
        private readonly UiOptions? _uiOptions;
        private readonly IWebUI _webUi;
        private AuthorizationResult _authorizationResult;

        public InteractiveRequest(AuthenticationRequestParameters authenticationRequestParameters,
            string[] additionalScope, IPlatformParameters parameters, User user,
            UiOptions uiOptions, IWebUI webUI)
            : this(
                authenticationRequestParameters, additionalScope, parameters, user?.DisplayableId,
                uiOptions, webUI)
        {
            this.User = user;
        }

        public InteractiveRequest(AuthenticationRequestParameters authenticationRequestParameters,
            string[] additionalScope, IPlatformParameters parameters, string loginHint,
            UiOptions? uiOptions, IWebUI webUI)
            : base(authenticationRequestParameters)
        {
            PlatformPlugin.PlatformInformation.ValidateRedirectUri(authenticationRequestParameters.RedirectUri,
                this.CallState);
            if (!string.IsNullOrWhiteSpace(authenticationRequestParameters.RedirectUri.Fragment))
            {
                throw new ArgumentException(MsalErrorMessage.RedirectUriContainsFragment, "redirectUri");
            }

            _additionalScope = new HashSet<string>();
            if (!MsalStringHelper.IsNullOrEmpty(additionalScope))
            {
                this._additionalScope = additionalScope.CreateSetFromArray();
            }

            ValidateScopeInput(this._additionalScope);

            this._authorizationParameters = parameters;


            authenticationRequestParameters.LoginHint = loginHint;
            if (!string.IsNullOrWhiteSpace(authenticationRequestParameters.ExtraQueryParameters) && authenticationRequestParameters.ExtraQueryParameters[0] == '&')
            {
                authenticationRequestParameters.ExtraQueryParameters = authenticationRequestParameters.ExtraQueryParameters.Substring(1);
            }
            
            this._webUi = webUI;
            this._uiOptions = uiOptions;
            this.LoadFromCache = false; //no cache lookup and refresh for interactive.
            this.SupportADFS = false;

            if (string.IsNullOrWhiteSpace(loginHint) && _uiOptions == UiOptions.ActAsCurrentUser)
            {
                throw new ArgumentException(MsalErrorMessage.LoginHintNullForUiOption, "loginHint");
            }

            PlatformPlugin.BrokerHelper.PlatformParameters = _authorizationParameters;
        }

        internal override async Task PreTokenRequest()
        {
            //TODO commented code should be uncommented as per https://github.com/AzureAD/MSAL-Prototype/issues/66
            IDictionary<string, string> headers = new Dictionary<string, string>();
            //headers["x-ms-sso-Ignore-SSO"] = "1";

            await base.PreTokenRequest().ConfigureAwait(false);

/*            if (this.tokenCache!=null && this.User!=null  && _uiOptions == UiOptions.ActAsCurrentUser)

            {
                bool notifiedBeforeAccessCache = false;
                try
                {
                    this.NotifyBeforeAccessCache();
                    notifiedBeforeAccessCache = true;

                    AuthenticationResultEx resultEx = this.tokenCache.LoadFromCache(this.Authenticator.Authority,
                        this.Scope,
                        this.ClientKey.ClientId, this.User,
                        this.Policy, this.CallState);
                    if (resultEx != null && !string.IsNullOrWhiteSpace(resultEx.RefreshToken))
                    {
                        headers["x-ms-sso-RefreshToken"] = resultEx.RefreshToken;
                    }
                }
                finally
                {
                    if (notifiedBeforeAccessCache)
                    {
                        this.NotifyAfterAccessCache();
                    }

                }
            }*/

            // We do not have async interactive API in .NET, so we call this synchronous method instead.
            await this.AcquireAuthorizationAsync(headers).ConfigureAwait(false);
            this.VerifyAuthorizationResult();
        }

        internal async Task AcquireAuthorizationAsync(IDictionary<string, string> headers)
        {
            Uri authorizationUri = this.CreateAuthorizationUri();
            this._authorizationResult =
                await
                    this._webUi.AcquireAuthorizationAsync(authorizationUri, AuthenticationRequestParameters.RedirectUri,
                        headers, this.CallState)
                        .ConfigureAwait(false);
        }

        internal async Task<Uri> CreateAuthorizationUriAsync(CallState callState)
        {
            this.CallState = callState;
            await this.Authority.UpdateFromTemplateAsync(this.CallState).ConfigureAwait(false);
            return this.CreateAuthorizationUri();
        }

        protected override void SetAdditionalRequestParameters(OAuth2Client client)
        {
            client.AddBodyParameter(OAuth2Parameter.GrantType, OAuth2GrantType.AuthorizationCode);
            client.AddBodyParameter(OAuth2Parameter.Code, this._authorizationResult.Code);
            client.AddBodyParameter(OAuth2Parameter.RedirectUri, AuthenticationRequestParameters.RedirectUri.AbsoluteUri);
        }

        protected override void PostTokenRequest(AuthenticationResultEx resultEx)
        {
            base.PostTokenRequest(resultEx);
        }

        private Uri CreateAuthorizationUri()
        {
            IDictionary<string, string> requestParameters = this.CreateAuthorizationRequestParameters();

            if (!string.IsNullOrWhiteSpace(AuthenticationRequestParameters.ExtraQueryParameters))
            {
                // Checks for _extraQueryParameters duplicating standard parameters
                Dictionary<string, string> kvps =
                    EncodingHelper.ParseKeyValueList(AuthenticationRequestParameters.ExtraQueryParameters, '&', false,
                        this.CallState);
                foreach (KeyValuePair<string, string> kvp in kvps)
                {
                    if (requestParameters.ContainsKey(kvp.Key))
                    {
                        throw new MsalException(MsalError.DuplicateQueryParameter,
                            string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.DuplicateQueryParameterTemplate,
                                kvp.Key));
                    }
                }
            }

            string qp = requestParameters.ToQueryParameter();
            if (!string.IsNullOrEmpty(AuthenticationRequestParameters.ExtraQueryParameters))
            {
                qp += "&" + AuthenticationRequestParameters.ExtraQueryParameters;
            }

            return new Uri(new Uri(this.Authority.AuthorizationEndpoint), "?" + qp);
        }

        private Dictionary<string, string> CreateAuthorizationRequestParameters()
        {
            HashSet<string> unionScope =
                this.GetDecoratedScope(
                    new HashSet<string>(AuthenticationRequestParameters.Scope.Union(this._additionalScope)));

            Dictionary<string, string> authorizationRequestParameters =
                new Dictionary<string, string>(AuthenticationRequestParameters.ClientKey.ToParameters());
            authorizationRequestParameters[OAuth2Parameter.Scope] = unionScope.AsSingleString();
            authorizationRequestParameters[OAuth2Parameter.ResponseType] = OAuth2ResponseType.Code;

            if (!string.IsNullOrWhiteSpace(AuthenticationRequestParameters.Policy))
            {
                authorizationRequestParameters[OAuth2Parameter.Policy] = AuthenticationRequestParameters.Policy;
            }

            authorizationRequestParameters[OAuth2Parameter.RedirectUri] =
                AuthenticationRequestParameters.RedirectUri.AbsoluteUri;

            if (!string.IsNullOrWhiteSpace(AuthenticationRequestParameters.LoginHint))
            {
                authorizationRequestParameters[OAuth2Parameter.LoginHint] = AuthenticationRequestParameters.LoginHint;
            }

            if (this.CallState != null && !string.IsNullOrEmpty(CallState.CorrelationId))
            {
                authorizationRequestParameters[OAuth2Parameter.CorrelationId] = this.CallState.CorrelationId;
            }

            IDictionary<string, string> adalIdParameters = MsalIdHelper.GetMsalIdParameters();
            foreach (KeyValuePair<string, string> kvp in adalIdParameters)
            {
                authorizationRequestParameters[kvp.Key] = kvp.Value;
            }

            AddUiOptionToRequestParameters(authorizationRequestParameters);
            return authorizationRequestParameters;
        }

        private void VerifyAuthorizationResult()
        {
            if (this._authorizationResult.Error == OAuth2Error.LoginRequired)
            {
                throw new MsalException(MsalError.UserInteractionRequired);
            }

            if (this._authorizationResult.Status != AuthorizationStatus.Success)
            {
                throw new MsalServiceException(this._authorizationResult.Error,
                    this._authorizationResult.ErrorDescription);
            }
        }

        private void AddUiOptionToRequestParameters(Dictionary<string, string> authorizationRequestParameters)
        {
            switch (this._uiOptions)
            {
                case UiOptions.ForceConsent:
                    authorizationRequestParameters[OAuth2Parameter.Prompt] = "consent";
                    break;

                case UiOptions.ForceLogin:
                    authorizationRequestParameters[OAuth2Parameter.Prompt] = "login";
                    break;

                case UiOptions.SelectAccount:
                    authorizationRequestParameters[OAuth2Parameter.Prompt] = "select_account";
                    break;

                case UiOptions.ActAsCurrentUser:
                    authorizationRequestParameters[OAuth2Parameter.RestrictToHint] = "true";
                    break;
            }
        }
    }
}