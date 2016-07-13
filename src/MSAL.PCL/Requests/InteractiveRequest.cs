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
using Microsoft.Identity.Client.Interfaces;
using Microsoft.Identity.Client.Internal;
using System.Globalization;

namespace Microsoft.Identity.Client.Requests
{
    internal class InteractiveRequest : BaseRequest
    {

        internal AuthorizationResult authorizationResult;

        private readonly HashSet<string> _additionalScope;
        private readonly Uri _redirectUri;
        private readonly string _redirectUriRequestParameter;
        private readonly IPlatformParameters _authorizationParameters;
        private readonly string _extraQueryParameters;
        private readonly IWebUI _webUi;
        private readonly string _loginHint;
        private readonly UiOptions? _uiOptions;


        public InteractiveRequest(AuthenticationRequestParameters authenticationRequestParameters,
            string[] additionalScope, Uri redirectUri, IPlatformParameters parameters, User user,
            UiOptions uiOptions, string extraQueryParameters, IWebUI webUI) :this(authenticationRequestParameters, additionalScope, redirectUri, parameters, user?.DisplayableId, uiOptions, extraQueryParameters, webUI)
        {
            this.User = user;
        }

        public InteractiveRequest(AuthenticationRequestParameters authenticationRequestParameters,
            string[] additionalScope, Uri redirectUri, IPlatformParameters parameters, string loginHint, UiOptions? uiOptions, string extraQueryParameters, IWebUI webUI)
            : base(authenticationRequestParameters)
        {
            this._redirectUri = PlatformPlugin.PlatformInformation.ValidateRedirectUri(redirectUri, this.CallState);

            if (!string.IsNullOrWhiteSpace(this._redirectUri.Fragment))
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
            this._redirectUriRequestParameter = PlatformPlugin.PlatformInformation.GetRedirectUriAsString(this._redirectUri, this.CallState);
            

            this._loginHint = loginHint;
            if (!string.IsNullOrWhiteSpace(extraQueryParameters) && extraQueryParameters[0] == '&')
            {
                extraQueryParameters = extraQueryParameters.Substring(1);
            }

            this._extraQueryParameters = extraQueryParameters;
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
            this.authorizationResult = await this._webUi.AcquireAuthorizationAsync(authorizationUri, this._redirectUri, headers, this.CallState).ConfigureAwait(false);
        }

        internal async Task<Uri> CreateAuthorizationUriAsync(Guid correlationId)
        {
            this.CallState.CorrelationId = correlationId;
            await this.Authenticator.UpdateFromTemplateAsync(this.CallState).ConfigureAwait(false);
            return this.CreateAuthorizationUri();
        }

        protected override void AddAditionalRequestParameters(DictionaryRequestParameters requestParameters)
        {
            requestParameters[OAuthParameter.GrantType] = OAuthGrantType.AuthorizationCode;
            requestParameters[OAuthParameter.Code] = this.authorizationResult.Code;
            requestParameters[OAuthParameter.RedirectUri] = this._redirectUriRequestParameter;
        }

        protected override void PostTokenRequest(AuthenticationResultEx resultEx)
        {
            base.PostTokenRequest(resultEx);
            //MSAL does not compare the input loginHint to the returned identifier anymore.
        }

        private Uri CreateAuthorizationUri()
        {
            IRequestParameters requestParameters = this.CreateAuthorizationRequest(_loginHint);
            return  new Uri(new Uri(this.Authenticator.AuthorizationUri), "?" + requestParameters);
        }

        private DictionaryRequestParameters CreateAuthorizationRequest(string loginHint)
        {
            HashSet<string> unionScope = this.GetDecoratedScope(new HashSet<string>(this.Scope.Union(this._additionalScope)));

            var authorizationRequestParameters = new DictionaryRequestParameters(unionScope, this.ClientKey);
            authorizationRequestParameters[OAuthParameter.ResponseType] = OAuthResponseType.Code;

            if (!string.IsNullOrWhiteSpace(this.Policy))
            {
                authorizationRequestParameters[OAuthParameter.Policy] = this.Policy;
            }

            authorizationRequestParameters[OAuthParameter.RedirectUri] = this._redirectUriRequestParameter;

            if (!string.IsNullOrWhiteSpace(loginHint))
            {
                authorizationRequestParameters[OAuthParameter.LoginHint] = loginHint;
            }

            if (this.CallState != null && this.CallState.CorrelationId != Guid.Empty)
            {
                authorizationRequestParameters[OAuthParameter.CorrelationId] = this.CallState.CorrelationId.ToString();
            }
            
                IDictionary<string, string> adalIdParameters = MsalIdHelper.GetMsalIdParameters();
                foreach (KeyValuePair<string, string> kvp in adalIdParameters)
                {
                    authorizationRequestParameters[kvp.Key] = kvp.Value;
                }

            AddUiOptionToRequestParameters(authorizationRequestParameters);

            if (!string.IsNullOrWhiteSpace(_extraQueryParameters))
            {
                // Checks for _extraQueryParameters duplicating standard parameters
                Dictionary<string, string> kvps = EncodingHelper.ParseKeyValueList(_extraQueryParameters, '&', false, this.CallState);
                foreach (KeyValuePair<string, string> kvp in kvps)
                {
                    if (authorizationRequestParameters.ContainsKey(kvp.Key))
                    {
                        throw new MsalException(MsalError.DuplicateQueryParameter, string.Format(CultureInfo.InvariantCulture,MsalErrorMessage.DuplicateQueryParameterTemplate, kvp.Key));
                    }
                }

                authorizationRequestParameters.ExtraQueryParameter = _extraQueryParameters;
            }

            return authorizationRequestParameters;
        }

        private void VerifyAuthorizationResult()
        {
            if (this.authorizationResult.Error == OAuthError.LoginRequired)
            {
                throw new MsalException(MsalError.UserInteractionRequired);
            }

            if (this.authorizationResult.Status != AuthorizationStatus.Success)
            {
                throw new MsalServiceException(this.authorizationResult.Error, this.authorizationResult.ErrorDescription);
            }
        }

        private void AddUiOptionToRequestParameters(DictionaryRequestParameters authorizationRequestParameters)
        {
            switch (this._uiOptions)
            {
                case UiOptions.ForceConsent:
                    authorizationRequestParameters[OAuthParameter.Prompt]= "consent";
                    break;

                case UiOptions.ForceLogin:
                    authorizationRequestParameters[OAuthParameter.Prompt] = "login";
                    break;

                case UiOptions.SelectAccount:
                    authorizationRequestParameters[OAuthParameter.Prompt] = "select_account";
                    break;

                case UiOptions.ActAsCurrentUser:
                    authorizationRequestParameters[OAuthParameter.RestrictToHint] = "true";
                    break;
            }
            
        }
    }
}
