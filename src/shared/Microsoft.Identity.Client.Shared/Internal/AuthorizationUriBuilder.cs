using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.PublicClient.Internal.Requests
{
    internal class AuthorizationUriBuilder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns>(uri, state, pkce code verifier)</returns>
        public static Tuple<Uri, string, string> CreateAuthorizationUri(
            GetAuthorizationRequestUrlParameters getAuthUriParams,
            AuthenticationRequestParameters requestParameters,
            bool addPkceAndState)
        {
            IDictionary<string, string> requestParamsMap = 
                CreateAuthorizationRequestParameters(getAuthUriParams, requestParameters);

            var crypto = requestParameters.RequestContext.ServiceBundle.PlatformProxy.CryptographyManager;

            string codeVerifier = null;
            string state = null;

            if (addPkceAndState)
            {
                codeVerifier = crypto.GenerateCodeVerifier();
                string codeVerifierHash = crypto.CreateBase64UrlEncodedSha256Hash(codeVerifier);

                requestParamsMap[OAuth2Parameter.CodeChallenge] = codeVerifierHash;
                requestParamsMap[OAuth2Parameter.CodeChallengeMethod] = OAuth2Value.CodeChallengeMethodValue;

                state = Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
                requestParamsMap[OAuth2Parameter.State] = state;
            }

            // Add uid/utid values to QP if user object was passed in.
            if (getAuthUriParams.Account != null)
            {
                if (!string.IsNullOrEmpty(getAuthUriParams.Account.Username))
                {
                    requestParamsMap[OAuth2Parameter.LoginHint] = getAuthUriParams.Account.Username;
                }

                if (getAuthUriParams.Account?.HomeAccountId?.ObjectId != null)
                {
                    requestParamsMap[OAuth2Parameter.LoginReq] =
                        getAuthUriParams.Account.HomeAccountId.ObjectId;
                }

                if (!string.IsNullOrEmpty(getAuthUriParams.Account?.HomeAccountId?.TenantId))
                {
                    requestParamsMap[OAuth2Parameter.DomainReq] =
                        getAuthUriParams.Account.HomeAccountId.TenantId;
                }
            }

            CheckForDuplicateQueryParameters(requestParameters.ExtraQueryParameters, requestParamsMap);

            string qp = requestParamsMap.ToQueryParameter();
            var builder = new UriBuilder(new Uri(requestParameters.Endpoints.AuthorizationEndpoint));
            builder.AppendQueryParameters(qp);

            return new Tuple<Uri, string, string>(builder.Uri, state, codeVerifier);
        }

        private static Dictionary<string, string> CreateAuthorizationRequestParameters(
            GetAuthorizationRequestUrlParameters getAuthParams,
            AuthenticationRequestParameters requestParams)
        {
            var extraScopesToConsent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!getAuthParams.ExtraScopesToConsent.IsNullOrEmpty())
            {
                extraScopesToConsent = ScopeHelper.CreateScopeSet(getAuthParams.ExtraScopesToConsent);
            }

            if (extraScopesToConsent.Contains(requestParams.ClientId))
            {
                throw new ArgumentException("API does not accept client id as a user-provided scope");
            }

            var unionScope = ScopeHelper.GetMsalScopes(
                new HashSet<string>(requestParams.Scope.Concat(extraScopesToConsent)));

            var authorizationRequestParameters = new Dictionary<string, string>
            {
                [OAuth2Parameter.Scope] = unionScope.AsSingleString(),
                [OAuth2Parameter.ResponseType] = OAuth2ResponseType.Code,

                [OAuth2Parameter.ClientId] = requestParams.ClientId,
                [OAuth2Parameter.RedirectUri] = requestParams.RedirectUri.OriginalString
            };

            if (!string.IsNullOrWhiteSpace(requestParams.ClaimsAndClientCapabilities))
            {
                authorizationRequestParameters[OAuth2Parameter.Claims] = requestParams.ClaimsAndClientCapabilities;
            }

            if (!string.IsNullOrWhiteSpace(getAuthParams.LoginHint))
            {
                authorizationRequestParameters[OAuth2Parameter.LoginHint] = getAuthParams.LoginHint;
            }

            if (requestParams.RequestContext.CorrelationId != Guid.Empty)
            {
                authorizationRequestParameters[OAuth2Parameter.CorrelationId] =
                    requestParams.RequestContext.CorrelationId.ToString();
            }

            foreach (KeyValuePair<string, string> kvp in MsalIdHelper.GetMsalIdParameters(
                requestParams.RequestContext.ServiceBundle.PlatformProxy))
            {
                authorizationRequestParameters[kvp.Key] = kvp.Value;
            }

            if (getAuthParams.Prompt == Prompt.NotSpecified.PromptValue || string.IsNullOrEmpty(getAuthParams.Prompt))
            {
                authorizationRequestParameters[OAuth2Parameter.Prompt] = Prompt.SelectAccount.PromptValue;
            }
            else if (getAuthParams.Prompt != Prompt.NoPrompt.PromptValue)
            {
                authorizationRequestParameters[OAuth2Parameter.Prompt] = getAuthParams.Prompt;
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
