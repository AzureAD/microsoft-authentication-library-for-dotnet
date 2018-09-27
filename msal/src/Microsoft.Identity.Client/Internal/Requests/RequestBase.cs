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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.Identity.Core.Telemetry;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal abstract class RequestBase
    {
        static RequestBase()
        {
            PlatformPlugin.PlatformInformation = new PlatformInformation();
        }

        protected static readonly Task CompletedTask = Task.FromResult(false);
        internal AuthenticationRequestParameters AuthenticationRequestParameters { get; }
        internal TokenCache TokenCache { get; }
        protected MsalTokenResponse Response { get; set; }
        protected MsalAccessTokenCacheItem MsalAccessTokenItem { get; set; }
        protected MsalIdTokenCacheItem MsalIdTokenItem { get; set; }

        public ApiEvent.ApiIds ApiId { get; set; }
        public bool IsConfidentialClient { get; set; }
        protected virtual string GetUIBehaviorPromptValue()
        {
            return null;
        }

        protected bool SupportADFS { get; set; }

        protected bool LoadFromCache { get; set; }

        protected bool ForceRefresh { get; set; }

        protected bool StoreToCache { get; set; }

        protected RequestBase(AuthenticationRequestParameters authenticationRequestParameters)
        {
            TokenCache = authenticationRequestParameters.TokenCache;
            // Log contains Pii 
            authenticationRequestParameters.RequestContext.Logger.InfoPii(string.Format(CultureInfo.InvariantCulture,
                "=== Token Acquisition ({4}) started:\n\tAuthority: {0}\n\tScope: {1}\n\tClientId: {2}\n\tCache Provided: {3}",
                authenticationRequestParameters?.Authority?.CanonicalAuthority,
                authenticationRequestParameters.Scope.AsSingleString(),
                authenticationRequestParameters.ClientId,
                TokenCache != null, this.GetType().Name));

            // Log does not contain Pii
            var msg = string.Format(CultureInfo.InvariantCulture,
                "=== Token Acquisition ({1}) started:\n\tCache Provided: {0}", TokenCache != null, this.GetType().Name);

            if (authenticationRequestParameters.Authority != null &&
                AadAuthority.IsInTrustedHostList(authenticationRequestParameters.Authority.Host))
            {
                msg += string.Format(CultureInfo.CurrentCulture, "\n\tAuthority Host: {0}",
                    authenticationRequestParameters.Authority.Host);
            }
            authenticationRequestParameters.RequestContext.Logger.Info(msg);

            AuthenticationRequestParameters = authenticationRequestParameters;
            if (authenticationRequestParameters.Scope == null || authenticationRequestParameters.Scope.Count == 0)
            {
                throw new ArgumentNullException(nameof(authenticationRequestParameters.Scope));
            }

            ValidateScopeInput(authenticationRequestParameters.Scope);
            LoadFromCache = (TokenCache != null);
            StoreToCache = (TokenCache != null);
            SupportADFS = false;

            AuthenticationRequestParameters.LogState();
            Telemetry.GetInstance().ClientId = AuthenticationRequestParameters.ClientId;
        }

        protected virtual SortedSet<string> GetDecoratedScope(SortedSet<string> inputScope)
        {
            SortedSet<string> set = new SortedSet<string>(inputScope.ToArray());
            set.UnionWith(OAuth2Value.ReservedScopes.CreateSetFromEnumerable());
            return set;
        }

        protected void ValidateScopeInput(SortedSet<string> scopesToValidate)
        {
            //check if scope or additional scope contains client ID.
            if (scopesToValidate.Intersect(OAuth2Value.ReservedScopes.CreateSetFromEnumerable()).Any())
            {
                throw new ArgumentException("MSAL always sends the scopes 'openid profile offline_access'. " +
                                            "They cannot be suppressed as they are required for the " +
                                            "library to function. Do not include any of these scopes in the scope parameter.");
            }

            if (scopesToValidate.Contains(AuthenticationRequestParameters.ClientId))
            {
                throw new ArgumentException("API does not accept client id as a user-provided scope");
            }
        }

        public async Task<AuthenticationResult> RunAsync(CancellationToken cancellationToken)
        {
            //this method is the common entrance for all token requests, so it is a good place to put the generic Telemetry logic here
            AuthenticationRequestParameters.RequestContext.TelemetryRequestId = Telemetry.GetInstance().GenerateNewRequestId();
            string accountId = AuthenticationRequestParameters.Account?.HomeAccountId?.Identifier;
            var apiEvent = new ApiEvent()
            {
                ApiId = ApiId,
                ValidationStatus = AuthenticationRequestParameters.ValidateAuthority.ToString(),
                AccountId = accountId ?? "",
                CorrelationId = AuthenticationRequestParameters.RequestContext.Logger.CorrelationId.ToString(),
                RequestId = AuthenticationRequestParameters.RequestContext.TelemetryRequestId,
                IsConfidentialClient = IsConfidentialClient,
                UiBehavior = GetUIBehaviorPromptValue(),
                WasSuccessful = false
            };

            if (AuthenticationRequestParameters.Authority != null)
            {
                apiEvent.Authority = new Uri(AuthenticationRequestParameters.Authority.CanonicalAuthority);
                apiEvent.AuthorityType = AuthenticationRequestParameters.Authority.AuthorityType.ToString();
            }

            using (CoreTelemetryService.CreateTelemetryHelper(
                AuthenticationRequestParameters.RequestContext.TelemetryRequestId, 
                apiEvent, 
                shouldFlush: true))
            {
                try
                {
                    //authority endpoints resolution and validation
                    await PreTokenRequestAsync(cancellationToken).ConfigureAwait(false);
                    await SendTokenRequestAsync(cancellationToken).ConfigureAwait(false);
                    AuthenticationResult result = PostTokenRequest(cancellationToken);
                    await PostRunAsync(result).ConfigureAwait(false);

                    apiEvent.TenantId = result.TenantId;
                    apiEvent.AccountId = result.UniqueId;
                    apiEvent.WasSuccessful = true;
                    return result;
                }
                catch (MsalException ex)
                {
                    apiEvent.ApiErrorCode = ex.ErrorCode;
                    string noPiiMsg = MsalExceptionFactory.GetPiiScrubbedExceptionDetails(ex);
                    AuthenticationRequestParameters.RequestContext.Logger.Error(noPiiMsg);
                    AuthenticationRequestParameters.RequestContext.Logger.ErrorPii(ex);
                    throw;
                }
                catch (Exception ex)
                {
                    string noPiiMsg = MsalExceptionFactory.GetPiiScrubbedExceptionDetails(ex);
                    AuthenticationRequestParameters.RequestContext.Logger.Error(noPiiMsg);
                    AuthenticationRequestParameters.RequestContext.Logger.ErrorPii(ex);
                    throw;
                }
            }
        }

        private void SaveTokenResponseToCache()
        {
            // developer passed in user object.
            string msg = "checking client info returned from the server..";
            AuthenticationRequestParameters.RequestContext.Logger.Info(msg);
            AuthenticationRequestParameters.RequestContext.Logger.InfoPii(msg);

            ClientInfo fromServer = null;

            if (!AuthenticationRequestParameters.IsClientCredentialRequest)
            {
                //client_info is not returned from client credential flows because there is no user present.
                fromServer = ClientInfo.CreateFromJson(Response.ClientInfo);
            }

            if (fromServer!= null && AuthenticationRequestParameters.ClientInfo != null)
            {
                if (!fromServer.UniqueObjectIdentifier.Equals(AuthenticationRequestParameters.ClientInfo.UniqueObjectIdentifier, StringComparison.OrdinalIgnoreCase) ||
                    !fromServer.UniqueTenantIdentifier.Equals(AuthenticationRequestParameters.ClientInfo.UniqueTenantIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    AuthenticationRequestParameters.RequestContext.Logger.Error("Returned user identifiers do not match the sent user" +
                                                                                "identifier");

                    AuthenticationRequestParameters.RequestContext.Logger.ErrorPii(String.Format(
                        CultureInfo.InvariantCulture,
                        "Returned user identifiers (uid:{0} utid:{1}) does not meatch the sent user identifier (uid:{2} utid:{3})",
                        fromServer.UniqueObjectIdentifier, fromServer.UniqueTenantIdentifier,
                        AuthenticationRequestParameters.ClientInfo.UniqueObjectIdentifier,
                        AuthenticationRequestParameters.ClientInfo.UniqueTenantIdentifier));

                    throw new MsalServiceException("user_mismatch", "Returned user identifier does not match the sent user identifier");
                }
            }

            IdToken idToken = IdToken.Parse(Response.IdToken);

            AuthenticationRequestParameters.TenantUpdatedCanonicalAuthority = Authority.UpdateTenantId(
                AuthenticationRequestParameters.Authority.CanonicalAuthority, idToken?.TenantId);

            if (StoreToCache)
            {
                msg = "Saving Token Response to cache..";
                AuthenticationRequestParameters.RequestContext.Logger.Info(msg);
                AuthenticationRequestParameters.RequestContext.Logger.InfoPii(msg);

                var tuple = TokenCache.SaveAccessAndRefreshToken(AuthenticationRequestParameters, Response);
                MsalAccessTokenItem = tuple.Item1;
                MsalIdTokenItem = tuple.Item2;
            }
            else{
                MsalAccessTokenItem = new MsalAccessTokenCacheItem(AuthenticationRequestParameters.Authority.Host,
                    AuthenticationRequestParameters.ClientId, Response, idToken?.TenantId);

                MsalIdTokenItem = new MsalIdTokenCacheItem(AuthenticationRequestParameters.Authority.Host,
                    AuthenticationRequestParameters.ClientId, Response, idToken?.TenantId);
            }
        }

        protected virtual Task PostRunAsync(AuthenticationResult result)
        {
            LogReturnedToken(result);
            return CompletedTask;
        }

        internal virtual async Task PreTokenRequestAsync(CancellationToken cancellationToken)
        {
            await ResolveAuthorityEndpointsAsync().ConfigureAwait(false);
        }


        internal async Task ResolveAuthorityEndpointsAsync()
        {
            await AuthenticationRequestParameters.Authority.UpdateCanonicalAuthorityAsync
                (AuthenticationRequestParameters.RequestContext).ConfigureAwait(false);

            await AuthenticationRequestParameters.Authority
                .ResolveEndpointsAsync(AuthenticationRequestParameters.LoginHint,
                    AuthenticationRequestParameters.RequestContext)
                .ConfigureAwait(false);
        }


        protected virtual AuthenticationResult PostTokenRequest(CancellationToken cancellationToken)
        {
            //save to cache if no access token item found
            //this means that no cached item was found
            if (MsalAccessTokenItem == null)
            {
                SaveTokenResponseToCache();
            }

            return new AuthenticationResult(MsalAccessTokenItem, MsalIdTokenItem);
        }

        protected abstract void SetAdditionalRequestParameters(OAuth2Client client);

        protected virtual async Task SendTokenRequestAsync(CancellationToken cancellationToken)
        {
            OAuth2Client client = new OAuth2Client();
            client.AddBodyParameter(OAuth2Parameter.ClientId, AuthenticationRequestParameters.ClientId);
            client.AddBodyParameter(OAuth2Parameter.ClientInfo, "1");
            foreach (var entry in AuthenticationRequestParameters.ToParameters())
            {
                client.AddBodyParameter(entry.Key, entry.Value);
            }

            client.AddBodyParameter(OAuth2Parameter.Scope,
                GetDecoratedScope(AuthenticationRequestParameters.Scope).AsSingleString());
            SetAdditionalRequestParameters(client);
            await SendHttpMessageAsync(client).ConfigureAwait(false);
        }

        private async Task SendHttpMessageAsync(OAuth2Client client)
        {
            UriBuilder builder = new UriBuilder(AuthenticationRequestParameters.Authority.TokenEndpoint);
            builder.AppendQueryParameters(AuthenticationRequestParameters.SliceParameters);
            Response =
                await client
                    .GetTokenAsync(builder.Uri,
                        AuthenticationRequestParameters.RequestContext)
                    .ConfigureAwait(false);

            if (string.IsNullOrEmpty(Response.Scope))
            {
                Response.Scope = AuthenticationRequestParameters.Scope.AsSingleString();
                const string msg = "ScopeSet was missing from the token response, so using developer provided scopes in the result";
                AuthenticationRequestParameters.RequestContext.Logger.Info(msg);
                AuthenticationRequestParameters.RequestContext.Logger.InfoPii(msg);

            }
        }

        private void LogReturnedToken(AuthenticationResult result)
        {
            if (result.AccessToken != null)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, "=== Token Acquisition finished successfully. An access token was returned with Expiration Time: {0} ===",
                    result.ExpiresOn);
                AuthenticationRequestParameters.RequestContext.Logger.Info(msg);
                AuthenticationRequestParameters.RequestContext.Logger.InfoPii(msg);
            }
        }
    }
}