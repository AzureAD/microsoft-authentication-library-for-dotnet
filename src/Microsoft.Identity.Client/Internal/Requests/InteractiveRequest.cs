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
using Microsoft.Identity.Client.Internal.Interfaces;
using Microsoft.Identity.Client.Internal.OAuth2;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class InteractiveRequest : BaseRequest
    {
        private readonly SortedSet<string> _additionalScope;
        private readonly UIBehavior _UIBehavior;
        private readonly IWebUI _webUi;
        private AuthorizationResult _authorizationResult;
        private string _codeVerifier;
        private string _state;

        public InteractiveRequest(AuthenticationRequestParameters authenticationRequestParameters,
            IEnumerable<string> additionalScope, UIBehavior UIBehavior, IWebUI webUI)
            : this(
                authenticationRequestParameters, additionalScope, authenticationRequestParameters.User?.DisplayableId,
                UIBehavior, webUI)
        {
        }

        public InteractiveRequest(AuthenticationRequestParameters authenticationRequestParameters,
            IEnumerable<string> additionalScope, string loginHint,
            UIBehavior UIBehavior, IWebUI webUI)
            : base(authenticationRequestParameters)
        {
            PlatformPlugin.PlatformInformation.ValidateRedirectUri(authenticationRequestParameters.RedirectUri,
                RequestContext);
            if (!string.IsNullOrWhiteSpace(authenticationRequestParameters.RedirectUri.Fragment))
            {
                throw new ArgumentException(MsalErrorMessage.RedirectUriContainsFragment, nameof(authenticationRequestParameters.RedirectUri));
            }

            _additionalScope = new SortedSet<string>();
            if (!MsalHelpers.IsNullOrEmpty(additionalScope))
            {
                _additionalScope = additionalScope.CreateSetFromEnumerable();
            }

            ValidateScopeInput(_additionalScope);
            
            authenticationRequestParameters.LoginHint = loginHint;
            if (!string.IsNullOrWhiteSpace(authenticationRequestParameters.ExtraQueryParameters) &&
                authenticationRequestParameters.ExtraQueryParameters[0] == '&')
            {
                authenticationRequestParameters.ExtraQueryParameters =
                    authenticationRequestParameters.ExtraQueryParameters.Substring(1);
            }

            _webUi = webUI;
            _UIBehavior = UIBehavior;
            LoadFromCache = false; //no cache lookup and refresh for interactive.
        }

        internal override async Task PreTokenRequest()
        {
            await base.PreTokenRequest().ConfigureAwait(false);
            
            await AcquireAuthorizationAsync().ConfigureAwait(false);
            VerifyAuthorizationResult();
        }

        internal async Task AcquireAuthorizationAsync()
        {
            Uri authorizationUri = CreateAuthorizationUri(true, true);
            _authorizationResult =
                await
                    _webUi.AcquireAuthorizationAsync(authorizationUri, AuthenticationRequestParameters.RedirectUri,
                        RequestContext)
                        .ConfigureAwait(false);
        }

        internal async Task<Uri> CreateAuthorizationUriAsync(RequestContext requestContext)
        {
            //this method is used in confidential clients to create authorization URLs.
            RequestContext = requestContext;
            await AuthenticationRequestParameters.Authority.ResolveEndpointsAsync(AuthenticationRequestParameters.LoginHint, RequestContext).ConfigureAwait(false);
            return CreateAuthorizationUri();
        }

        protected override void SetAdditionalRequestParameters(OAuth2Client client)
        {
            client.AddBodyParameter(OAuth2Parameter.GrantType, OAuth2GrantType.AuthorizationCode);
            client.AddBodyParameter(OAuth2Parameter.Code, _authorizationResult.Code);
            client.AddBodyParameter(OAuth2Parameter.RedirectUri, AuthenticationRequestParameters.RedirectUri.OriginalString);
            client.AddBodyParameter(OAuth2Parameter.CodeVerifier, _codeVerifier);
        }

        private Uri CreateAuthorizationUri(bool addVerifier = false, bool addState = false)
        {
            IDictionary<string, string> requestParameters = CreateAuthorizationRequestParameters();

            if (addVerifier)
            {
                _codeVerifier = PlatformPlugin.CryptographyHelper.GenerateCodeVerifier();
                string codeVerifierHash = PlatformPlugin.CryptographyHelper.CreateBase64UrlEncodedSha256Hash(_codeVerifier);

                requestParameters[OAuth2Parameter.CodeChallenge] = codeVerifierHash;
                requestParameters[OAuth2Parameter.CodeChallengeMethod] = OAuth2Value.CodeChallengeMethodValue;
            }

            if (addState)
            {
                _state = Guid.NewGuid().ToString();
                requestParameters[OAuth2Parameter.State] = _state;
            }

            //add uid/utid values to QP if user object was passed in.
            if(AuthenticationRequestParameters.User != null)
            {
                if (!string.IsNullOrEmpty(AuthenticationRequestParameters.User.DisplayableId))
                {
                    requestParameters[OAuth2Parameter.LoginHint] = AuthenticationRequestParameters.User.DisplayableId;
                }

                AuthenticationRequestParameters.ClientInfo = ClientInfo.CreateFromEncodedString(AuthenticationRequestParameters.User.Identifier);

                if (!string.IsNullOrEmpty(AuthenticationRequestParameters.ClientInfo.UniqueIdentifier))
                {
                    requestParameters[OAuth2Parameter.LoginReq] = AuthenticationRequestParameters.ClientInfo.UniqueIdentifier;
                }

                if (!string.IsNullOrEmpty(AuthenticationRequestParameters.ClientInfo.UniqueTenantIdentifier))
                {
                    requestParameters[OAuth2Parameter.DomainReq] = AuthenticationRequestParameters.ClientInfo.UniqueTenantIdentifier;
                }
            }


            if (!string.IsNullOrWhiteSpace(AuthenticationRequestParameters.ExtraQueryParameters))
            {
                // Checks for _extraQueryParameters duplicating standard parameters
                Dictionary<string, string> kvps =
                    MsalHelpers.ParseKeyValueList(AuthenticationRequestParameters.ExtraQueryParameters, '&', false,
                        RequestContext);

                foreach (KeyValuePair<string, string> kvp in kvps)
                {
                    if (requestParameters.ContainsKey(kvp.Key))
                    {
                        throw new MsalClientException(MsalClientException.DuplicateQueryParameterError,
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

            UriBuilder builder = new UriBuilder(new Uri(AuthenticationRequestParameters.Authority.AuthorizationEndpoint)) {Query = qp};
            return new Uri(MsalHelpers.CheckForExtraQueryParameter(builder.ToString()));

        }

        private Dictionary<string, string> CreateAuthorizationRequestParameters()
        {
            SortedSet<string> unionScope =
                GetDecoratedScope(
                    new SortedSet<string>(AuthenticationRequestParameters.Scope.Union(_additionalScope)));

            Dictionary<string, string> authorizationRequestParameters = new Dictionary<string, string>();
            authorizationRequestParameters[OAuth2Parameter.Scope] = unionScope.AsSingleString();
            authorizationRequestParameters[OAuth2Parameter.ResponseType] = OAuth2ResponseType.Code;

            authorizationRequestParameters[OAuth2Parameter.ClientId] = AuthenticationRequestParameters.ClientId;
            authorizationRequestParameters[OAuth2Parameter.RedirectUri] =
                AuthenticationRequestParameters.RedirectUri.OriginalString;

            if (!string.IsNullOrWhiteSpace(AuthenticationRequestParameters.LoginHint))
            {
                authorizationRequestParameters[OAuth2Parameter.LoginHint] = AuthenticationRequestParameters.LoginHint;
            }

            if (!string.IsNullOrEmpty(RequestContext?.CorrelationId))
            {
                authorizationRequestParameters[OAuth2Parameter.CorrelationId] = RequestContext.CorrelationId;
            }

            IDictionary<string, string> adalIdParameters = MsalIdHelper.GetMsalIdParameters();
            foreach (KeyValuePair<string, string> kvp in adalIdParameters)
            {
                authorizationRequestParameters[kvp.Key] = kvp.Value;
            }

            authorizationRequestParameters[OAuth2Parameter.Prompt] = _UIBehavior.PromptValue;
            return authorizationRequestParameters;
        }

        private void VerifyAuthorizationResult()
        {
            if (!_state.Equals(_authorizationResult.State))
            {
                throw new MsalClientException(MsalClientException.StateMismatchError,
                    string.Format(CultureInfo.InvariantCulture, "Returned state({0}) from authorize endpoint is not the same as the one sent({1})", _authorizationResult.State, _state));
            }

            if (_authorizationResult.Error == OAuth2Error.LoginRequired)
            {
                throw new MsalUiRequiredException(MsalUiRequiredException.NoPromptFailedError,
                    MsalErrorMessage.NoPromptFailedErrorMessage);
            }

            if (_authorizationResult.Status != AuthorizationStatus.Success)
            {
                throw new MsalServiceException(_authorizationResult.Error,
                    _authorizationResult.ErrorDescription);
            }
        }
    }
}