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
    internal class AuthCodeRequestComponentBase : IAuthCodeRequestComponent
    {
        protected readonly AuthenticationRequestParameters _requestParams;
        private readonly GetAuthorizationRequestUrlParameters _getAuthUrlParameters;
        protected readonly IServiceBundle _serviceBundle;

        public AuthCodeRequestComponentBase(
            AuthenticationRequestParameters requestParams,
            GetAuthorizationRequestUrlParameters getAuthUrlParameters)
        {
            _requestParams = requestParams ?? throw new ArgumentNullException(nameof(requestParams));
            _getAuthUrlParameters = getAuthUrlParameters ?? throw new ArgumentNullException(nameof(getAuthUrlParameters));
            _serviceBundle = _requestParams.RequestContext.ServiceBundle;
        }

        public async Task<Tuple<AuthorizationResult, string>> FetchAuthCodeAndPkceVerifierAsync(
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Uri GetAuthorizationUriWithoutPkce()
        {
            var result = CreateAuthorizationUri(false);
            return result.Item1;
        }

        public Uri GetAuthorizationUriWithPkce(string codeVerifier)
        {
            var result = CreateAuthorizationUriWithCodeChallenge(codeVerifier);
            return result.Item1;
        }

        private Tuple<Uri, string> CreateAuthorizationUriWithCodeChallenge(
            string codeVerifier)
        {
            IDictionary<string, string> requestParameters = CreateAuthorizationRequestParameters();

            string codeChallenge = _serviceBundle.PlatformProxy.CryptographyManager.CreateBase64UrlEncodedSha256Hash(codeVerifier);
            requestParameters[OAuth2Parameter.CodeChallenge] = codeChallenge;
            requestParameters[OAuth2Parameter.CodeChallengeMethod] = OAuth2Value.CodeChallengeMethodValue;

            UriBuilder builder = CreateInteractiveRequestParameters(requestParameters);

            return new Tuple<Uri, string>(builder.Uri, codeVerifier);
        }

        protected Tuple<Uri, string, string> CreateAuthorizationUri(bool addPkceAndState = false)
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
            UriBuilder builder = CreateInteractiveRequestParameters(requestParameters);

            return new Tuple<Uri, string, string>(builder.Uri, state, codeVerifier);
        }

        private UriBuilder CreateInteractiveRequestParameters(IDictionary<string, string> requestParameters)
        {
            // Add uid/utid values to QP if user object was passed in.
            if (_getAuthUrlParameters.Account != null)
            {
                if (!string.IsNullOrEmpty(_getAuthUrlParameters.Account.Username))
                {
                    requestParameters[OAuth2Parameter.LoginHint] = _getAuthUrlParameters.Account.Username;
                }

                if (_getAuthUrlParameters.Account?.HomeAccountId?.ObjectId != null)
                {
                    requestParameters[OAuth2Parameter.LoginReq] =
                        _getAuthUrlParameters.Account.HomeAccountId.ObjectId;
                }

                if (!string.IsNullOrEmpty(_getAuthUrlParameters.Account?.HomeAccountId?.TenantId))
                {
                    requestParameters[OAuth2Parameter.DomainReq] =
                        _getAuthUrlParameters.Account.HomeAccountId.TenantId;
                }
            }

            CheckForDuplicateQueryParameters(_requestParams.ExtraQueryParameters, requestParameters);

            string qp = requestParameters.ToQueryParameter();
            var builder = new UriBuilder(new Uri(_requestParams.Authority.GetAuthorizationEndpoint()));
            builder.AppendQueryParameters(qp);
            return builder;
        }

        private Dictionary<string, string> CreateAuthorizationRequestParameters(Uri redirectUriOverride = null)
        {
            var extraScopesToConsent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!_getAuthUrlParameters.ExtraScopesToConsent.IsNullOrEmpty())
            {
                extraScopesToConsent = ScopeHelper.CreateScopeSet(_getAuthUrlParameters.ExtraScopesToConsent);
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
            if (!string.IsNullOrWhiteSpace(_getAuthUrlParameters.LoginHint) || _requestParams.CcsRoutingHint != null)
            {
                string OidCcsHeader;
                if (_requestParams.CcsRoutingHint == null)
                {
                    authorizationRequestParameters[OAuth2Parameter.LoginHint] = _getAuthUrlParameters.LoginHint;
                    OidCcsHeader = CoreHelpers.GetCcsUpnHint(_getAuthUrlParameters.LoginHint);
                }
                else
                {
                    authorizationRequestParameters[OAuth2Parameter.LoginHint] = _getAuthUrlParameters.LoginHint;
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

            if (_getAuthUrlParameters.Prompt == Prompt.NotSpecified)
            {
                authorizationRequestParameters[OAuth2Parameter.Prompt] = Prompt.SelectAccount.PromptValue;
            }
            else if (_getAuthUrlParameters.Prompt.PromptValue != Prompt.NoPrompt.PromptValue)
            {
                authorizationRequestParameters[OAuth2Parameter.Prompt] = _getAuthUrlParameters.Prompt.PromptValue;
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
    }
}
