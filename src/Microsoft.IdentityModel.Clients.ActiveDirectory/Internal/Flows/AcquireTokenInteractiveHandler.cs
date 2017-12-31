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
using System.Threading.Tasks;
using Microsoft.Identity.Core.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Helpers;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Flows
{
    internal class AcquireTokenInteractiveHandler : AcquireTokenHandlerBase
    {
        internal AuthorizationResult authorizationResult;

        private readonly Uri redirectUri;

        private readonly string redirectUriRequestParameter;

        private readonly IPlatformParameters authorizationParameters;

        private readonly string extraQueryParameters;

        private readonly IWebUI webUi;

        private readonly UserIdentifier userId;

        private readonly string claims;

        public AcquireTokenInteractiveHandler(RequestData requestData, Uri redirectUri, IPlatformParameters parameters,
            UserIdentifier userId, string extraQueryParameters, IWebUI webUI, string claims)
            : base(requestData)
        {
            this.redirectUri = platformInformation.ValidateRedirectUri(redirectUri, RequestContext);

            if (!string.IsNullOrWhiteSpace(this.redirectUri.Fragment))
            {
                throw new ArgumentException(AdalErrorMessage.RedirectUriContainsFragment, "redirectUri");
            }

            this.authorizationParameters = parameters;

            this.redirectUriRequestParameter = platformInformation.GetRedirectUriAsString(this.redirectUri, RequestContext);

            if (userId == null)
            {
                throw new ArgumentNullException("userId", AdalErrorMessage.SpecifyAnyUser);
            }

            this.userId = userId;

            if (!string.IsNullOrEmpty(extraQueryParameters) && extraQueryParameters[0] == '&')
            {
                extraQueryParameters = extraQueryParameters.Substring(1);
            }

            this.extraQueryParameters = extraQueryParameters;
            this.webUi = webUI;
            this.UniqueId = userId.UniqueId;
            this.DisplayableId = userId.DisplayableId;
            this.UserIdentifierType = userId.Type;
            this.SupportADFS = true;

            if (!String.IsNullOrEmpty(claims))
            {
                this.LoadFromCache = false;

                var msg = "Claims present. Skip cache lookup.";
                RequestContext.Logger.Verbose(msg);
                RequestContext.Logger.VerbosePii(msg);

                this.claims = claims;
            }
            else
            {
                this.LoadFromCache = (requestData.TokenCache != null && parameters != null && platformInformation.GetCacheLoadPolicy(parameters));
            }

            this.brokerParameters[BrokerParameter.Force] = "NO";
            if (userId != UserIdentifier.AnyUser)
            {
                this.brokerParameters[BrokerParameter.Username] = userId.Id;
            }
            else
            {
                this.brokerParameters[BrokerParameter.Username] = string.Empty;
            }
            this.brokerParameters[BrokerParameter.UsernameType] = userId.Type.ToString();

            this.brokerParameters[BrokerParameter.RedirectUri] = this.redirectUri.AbsoluteUri;
            this.brokerParameters[BrokerParameter.ExtraQp] = extraQueryParameters;
            this.brokerParameters[BrokerParameter.Claims] = claims;
            brokerHelper.PlatformParameters = authorizationParameters;
        }

        private static string ReplaceHost(string original, string newHost)
        {
            return new UriBuilder(original) { Host = newHost }.Uri.ToString();
        }

        protected override async Task PreTokenRequest()
        {
            await base.PreTokenRequest().ConfigureAwait(false);

            // We do not have async interactive API in .NET, so we call this synchronous method instead.
            await this.AcquireAuthorizationAsync().ConfigureAwait(false);
            this.VerifyAuthorizationResult();

            if(!string.IsNullOrEmpty(authorizationResult.CloudInstanceHost))
            {
                var updatedAuthority = ReplaceHost(Authenticator.Authority, authorizationResult.CloudInstanceHost);

                await UpdateAuthority(updatedAuthority).ConfigureAwait(false);
            }
        }

        internal async Task AcquireAuthorizationAsync()
        {
            Uri authorizationUri = this.CreateAuthorizationUri();
            this.authorizationResult = await this.webUi.AcquireAuthorizationAsync(authorizationUri, this.redirectUri, RequestContext).ConfigureAwait(false);
        }

        internal async Task<Uri> CreateAuthorizationUriAsync(Guid correlationId)
        {
            this.RequestContext.CorrelationId = correlationId;
            await this.Authenticator.UpdateFromTemplateAsync(RequestContext).ConfigureAwait(false);
            return this.CreateAuthorizationUri();
        }
        protected override void AddAditionalRequestParameters(DictionaryRequestParameters requestParameters)
        {
            requestParameters[OAuthParameter.GrantType] = OAuthGrantType.AuthorizationCode;
            requestParameters[OAuthParameter.Code] = this.authorizationResult.Code;
            requestParameters[OAuthParameter.RedirectUri] = this.redirectUriRequestParameter;
        }

        protected override async Task PostTokenRequest(AdalResultWrapper resultEx)
        {
            await base.PostTokenRequest(resultEx).ConfigureAwait(false);
            if ((this.DisplayableId == null && this.UniqueId == null) || this.UserIdentifierType == UserIdentifierType.OptionalDisplayableId)
            {
                return;
            }

            string uniqueId = (resultEx.Result.UserInfo != null && resultEx.Result.UserInfo.UniqueId != null) ? resultEx.Result.UserInfo.UniqueId : "NULL";
            string displayableId = (resultEx.Result.UserInfo != null) ? resultEx.Result.UserInfo.DisplayableId : "NULL";

            if (this.UserIdentifierType == UserIdentifierType.UniqueId && string.Compare(uniqueId, this.UniqueId, StringComparison.Ordinal) != 0)
            {
                throw new AdalUserMismatchException(this.UniqueId, uniqueId);
            }

            if (this.UserIdentifierType == UserIdentifierType.RequiredDisplayableId && string.Compare(displayableId, this.DisplayableId, StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new AdalUserMismatchException(this.DisplayableId, displayableId);
            }
        }

        private Uri CreateAuthorizationUri()
        {
            string loginHint = null;

            if (!userId.IsAnyUser
                && (userId.Type == UserIdentifierType.OptionalDisplayableId
                    || userId.Type == UserIdentifierType.RequiredDisplayableId))
            {
                loginHint = userId.Id;
            }

            IRequestParameters requestParameters = this.CreateAuthorizationRequest(loginHint);

            return new Uri(new Uri(this.Authenticator.AuthorizationUri), "?" + requestParameters);
        }

        private DictionaryRequestParameters CreateAuthorizationRequest(string loginHint)
        {
            var authorizationRequestParameters = new DictionaryRequestParameters(this.Resource, this.ClientKey);
            authorizationRequestParameters[OAuthParameter.ResponseType] = OAuthResponseType.Code;
            authorizationRequestParameters[OAuthParameter.HasChrome] = "1";
            authorizationRequestParameters[OAuthParameter.RedirectUri] = this.redirectUriRequestParameter;

            if (!string.IsNullOrWhiteSpace(loginHint))
            {
                authorizationRequestParameters[OAuthParameter.LoginHint] = loginHint;
            }

            if (!string.IsNullOrWhiteSpace(claims))
            {
                authorizationRequestParameters["claims"] = claims;
            }

            if (this.RequestContext != null && this.RequestContext.CorrelationId != Guid.Empty)
            {
                authorizationRequestParameters[OAuthParameter.CorrelationId] = this.RequestContext.CorrelationId.ToString();
            }

            if (this.authorizationParameters != null)
            {
                platformInformation.AddPromptBehaviorQueryParameter(this.authorizationParameters, authorizationRequestParameters);
            }
            
                IDictionary<string, string> adalIdParameters = AdalIdHelper.GetAdalIdParameters();
                foreach (KeyValuePair<string, string> kvp in adalIdParameters)
                {
                    authorizationRequestParameters[kvp.Key] = kvp.Value;
                }
            

            if (!string.IsNullOrWhiteSpace(extraQueryParameters))
            {
                // Checks for extraQueryParameters duplicating standard parameters
                Dictionary<string, string> kvps = EncodingHelper.ParseKeyValueList(extraQueryParameters, '&', false, RequestContext);
                foreach (KeyValuePair<string, string> kvp in kvps)
                {
                    if (authorizationRequestParameters.ContainsKey(kvp.Key))
                    {
                        throw new AdalException(AdalError.DuplicateQueryParameter, string.Format(CultureInfo.CurrentCulture, AdalErrorMessage.DuplicateQueryParameterTemplate, kvp.Key));
                    }
                }

                authorizationRequestParameters.ExtraQueryParameter = extraQueryParameters;
            }

            return authorizationRequestParameters;
        }

        private void VerifyAuthorizationResult()
        {
            if (this.authorizationResult.Error == OAuthError.LoginRequired)
            {
                throw new AdalException(AdalError.UserInteractionRequired);
            }

            if (this.authorizationResult.Status != AuthorizationStatus.Success)
            {
                throw new AdalServiceException(this.authorizationResult.Error, this.authorizationResult.ErrorDescription);
            }
        }

        protected override void UpdateBrokerParameters(IDictionary<string, string> parameters)
        {
            Uri uri = new Uri(this.authorizationResult.Code);
            string query = EncodingHelper.UrlDecode(uri.Query);
            Dictionary<string, string> kvps = EncodingHelper.ParseKeyValueList(query, '&', false, RequestContext);
            parameters["username"] = kvps["username"];
        }

        protected override bool BrokerInvocationRequired()
        {
            if (this.authorizationResult != null
                && !string.IsNullOrEmpty(this.authorizationResult.Code)
                && this.authorizationResult.Code.StartsWith("msauth://", StringComparison.OrdinalIgnoreCase))
            {
                this.brokerParameters[BrokerParameter.BrokerInstallUrl] = this.authorizationResult.Code;
                return true;
            }

            return false;
        }
    }
}