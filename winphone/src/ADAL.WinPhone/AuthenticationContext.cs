//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Security.Authentication.Web;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Optional delegate that can be defined by the developer to process the result of acquire token requests.
    /// </summary>
    /// <param name="result">Authentication result instance received from acquire token call</param>
    public delegate void AuthenticationContextDelegate(AuthenticationResult result);

    /// <summary>
    /// The main class representing the authority issuing tokens for resources.
    /// </summary>
    public sealed partial class AuthenticationContext
    {
        private static readonly IDictionary<TokenCacheKey, string> StaticTokenCacheStore = new DefaultTokenCacheStore();
        //private string EventKey = string.Empty;
        
        /// <summary>
        /// Gets or Sets flag to enable logged in user authentication. Note that enabling this flag requires some extra application capabilites.
        /// </summary>
        public bool UseCorporateNetwork { get; set; }

        private AuthenticationContextDelegate instance;

        private AuthenticationContext()
        {
        }

        /// <summary>
        /// Constructor to create the context with the address of the authority.
        /// Using this constructor will turn ON validation of the authority URL by default if validation is supported for the authority address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <returns> IAsyncOperation representing the instance of the class.</returns>
        public static IAsyncOperation<AuthenticationContext> CreateAsync(string authority)
        {
            var ret = new AuthenticationContext(authority);
            return RunTaskAsAsyncOperation(ret.Initialize());
        }

        /// <summary>
        /// Factory method to create the context with the address of the authority and flag to turn address validation off.
        /// Using this constructor, address validation can be turned off. Make sure you are aware of the security implication of not validating the address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <param name="validateAuthority">Flag to turn address validation ON or OFF.</param>
        /// <returns> IAsyncOperation representing the instance of the class.</returns>
        [DefaultOverload]
        public static IAsyncOperation<AuthenticationContext> CreateAsync(string authority, bool validateAuthority)
        {
            var ret = new AuthenticationContext(authority, validateAuthority);
            return RunTaskAsAsyncOperation(ret.Initialize());
        }

        /// <summary>
        /// Factory method to create the context with the address of the authority and flag to turn address validation off.
        /// Using this constructor, address validation can be turned off. Make sure you are aware of the security implication of not validating the address.
        /// </summary>
        /// <param name="authority">Address of the authority to issue token.</param>
        /// <param name="validateAuthority">Flag to turn address validation ON or OFF.</param>
        /// <param name="tokenCacheStore">User provided cache implementation instance.</param>
        /// <returns> IAsyncOperation representing the instance of the class.</returns>
        public static IAsyncOperation<AuthenticationContext> CreateAsync(string authority, bool validateAuthority,
            IDictionary<TokenCacheKey, string> tokenCacheStore)
        {
            var ret = new AuthenticationContext(authority, validateAuthority ? AuthorityValidationType.True : AuthorityValidationType.False, tokenCacheStore);
            return RunTaskAsAsyncOperation(ret.Initialize());
        }

        private async Task<AuthenticationContext> Initialize()
        {
            await this.CreateAuthenticatorAsync(this.CreateCallState());
            return this;
        }

        /// <summary>
        /// Acquires security token from the cache ONLY and if the expiration is within threshold window, refresh it.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userId">User name Identifier. This parameter can be null.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public IAsyncOperation<AuthenticationResult> AcquireTokenSilentlyAsync(string resource, string clientId, string userId)
        {
            return RunTaskAsAsyncOperation(AcquireTokenSilentCommonAsync(resource, clientId, userId));
        }

        /// <summary>
        /// Acquires security token from the cache ONLY and if the expiration is within threshold window, refresh it.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>\
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public IAsyncOperation<AuthenticationResult> AcquireTokenSilentlyAsync(string resource, string clientId)
        {
            return this.AcquireTokenSilentlyAsync(resource, clientId, null);
        }

        /// <summary>
        /// Acquires security token from the authority in SSO mode.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        /// <param name="authDelegate">Optional delegate that can be passed by the developer to process authentication result.</param>
        [DefaultOverload]
        public void AcquireTokenAndContinue(string resource, string clientId, AuthenticationContextDelegate authDelegate)
        {
            this.AcquireTokenAndContinueCommon(resource, clientId, WebAuthenticationBroker.GetCurrentApplicationCallbackUri());
            this.instance = authDelegate;
        }


        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="authDelegate">Optional delegate that can be passed by the developer to process authentication result.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        [DefaultOverload]
        public void AcquireTokenAndContinue(string resource, string clientId, Uri redirectUri, AuthenticationContextDelegate authDelegate)
        {
            this.AcquireTokenAndContinueCommon(resource, clientId, GetRedirectUri(redirectUri));
            this.instance = authDelegate;
        }


        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="userId">This parameter will be used to pre-populate the username field in the authentication form. This parameter can be null.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="authDelegate">Optional delegate that can be passed by the developer to process authentication result.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        [DefaultOverload]
        public void AcquireTokenAndContinue(string resource, string clientId, Uri redirectUri, string userId, string extraQueryParameters, AuthenticationContextDelegate authDelegate)
        {
            this.AcquireTokenAndContinueCommon(resource, clientId, GetRedirectUri(redirectUri), userId, extraQueryParameters);
            this.instance = authDelegate;
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="authDelegate">Optional delegate that can be passed by the developer to process authentication result.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        [DefaultOverload]
        public void AcquireTokenAndContinue(string resource, string clientId, Uri redirectUri, string extraQueryParameters, AuthenticationContextDelegate authDelegate)
        {
            this.AcquireTokenAndContinueCommon(resource, clientId, GetRedirectUri(redirectUri), null, extraQueryParameters);
            this.instance = authDelegate;
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userId">This parameter will be used to pre-populate the username field in the authentication form. Please note that the end user can still edit the username field and authenticate as a different user. This parameter can be null.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="authDelegate">Optional delegate that can be passed by the developer to process authentication result.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public void AcquireTokenAndContinue(string resource, string clientId, string userId, string extraQueryParameters, AuthenticationContextDelegate authDelegate)
        {
            this.AcquireTokenAndContinueCommon(resource, clientId, WebAuthenticationBroker.GetCurrentApplicationCallbackUri(), userId, extraQueryParameters);
            this.instance = authDelegate;
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public IAsyncOperation<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId)
        {
            return RunTaskAsAsyncOperation(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, null));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. If null, token is requested for the same resource refresh token was originally issued for.
        /// If passed, resource should match the original resource used to acquire refresh token unless token service supports refresh token for multiple resources.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public IAsyncOperation<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, string resource)
        {
            return RunTaskAsAsyncOperation(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, resource));
        }

        private IWebUI CreateWebAuthenticationDialog()
        {
            return NetworkPlugin.WebUIFactory.Create(this.UseCorporateNetwork);
        }

        private void AcquireAuthorization(string resource, string clientId, Uri redirectUri, string userId, string extraQueryParameters, CallState callState)
        {
            OAuth2Request.SendAuthorizeRequest(this.Authenticator, resource, redirectUri, clientId, userId, extraQueryParameters, this.CreateWebAuthenticationDialog(), callState);
        
        }

        private static IAsyncAction RunTaskAsAsyncAction(Task task)
        {
            return Task.Run(() => task).AsAsyncAction();
        }

        private static IAsyncOperation<AuthenticationResult> RunTaskAsAsyncOperation(Task<AuthenticationResult> task)
        {
            return RunTask(task).AsAsyncOperation();
        }

        private static IAsyncOperation<AuthenticationContext> RunTaskAsAsyncOperation(Task<AuthenticationContext> task)
        {
            return task.AsAsyncOperation();
        }

        private static async Task<AuthenticationResult> RunTask(Task<AuthenticationResult> task)
        {
            AuthenticationResult result;
            try
            {
                result = await task;
            }
            catch (Exception ex)
            {
                result = new AuthenticationResult(ex);
            }

            return result;
        }

        private static Uri GetRedirectUri(Uri redirectUri)
        {
            return redirectUri ?? WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public IAsyncOperation<AuthenticationResult> ContinueAcquireToken(
            IWebAuthenticationBrokerContinuationEventArgs args)
        {
            CallState callState = new CallState(new Guid((string)args.ContinuationData["correlation_id"]));
            WebAuthenticationResult wabResult = args.WebAuthenticationResult;
            AdalWebAuthenticationResult adalWabResult = NetworkPlugin.AdalWabResultHandler.Create(wabResult);
            AuthorizationResult result = ProcessAuthorizationResult(adalWabResult, callState);
            string userId = null;
            if (args.ContinuationData.ContainsKey("user_id"))
            {
                userId = (string) args.ContinuationData["user_id"];
            }

            return Task.Run(() => GetAccessTokenFromAuthorizationResult(result,
                new Uri((string) args.ContinuationData["redirect_uri"]), userId,
                (string) args.ContinuationData["resource"], (string) args.ContinuationData["client_id"], callState))
                .AsAsyncOperation();
        }

        private async Task<AuthenticationResult> GetAccessTokenFromAuthorizationResult(
            AuthorizationResult authorizationResult, Uri redirectUri, string userId,
            string resource, string clientId, CallState callState)
        {
            AuthenticationResult result = null;
            if (authorizationResult.Status == AuthorizationStatus.Succeeded)
            {
                string uri = this.Authenticator.TokenUri;
                result =
                    await
                        OAuth2Request.SendTokenRequestAsync(uri, authorizationResult.Code, redirectUri, resource,
                            clientId, callState);
                await this.UpdateAuthorityTenantAsync(result.TenantId, callState);
                this.tokenCacheManager.StoreToCache(result, resource, clientId);

                VerifyUserMatch(userId, result);
            }
            else
            {
                result = PlatformSpecificHelper.ProcessServiceError(authorizationResult.Error,
                    authorizationResult.ErrorDescription);
            }
            //execute callback
            if (instance != null)
            {
                var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
                await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => instance(result));
                instance = null;
            }
            return result;
        }

        private AuthorizationResult ProcessAuthorizationResult(AdalWebAuthenticationResult adalWebAuthenticationResult, CallState callState)
        {
            AuthorizationResult result;
            switch (adalWebAuthenticationResult.ResponseStatus)
            {
                case WebAuthenticationStatus.Success:
                    result = OAuth2Response.ParseAuthorizeResponse(adalWebAuthenticationResult.ResponseData, callState);
                    break;
                case WebAuthenticationStatus.ErrorHttp:
                    result = new AuthorizationResult(ActiveDirectoryAuthenticationError.AuthenticationFailed,
                        adalWebAuthenticationResult.ResponseErrorDetail.ToString());
                    break;
                case WebAuthenticationStatus.UserCancel:
                    result = new AuthorizationResult(ActiveDirectoryAuthenticationError.AuthenticationCanceled,
                        ActiveDirectoryAuthenticationErrorMessage.AuthenticationCanceled);
                    break;
                default:
                    result = new AuthorizationResult(ActiveDirectoryAuthenticationError.AuthenticationFailed,
                        ActiveDirectoryAuthenticationErrorMessage.AuthorizationServerInvalidResponse);
                    break;
            }
            return result;
        }
    }
}