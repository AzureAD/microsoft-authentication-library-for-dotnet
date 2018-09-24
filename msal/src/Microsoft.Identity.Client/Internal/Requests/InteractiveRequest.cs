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
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.Identity.Core.Telemetry;
using Microsoft.Identity.Core.UI;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class InteractiveRequest : RequestBase
    {
        private readonly SortedSet<string> _extraScopesToConsent;
        private readonly UIBehavior _UIBehavior;
        private readonly IWebUI _webUi;
        private AuthorizationResult _authorizationResult;
        private string _codeVerifier;
        private string _state;

        public InteractiveRequest(AuthenticationRequestParameters authenticationRequestParameters,
            IEnumerable<string> extraScopesToConsent, UIBehavior UIBehavior, IWebUI webUI)
            : this(
                authenticationRequestParameters, extraScopesToConsent, authenticationRequestParameters.Account?.Username,
                UIBehavior, webUI)
        {
        }

        public InteractiveRequest(AuthenticationRequestParameters authenticationRequestParameters,
            IEnumerable<string> extraScopesToConsent, string loginHint,
            UIBehavior UIBehavior, IWebUI webUI)
            : base(authenticationRequestParameters)
        {
            if (!string.IsNullOrWhiteSpace(authenticationRequestParameters.RedirectUri.Fragment))
            {
                throw new ArgumentException(MsalErrorMessage.RedirectUriContainsFragment, nameof(authenticationRequestParameters.RedirectUri));
            }

            _extraScopesToConsent = new SortedSet<string>();
            if (!CoreHelpers.IsNullOrEmpty(extraScopesToConsent))
            {
                _extraScopesToConsent = extraScopesToConsent.CreateSetFromEnumerable();
            }

            ValidateScopeInput(_extraScopesToConsent);

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
            var msg = "Additional scopes - " + _extraScopesToConsent.AsSingleString() + ";" + "UIBehavior - " +
                            _UIBehavior.PromptValue;
            AuthenticationRequestParameters.RequestContext.Logger.Info(msg);
            AuthenticationRequestParameters.RequestContext.Logger.InfoPii(msg);
        }

        protected override string GetUIBehaviorPromptValue()
        {
            return _UIBehavior.PromptValue;
        }

        internal override async Task PreTokenRequestAsync()
        {
            await base.PreTokenRequestAsync().ConfigureAwait(false);
            await AcquireAuthorizationAsync().ConfigureAwait(false);
            VerifyAuthorizationResult();
        }

        internal async Task AcquireAuthorizationAsync()
        {
            Uri authorizationUri = CreateAuthorizationUri(true, true);

            var uiEvent = new UiEvent();
            using (CoreTelemetryService.CreateTelemetryHelper(AuthenticationRequestParameters.RequestContext.TelemetryRequestId, uiEvent))
            {
                _authorizationResult = await
                    _webUi.AcquireAuthorizationAsync(authorizationUri, AuthenticationRequestParameters.RedirectUri,
                        AuthenticationRequestParameters.RequestContext)
                        .ConfigureAwait(false);
                uiEvent.UserCancelled = _authorizationResult.Status == AuthorizationStatus.UserCancel;
                uiEvent.AccessDenied = _authorizationResult.Status == AuthorizationStatus.ProtocolError;
            }
        }

        internal async Task<Uri> CreateAuthorizationUriAsync()
        {
            await AuthenticationRequestParameters.Authority.UpdateCanonicalAuthorityAsync
                (AuthenticationRequestParameters.RequestContext).ConfigureAwait(false);

            //this method is used in confidential clients to create authorization URLs.
            await AuthenticationRequestParameters.Authority.ResolveEndpointsAsync(AuthenticationRequestParameters.LoginHint, AuthenticationRequestParameters.RequestContext).ConfigureAwait(false);
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
                _codeVerifier = CoreCryptographyHelpers.GenerateCodeVerifier();
                string codeVerifierHash = CoreCryptographyHelpers.CreateBase64UrlEncodedSha256Hash(_codeVerifier);

                requestParameters[OAuth2Parameter.CodeChallenge] = codeVerifierHash;
                requestParameters[OAuth2Parameter.CodeChallengeMethod] = OAuth2Value.CodeChallengeMethodValue;
            }

            if (addState)
            {
                _state = Guid.NewGuid().ToString();
                requestParameters[OAuth2Parameter.State] = _state;
            }
            //add uid/utid values to QP if user object was passed in.
            if (AuthenticationRequestParameters.Account != null)
            {
                if (!string.IsNullOrEmpty(AuthenticationRequestParameters.Account.Username))
                {
                    requestParameters[OAuth2Parameter.LoginHint] = AuthenticationRequestParameters.Account.Username;
                }

                AuthenticationRequestParameters.ClientInfo = AuthenticationRequestParameters.Account.HomeAccountId.ToClientInfo();

                if (!string.IsNullOrEmpty(AuthenticationRequestParameters.ClientInfo.UniqueObjectIdentifier))
                {
                    requestParameters[OAuth2Parameter.LoginReq] = AuthenticationRequestParameters.ClientInfo.UniqueObjectIdentifier;
                }

                if (!string.IsNullOrEmpty(AuthenticationRequestParameters.ClientInfo.UniqueTenantIdentifier))
                {
                    requestParameters[OAuth2Parameter.DomainReq] = AuthenticationRequestParameters.ClientInfo.UniqueTenantIdentifier;
                }
            }

            CheckForDuplicateQueryParameters(AuthenticationRequestParameters.ExtraQueryParameters, requestParameters);
            CheckForDuplicateQueryParameters(AuthenticationRequestParameters.SliceParameters, requestParameters);

            string qp = requestParameters.ToQueryParameter();
            UriBuilder builder =
                new UriBuilder(new Uri(AuthenticationRequestParameters.Authority.AuthorizationEndpoint));
            builder.AppendQueryParameters(qp);

            return builder.Uri;
        }

        private void CheckForDuplicateQueryParameters(string queryParams, IDictionary<string, string> requestParameters)
        {
            if (!string.IsNullOrWhiteSpace(queryParams))
            {
                // Checks for _extraQueryParameters duplicating standard parameters
                Dictionary<string, string> kvps =
                    CoreHelpers.ParseKeyValueList(queryParams, '&', false,
                        AuthenticationRequestParameters.RequestContext);

                foreach (KeyValuePair<string, string> kvp in kvps)
                {
                    if (requestParameters.ContainsKey(kvp.Key))
                    {
                        throw new MsalClientException(MsalClientException.DuplicateQueryParameterError,
                            string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.DuplicateQueryParameterTemplate,
                                kvp.Key));
                    }

                    requestParameters[kvp.Key] = kvp.Value;
                }
            }
        }

        private Dictionary<string, string> CreateAuthorizationRequestParameters()
        {
            SortedSet<string> unionScope =
                GetDecoratedScope(
                    new SortedSet<string>(AuthenticationRequestParameters.Scope.Union(_extraScopesToConsent)));

            var authorizationRequestParameters = new Dictionary<string, string>
            {
                [OAuth2Parameter.Scope] = unionScope.AsSingleString(),
                [OAuth2Parameter.ResponseType] = OAuth2ResponseType.Code,

                [OAuth2Parameter.ClientId] = AuthenticationRequestParameters.ClientId,
                [OAuth2Parameter.RedirectUri] =
                AuthenticationRequestParameters.RedirectUri.OriginalString
            };

            if (!string.IsNullOrWhiteSpace(AuthenticationRequestParameters.LoginHint))
            {
                authorizationRequestParameters[OAuth2Parameter.LoginHint] = AuthenticationRequestParameters.LoginHint;
            }

            if (AuthenticationRequestParameters.RequestContext?.Logger?.CorrelationId != Guid.Empty)
            {
                authorizationRequestParameters[OAuth2Parameter.CorrelationId] = AuthenticationRequestParameters.RequestContext.Logger.CorrelationId.ToString();
            }

            foreach (var kvp in MsalIdHelper.GetMsalIdParameters())
            {
                authorizationRequestParameters[kvp.Key] = kvp.Value;
            }

            authorizationRequestParameters[OAuth2Parameter.Prompt] = _UIBehavior.PromptValue;
            return authorizationRequestParameters;
        }

        private void VerifyAuthorizationResult()
        {
            if (_authorizationResult.Status == AuthorizationStatus.Success && !_state.Equals(_authorizationResult.State, StringComparison.OrdinalIgnoreCase))
            {
                throw new MsalClientException(MsalClientException.StateMismatchError,
                    string.Format(CultureInfo.InvariantCulture, "Returned state({0}) from authorize endpoint is not the same as the one sent({1})", _authorizationResult.State, _state));
            }

            if (_authorizationResult.Error == OAuth2Error.LoginRequired)
            {
                throw new MsalUiRequiredException(MsalUiRequiredException.NoPromptFailedError,
                    MsalErrorMessage.NoPromptFailedErrorMessage);
            }

            if (_authorizationResult.Status == AuthorizationStatus.UserCancel)
            {
                throw new MsalClientException(_authorizationResult.Error,
                _authorizationResult.ErrorDescription);
            }

            if (_authorizationResult.Status != AuthorizationStatus.Success)
            {
                throw new MsalServiceException(_authorizationResult.Error,
                _authorizationResult.ErrorDescription);
            }
        }
    }
}
