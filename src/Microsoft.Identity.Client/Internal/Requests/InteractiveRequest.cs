// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Mats.Internal.Events;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class InteractiveRequest : RequestBase
    {
        private readonly SortedSet<string> _extraScopesToConsent;
        private readonly IWebUI _webUi;
        private AuthorizationResult _authorizationResult;
        private string _codeVerifier;
        private string _state;
        private readonly AcquireTokenInteractiveParameters _interactiveParameters;
        private MsalTokenResponse _msalTokenResponse;
#if WINDOWS_APP
        private readonly string _redirectUriRequestParameter;
#endif //WINDOWS_APP

        public InteractiveRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenInteractiveParameters interactiveParameters,
            IWebUI webUi)
            : base(serviceBundle, authenticationRequestParameters, interactiveParameters)
        {
            _webUi = webUi; // can be null just to generate the authorization uri 

            _interactiveParameters = interactiveParameters;
            RedirectUriHelper.Validate(authenticationRequestParameters.RedirectUri);

#if WINDOWS_APP
            _redirectUriRequestParameter = PlatformProxyFactory.CreatePlatformProxy(serviceBundle.DefaultLogger).GetBrokerOrRedirectUri(authenticationRequestParameters.RedirectUri);
#endif //WINDOWS_APP

            // todo(migration): can't this just come directly from interactive parameters instead of needing do to this?
            _extraScopesToConsent = new SortedSet<string>();
            if (!_interactiveParameters.ExtraScopesToConsent.IsNullOrEmpty())
            {
                _extraScopesToConsent = ScopeHelper.CreateSortedSetFromEnumerable(_interactiveParameters.ExtraScopesToConsent);
            }

            ValidateScopeInput(_extraScopesToConsent);

            _interactiveParameters.LogParameters(authenticationRequestParameters.RequestContext.Logger);
        }

        protected override void EnrichTelemetryApiEvent(ApiEvent apiEvent)
        {
            apiEvent.Prompt = _interactiveParameters.Prompt.PromptValue;
            if (_interactiveParameters.LoginHint != null)
            {
                apiEvent.LoginHint = _interactiveParameters.LoginHint;
            }
        }

        internal override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            await ResolveAuthorityEndpointsAsync().ConfigureAwait(false);
            await AcquireAuthorizationAsync(cancellationToken).ConfigureAwait(false);
            VerifyAuthorizationResult();

            if (AuthenticationRequestParameters.IsBrokerEnabled)
            {
                var brokerInteractiveRequest = new BrokerInteractiveRequest(
                    AuthenticationRequestParameters,
                    _interactiveParameters,
                    ServiceBundle,
                    _authorizationResult);

                if (brokerInteractiveRequest.IsBrokerInvocationRequired())
                {
                    _msalTokenResponse = await brokerInteractiveRequest.SendTokenRequestToBrokerAsync().ConfigureAwait(false);
                }
            }
            else
            {
                _msalTokenResponse = await SendTokenRequestAsync(GetBodyParameters(), cancellationToken).ConfigureAwait(false);
            }

            return await CacheTokenResponseAndCreateAuthenticationResultAsync(_msalTokenResponse).ConfigureAwait(false);
        }

        private async Task AcquireAuthorizationAsync(CancellationToken cancellationToken)
        {
            if (_webUi == null)
            {
                throw new ArgumentNullException("webUi");
            }

            AuthenticationRequestParameters.RedirectUri = _webUi.UpdateRedirectUri(AuthenticationRequestParameters.RedirectUri);
            var authorizationUri = CreateAuthorizationUri(true);

            var uiEvent = new UiEvent(AuthenticationRequestParameters.RequestContext.TelemetryCorrelationId);
            using (ServiceBundle.TelemetryManager.CreateTelemetryHelper(uiEvent))
            {
                _authorizationResult = await _webUi.AcquireAuthorizationAsync(
                                           authorizationUri,
                                           AuthenticationRequestParameters.RedirectUri,
                                           AuthenticationRequestParameters.RequestContext,
                                           cancellationToken).ConfigureAwait(false);
                uiEvent.UserCancelled = _authorizationResult.Status == AuthorizationStatus.UserCancel;
                uiEvent.AccessDenied = _authorizationResult.Status == AuthorizationStatus.ProtocolError;
            }
        }

        internal async Task<Uri> CreateAuthorizationUriAsync()
        {
            await ResolveAuthorityEndpointsAsync().ConfigureAwait(false);
            return CreateAuthorizationUri();
        }

        private Dictionary<string, string> GetBodyParameters()
        {
            var dict = new Dictionary<string, string>
            {
                [OAuth2Parameter.GrantType] = OAuth2GrantType.AuthorizationCode,
                [OAuth2Parameter.Code] = _authorizationResult.Code,
#if WINDOWS_APP
                [OAuth2Parameter.RedirectUri] = _redirectUriRequestParameter,
#else
                [OAuth2Parameter.RedirectUri] = AuthenticationRequestParameters.RedirectUri.OriginalString,
#endif //WINDOWS_APP
                [OAuth2Parameter.CodeVerifier] = _codeVerifier
            };

            return dict;
        }

        private Uri CreateAuthorizationUri(bool addPkceAndState = false)
        {
            IDictionary<string, string> requestParameters = CreateAuthorizationRequestParameters();

            if (addPkceAndState)
            {
                _codeVerifier = ServiceBundle.PlatformProxy.CryptographyManager.GenerateCodeVerifier();
                string codeVerifierHash = ServiceBundle.PlatformProxy.CryptographyManager.CreateBase64UrlEncodedSha256Hash(_codeVerifier);

                requestParameters[OAuth2Parameter.CodeChallenge] = codeVerifierHash;
                requestParameters[OAuth2Parameter.CodeChallengeMethod] = OAuth2Value.CodeChallengeMethodValue;

                _state = Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
                requestParameters[OAuth2Parameter.State] = _state;
            }

            // Add uid/utid values to QP if user object was passed in.
            if (_interactiveParameters.Account != null)
            {
                if (!string.IsNullOrEmpty(_interactiveParameters.Account.Username))
                {
                    requestParameters[OAuth2Parameter.LoginHint] = _interactiveParameters.Account.Username;
                }

                if (_interactiveParameters.Account?.HomeAccountId?.ObjectId != null)
                {
                    requestParameters[OAuth2Parameter.LoginReq] =
                        _interactiveParameters.Account.HomeAccountId.ObjectId;
                }

                if (!string.IsNullOrEmpty(_interactiveParameters.Account?.HomeAccountId?.TenantId))
                {
                    requestParameters[OAuth2Parameter.DomainReq] =
                        _interactiveParameters.Account.HomeAccountId.TenantId;
                }
            }

            CheckForDuplicateQueryParameters(AuthenticationRequestParameters.ExtraQueryParameters, requestParameters);

            string qp = requestParameters.ToQueryParameter();
            var builder = new UriBuilder(new Uri(AuthenticationRequestParameters.Endpoints.AuthorizationEndpoint));
            builder.AppendQueryParameters(qp);

            return builder.Uri;
        }

        private static void CheckForDuplicateQueryParameters(
            IDictionary<string, string> queryParamsDictionary,
            IDictionary<string, string> requestParameters)
        {
            foreach (KeyValuePair<string, string> kvp in queryParamsDictionary)
            {
                if (requestParameters.ContainsKey(kvp.Key))
                {
                    throw new MsalClientException(
                        MsalError.DuplicateQueryParameterError,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            MsalErrorMessage.DuplicateQueryParameterTemplate,
                            kvp.Key));
                }

                requestParameters[kvp.Key] = kvp.Value;
            }
        }

        private Dictionary<string, string> CreateAuthorizationRequestParameters(Uri redirectUriOverride = null)
        {
            SortedSet<string> unionScope = GetDecoratedScope(
                new SortedSet<string>(AuthenticationRequestParameters.Scope.Union(_extraScopesToConsent)));

            var authorizationRequestParameters = new Dictionary<string, string>
            {
                [OAuth2Parameter.Scope] = unionScope.AsSingleString(),
                [OAuth2Parameter.ResponseType] = OAuth2ResponseType.Code,

                [OAuth2Parameter.ClientId] = AuthenticationRequestParameters.ClientId,
#if WINDOWS_APP
                [OAuth2Parameter.RedirectUri] = _redirectUriRequestParameter
#else
                [OAuth2Parameter.RedirectUri] = redirectUriOverride?.OriginalString ?? AuthenticationRequestParameters.RedirectUri.OriginalString
#endif //WINDOWS_APP
            };

            if (!string.IsNullOrWhiteSpace(_interactiveParameters.LoginHint))
            {
                authorizationRequestParameters[OAuth2Parameter.LoginHint] = _interactiveParameters.LoginHint;
            }

            if (AuthenticationRequestParameters.RequestContext?.Logger?.CorrelationId != Guid.Empty)
            {
                authorizationRequestParameters[OAuth2Parameter.CorrelationId] =
                    AuthenticationRequestParameters.RequestContext.Logger.CorrelationId.ToString();
            }

            foreach (KeyValuePair<string, string> kvp in MsalIdHelper.GetMsalIdParameters(AuthenticationRequestParameters.RequestContext.Logger))
            {
                authorizationRequestParameters[kvp.Key] = kvp.Value;
            }

            if (_interactiveParameters.Prompt.PromptValue != Prompt.NoPrompt.PromptValue)
            {
                authorizationRequestParameters[OAuth2Parameter.Prompt] = _interactiveParameters.Prompt.PromptValue;
            }

            return authorizationRequestParameters;
        }

        private void VerifyAuthorizationResult()
        {
            if (_authorizationResult.Status == AuthorizationStatus.Success &&
                !_state.Equals(_authorizationResult.State,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new MsalClientException(
                    MsalError.StateMismatchError,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Returned state({0}) from authorize endpoint is not the same as the one sent({1})",
                        _authorizationResult.State,
                        _state));
            }

            if (_authorizationResult.Error == OAuth2Error.LoginRequired)
            {
                throw new MsalUiRequiredException(
                    MsalError.NoPromptFailedError,
                    MsalErrorMessage.NoPromptFailedErrorMessage);
            }

            if (_authorizationResult.Status != AuthorizationStatus.Success)
            {
                throw new MsalClientException(_authorizationResult.Error, _authorizationResult.ErrorDescription ?? "Unknown error.");
            }
        }
    }
}
