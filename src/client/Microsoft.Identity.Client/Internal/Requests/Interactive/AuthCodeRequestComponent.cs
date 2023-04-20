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
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal
{
    /// <summary>
    /// Responsible for getting an auth code
    /// </summary>
    internal class AuthCodeRequestComponent : IAuthCodeRequestComponent
    {
        private readonly AuthenticationRequestParameters _requestParams;
        private readonly AcquireTokenInteractiveParameters _interactiveParameters;
        private readonly IServiceBundle _serviceBundle;

        public AuthCodeRequestComponent(
            AuthenticationRequestParameters requestParams,
            AcquireTokenInteractiveParameters interactiveParameters)
        {
            _requestParams = requestParams ?? throw new ArgumentNullException(nameof(requestParams));
            _interactiveParameters = interactiveParameters ?? throw new ArgumentNullException(nameof(requestParams));
            _serviceBundle = _requestParams.RequestContext.ServiceBundle;
        }

        public async Task<Tuple<AuthorizationResult, string>> FetchAuthCodeAndPkceVerifierAsync(
            CancellationToken cancellationToken)
        {
            var webUi = CreateWebAuthenticationDialog();
            return await FetchAuthCodeAndPkceInternalAsync(webUi, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Uri> GetAuthorizationUriWithoutPkceAsync(CancellationToken cancellationToken)
        {
            string authEndpoint = await _requestParams.Authority.GetAuthorizationEndpointAsync(
                _requestParams.RequestContext).ConfigureAwait(false);

            var result = CreateAuthorizationUri(authEndpoint, false);
            return result.Item1;
        }

        public async Task<Uri> GetAuthorizationUriWithPkceAsync(string codeVerifier, CancellationToken cancellationToken)
        {
            string authEndpoint = await _requestParams.Authority.GetAuthorizationEndpointAsync(_requestParams.RequestContext)
                .ConfigureAwait(false);

            var result = CreateAuthorizationUriWithCodeChallenge(authEndpoint, codeVerifier);
            return result.Item1;
        }

        private async Task<Tuple<AuthorizationResult, string>> FetchAuthCodeAndPkceInternalAsync(
            IWebUI webUi,
            CancellationToken cancellationToken)
        {
            RedirectUriHelper.Validate(_requestParams.RedirectUri);

            _requestParams.RedirectUri = webUi.UpdateRedirectUri(_requestParams.RedirectUri);

            string authEndpoint = await _requestParams.Authority.GetAuthorizationEndpointAsync(_requestParams.RequestContext)
                .ConfigureAwait(false);

            Tuple<Uri, string, string> authorizationTuple = CreateAuthorizationUri(authEndpoint, true);
            Uri authorizationUri = authorizationTuple.Item1;
            string state = authorizationTuple.Item2;
            string codeVerifier = authorizationTuple.Item3;

            var authorizationResult = await webUi.AcquireAuthorizationAsync(
                                       authorizationUri,
                                       _requestParams.RedirectUri,
                                       _requestParams.RequestContext,
                                       cancellationToken).ConfigureAwait(false);

            VerifyAuthorizationResult(authorizationResult, state);

            return new Tuple<AuthorizationResult, string>(authorizationResult, codeVerifier);

        }

        private Tuple<Uri, string> CreateAuthorizationUriWithCodeChallenge(
            string authEndpoint, string codeVerifier)
        {
            IDictionary<string, string> requestParameters = CreateAuthorizationRequestParameters();

            string codeChallenge = _serviceBundle.PlatformProxy.CryptographyManager.CreateBase64UrlEncodedSha256Hash(codeVerifier);
            requestParameters[OAuth2Parameter.CodeChallenge] = codeChallenge;
            requestParameters[OAuth2Parameter.CodeChallengeMethod] = OAuth2Value.CodeChallengeMethodValue;

            UriBuilder builder = CreateInteractiveRequestParameters(authEndpoint, requestParameters);

            return new Tuple<Uri, string>(builder.Uri, codeVerifier);
        }

        private Tuple<Uri, string, string> CreateAuthorizationUri(string authEndpoint, bool addPkceAndState = false)
        {
            IDictionary<string, string> requestParameters = CreateAuthorizationRequestParameters();
            string codeVerifier = null;
            string state = null;

            if (addPkceAndState)
            {
                codeVerifier = _serviceBundle.PlatformProxy.CryptographyManager.GenerateCodeVerifier();
                string codeChallenge = _serviceBundle.PlatformProxy.CryptographyManager.CreateBase64UrlEncodedSha256Hash(codeVerifier);

                requestParameters[OAuth2Parameter.CodeChallenge] = codeChallenge;
                requestParameters[OAuth2Parameter.CodeChallengeMethod] = OAuth2Value.CodeChallengeMethodValue;

                state = Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
                requestParameters[OAuth2Parameter.State] = state;
            }

            requestParameters[OAuth2Parameter.ClientInfo] = "1";
            UriBuilder builder = CreateInteractiveRequestParameters(authEndpoint, requestParameters);

            return new Tuple<Uri, string, string>(builder.Uri, state, codeVerifier);
        }

        private UriBuilder CreateInteractiveRequestParameters(string authEndpoint, IDictionary<string, string> requestParameters)
        {
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

            CheckForDuplicateQueryParameters(_requestParams.ExtraQueryParameters, requestParameters);

            string qp = requestParameters.ToQueryParameter();
            var builder = new UriBuilder(authEndpoint);
            builder.AppendQueryParameters(qp);
            return builder;
        }

        private Dictionary<string, string> CreateAuthorizationRequestParameters(Uri redirectUriOverride = null)
        {
            var extraScopesToConsent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!_interactiveParameters.ExtraScopesToConsent.IsNullOrEmpty())
            {
                extraScopesToConsent = ScopeHelper.CreateScopeSet(_interactiveParameters.ExtraScopesToConsent);
            }

            if (extraScopesToConsent.Contains(_requestParams.AppConfig.ClientId))
            {
                throw new ArgumentException("API does not accept client id as a user-provided scope");
            }

            var unionScope = ScopeHelper.GetMsalScopes(
                new HashSet<string>(_requestParams.Scope.Concat(extraScopesToConsent)));

            var authorizationRequestParameters = new Dictionary<string, string>
            {
                [OAuth2Parameter.Scope] = unionScope.AsSingleString(),
                [OAuth2Parameter.ResponseType] = OAuth2ResponseType.Code,

                [OAuth2Parameter.ClientId] = _requestParams.AppConfig.ClientId,
                [OAuth2Parameter.RedirectUri] = redirectUriOverride?.OriginalString ?? _requestParams.RedirectUri.OriginalString
            };

            if (!string.IsNullOrWhiteSpace(_requestParams.ClaimsAndClientCapabilities))
            {
                authorizationRequestParameters[OAuth2Parameter.Claims] = _requestParams.ClaimsAndClientCapabilities;
            }

            //CcsRoutingHint passed in from WithCcsRoutingHint() will override the AAD backup authentication system Hint created from the login hint
            if (!string.IsNullOrWhiteSpace(_interactiveParameters.LoginHint) || _requestParams.CcsRoutingHint != null)
            {
                string OidCcsHeader;
                if (_requestParams.CcsRoutingHint == null)
                {
                    authorizationRequestParameters[OAuth2Parameter.LoginHint] = _interactiveParameters.LoginHint;
                    OidCcsHeader = CoreHelpers.GetCcsUpnHint(_interactiveParameters.LoginHint);
                }
                else
                {
                    authorizationRequestParameters[OAuth2Parameter.LoginHint] = _interactiveParameters.LoginHint;
                    OidCcsHeader = CoreHelpers.GetCcsClientInfoHint(_requestParams.CcsRoutingHint.Value.Key, _requestParams.CcsRoutingHint.Value.Value);
                }

                //The AAD backup authentication system header is used by the AAD backup authentication system service
                //to help route requests to resources in Azure during requests to speed up authentication.
                //It consists of either the ObjectId.TenantId or the upn of the account signing in.
                //See https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2525
                authorizationRequestParameters[Constants.CcsRoutingHintHeader] = OidCcsHeader;
            }

            if (_requestParams.RequestContext.CorrelationId != Guid.Empty)
            {
                authorizationRequestParameters[OAuth2Parameter.CorrelationId] =
                    _requestParams.RequestContext.CorrelationId.ToString();
            }

            foreach (KeyValuePair<string, string> kvp in MsalIdHelper.GetMsalIdParameters(_requestParams.RequestContext.Logger))
            {
                authorizationRequestParameters[kvp.Key] = kvp.Value;
            }

            if (_interactiveParameters.Prompt == Prompt.NotSpecified)
            {
                authorizationRequestParameters[OAuth2Parameter.Prompt] = Prompt.SelectAccount.PromptValue;
            }
            else if (_interactiveParameters.Prompt.PromptValue != Prompt.NoPrompt.PromptValue)
            {
                authorizationRequestParameters[OAuth2Parameter.Prompt] = _interactiveParameters.Prompt.PromptValue;
            }

            return authorizationRequestParameters;
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

        private void VerifyAuthorizationResult(AuthorizationResult authorizationResult, string originalState)
        {
            if (authorizationResult.Status == AuthorizationStatus.Success &&
                originalState != null &&
                !originalState.Equals(authorizationResult.State,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new MsalClientException(
                    MsalError.StateMismatchError,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        MsalErrorMessage.StateMismatchErrorMessage,
                        authorizationResult.State,
                        originalState));
            }

            if (authorizationResult.Error == OAuth2Error.LoginRequired)
            {
                throw new MsalUiRequiredException(
                    MsalError.NoPromptFailedError,
                    MsalErrorMessage.NoPromptFailedErrorMessage,
                    null,
                    UiRequiredExceptionClassification.PromptNeverFailed);
            }

            if (authorizationResult.Status == AuthorizationStatus.UserCancel)
            {
                _requestParams.RequestContext.Logger.Info(LogMessages.UserCancelledAuthentication);
                throw new MsalClientException(
                    authorizationResult.Error,
                    authorizationResult.ErrorDescription ?? "User canceled authentication.");
            }

            if (authorizationResult.Status != AuthorizationStatus.Success)
            {
                _requestParams.RequestContext.Logger.ErrorPii(
                    LogMessages.AuthorizationResultWasNotSuccessful + authorizationResult.ErrorDescription ?? "Unknown error.",
                    LogMessages.AuthorizationResultWasNotSuccessful);

                throw new MsalServiceException(
                    authorizationResult.Error,
                    !string.IsNullOrEmpty(authorizationResult.ErrorDescription) ? authorizationResult.ErrorDescription : "Unknown error");
            }
        }

        private IWebUI CreateWebAuthenticationDialog()
        {
            if (_interactiveParameters.CustomWebUi != null)
            {
                return new CustomWebUiHandler(_interactiveParameters.CustomWebUi);
            }

            CoreUIParent coreUiParent = _interactiveParameters.UiParent;

#if WINDOWS_APP || DESKTOP
            // hidden web view can be used in both WinRT and desktop applications.
            coreUiParent.UseHiddenBrowser = _interactiveParameters.Prompt.Equals(Prompt.Never);
#if WINDOWS_APP
            coreUiParent.UseCorporateNetwork = _serviceBundle.Config.UseCorporateNetwork;
#endif
#endif            
            return _serviceBundle.PlatformProxy.GetWebUiFactory(_requestParams.AppConfig)
                .CreateAuthenticationDialog(
                coreUiParent,
                _interactiveParameters.UseEmbeddedWebView,
                _requestParams.RequestContext);
        }
    }
}
