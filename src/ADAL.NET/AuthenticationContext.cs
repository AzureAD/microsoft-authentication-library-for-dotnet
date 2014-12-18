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
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// The main class representing the authority issuing tokens for resources.
    /// </summary>
    public sealed partial class AuthenticationContext
    {
        private object ownerWindow;

        /// <summary>
        /// Gets or sets the owner of the browser dialog which pops up for receiving user credentials. It can be null.
        /// </summary>
        public object OwnerWindow
        {
            get
            {
                return this.ownerWindow;
            }

            set
            {
                WebUIFactory.ThrowIfUIAssemblyUnavailable();
                this.ownerWindow = value;
            }
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userCredential">The user credential to use for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, string clientId, UserCredential userCredential)
        {
            return await this.AcquireTokenCommonAsync(resource, clientId, userCredential);
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userAssertion">The assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, string clientId, UserAssertion userAssertion)
        {
            return await this.AcquireTokenCommonAsync(resource, clientId, userAssertion);
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>        
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, ClientCredential clientCredential)
        {
            return await this.AcquireTokenForClientCommonAsync(resource, new ClientKey(clientCredential));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, ClientAssertionCertificate clientCertificate)
        {
            return await this.AcquireTokenForClientCommonAsync(resource, new ClientKey(clientCertificate, this.Authenticator));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, ClientAssertion clientAssertion)
        {
            return await this.AcquireTokenForClientCommonAsync(resource, new ClientKey(clientAssertion));
        }

        /// <summary>
        /// Acquires security token from the authority using authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="clientCredential">The credential to use for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientCredential clientCredential)
        {
            return await this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientCredential), null);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="clientCredential">The credential to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientCredential clientCredential, string resource)
        {
            return await this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientCredential), resource);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientAssertion clientAssertion)
        {
            return await this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientAssertion), null);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientAssertion clientAssertion, string resource)
        {
            return await this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientAssertion), resource);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientAssertionCertificate clientCertificate)
        {
            return await this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientCertificate, this.Authenticator), null);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilentAsync(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientAssertionCertificate clientCertificate, string resource)
        {
            return await this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientCertificate, this.Authenticator), resource);
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId)
        {
            return await this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientId), null);
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. If null, token is requested for the same resource refresh token was originally issued for.
        /// If passed, resource should match the original resource used to acquire refresh token unless token service supports refresh token for multiple resources.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, string resource)
        {
            return await this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientId), resource);
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientCredential">The client credential used for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, ClientCredential clientCredential)
        {
            return await this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientCredential), null);
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientCredential">The client credential used for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. If null, token is requested for the same resource refresh token was originally issued for.
        /// If passed, resource should match the original resource used to acquire refresh token unless token service supports refresh token for multiple resources.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, ClientCredential clientCredential, string resource)
        {
            return await this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientCredential), resource);
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientAssertion">The client assertion used for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, ClientAssertion clientAssertion)
        {
            return await this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientAssertion), null);
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientAssertion">The client assertion used for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. If null, token is requested for the same resource refresh token was originally issued for.
        /// If passed, resource should match the original resource used to acquire refresh token unless token service supports refresh token for multiple resources.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, ClientAssertion clientAssertion, string resource)
        {
            return await this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientAssertion), resource);
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientCertificate">The client certificate used for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, ClientAssertionCertificate clientCertificate)
        {
            return await this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientCertificate, this.Authenticator), null);
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientCertificate">The client certificate used for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. If null, token is requested for the same resource refresh token was originally issued for.
        /// If passed, resource should match the original resource used to acquire refresh token unless token service supports refresh token for multiple resources.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, ClientAssertionCertificate clientCertificate, string resource)
        {
            return await this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientCertificate, this.Authenticator), resource);
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, ClientCredential clientCredential, UserAssertion userAssertion)
        {
            return await this.AcquireTokenOnBehalfCommonAsync(resource, new ClientKey(clientCredential), userAssertion);
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, ClientAssertionCertificate clientCertificate, UserAssertion userAssertion)
        {
            return await this.AcquireTokenOnBehalfCommonAsync(resource, new ClientKey(clientCertificate, this.Authenticator), userAssertion);
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, ClientAssertion clientAssertion, UserAssertion userAssertion)
        {
            return await this.AcquireTokenOnBehalfCommonAsync(resource, new ClientKey(clientAssertion), userAssertion);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource, string clientId)
        {
            return await this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientId), UserIdentifier.AnyUser);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource, string clientId, UserIdentifier userId)
        {
            return await this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientId), userId);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource, ClientCredential clientCredential, UserIdentifier userId)
        {
            return await this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientCredential), userId);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource, ClientAssertionCertificate clientCertificate, UserIdentifier userId)
        {
            return await this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientCertificate, this.Authenticator), userId);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource, ClientAssertion clientAssertion, UserIdentifier userId)
        {
            return await this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientAssertion), userId);
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userCredential">The credential to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public AuthenticationResult AcquireToken(string resource, string clientId, UserCredential userCredential)
        {
            return RunAsyncTask(this.AcquireTokenCommonAsync(resource, clientId, userCredential, callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userAssertion">The assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public AuthenticationResult AcquireToken(string resource, string clientId, UserAssertion userAssertion)
        {
            return RunAsyncTask(this.AcquireTokenCommonAsync(resource, clientId, userAssertion, callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public AuthenticationResult AcquireToken(string resource, ClientCredential clientCredential)
        {
            return RunAsyncTask(this.AcquireTokenForClientCommonAsync(resource, new ClientKey(clientCredential), callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public AuthenticationResult AcquireToken(string resource, ClientAssertionCertificate clientCertificate)
        {
            return RunAsyncTask(this.AcquireTokenForClientCommonAsync(resource, new ClientKey(clientCertificate, this.Authenticator), callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public AuthenticationResult AcquireToken(string resource, ClientAssertion clientAssertion)
        {
            return RunAsyncTask(this.AcquireTokenForClientCommonAsync(resource, new ClientKey(clientAssertion), callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireToken(string resource, string clientId, Uri redirectUri)
        {
            return RunAsyncTask(this.AcquireTokenCommonAsync(resource, clientId, redirectUri, PromptBehavior.Auto, UserIdentifier.AnyUser, extraQueryParameters: null, callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="promptBehavior">If <see cref="PromptBehavior.Always"/>, asks service to show user the authentication page which gives them chance to authenticate as a different user.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireToken(string resource, string clientId, Uri redirectUri, PromptBehavior promptBehavior)
        {
            return RunAsyncTask(this.AcquireTokenCommonAsync(resource, clientId, redirectUri, promptBehavior, UserIdentifier.AnyUser, extraQueryParameters: null, callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="promptBehavior">If <see cref="PromptBehavior.Always"/>, asks service to show user the authentication page which gives them chance to authenticate as a different user.</param>
        /// <param name="userId">Identifier of the user token is requested for. If created from DisplayableId, this parameter will be used to pre-populate the username field in the authentication form. Please note that the end user can still edit the username field and authenticate as a different user. 
        /// If you want to be notified of such change with an exception, create UserIdentifier with type RequiredDisplayableId. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireToken(string resource, string clientId, Uri redirectUri, PromptBehavior promptBehavior, UserIdentifier userId)
        {
            return RunAsyncTask(this.AcquireTokenCommonAsync(resource, clientId, redirectUri, promptBehavior: promptBehavior, userId: userId, extraQueryParameters: null, callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="userId">Identifier of the user token is requested for. If created from DisplayableId, this parameter will be used to pre-populate the username field in the authentication form. Please note that the end user can still edit the username field and authenticate as a different user. 
        /// If you want to be notified of such change with an exception, create UserIdentifier with type RequiredDisplayableId. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <param name="promptBehavior">If <see cref="PromptBehavior.Always"/>, asks service to show user the authentication page which gives them chance to authenticate as a different user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireToken(string resource, string clientId, Uri redirectUri, PromptBehavior promptBehavior, UserIdentifier userId, string extraQueryParameters)
        {
            return RunAsyncTask(this.AcquireTokenCommonAsync(resource, clientId, redirectUri, promptBehavior: promptBehavior, userId: userId, extraQueryParameters: extraQueryParameters, callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority using authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilent(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="clientCredential">The credential to use for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByAuthorizationCode(string authorizationCode, Uri redirectUri, ClientCredential clientCredential)
        {
            return RunAsyncTask(this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientCredential), null, callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilent(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="clientCredential">The credential to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByAuthorizationCode(string authorizationCode, Uri redirectUri, ClientCredential clientCredential, string resource)
        {
            return RunAsyncTask(this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientCredential), resource, callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilent(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByAuthorizationCode(string authorizationCode, Uri redirectUri, ClientAssertion clientAssertion)
        {
            return RunAsyncTask(this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientAssertion), null, callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilent(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByAuthorizationCode(string authorizationCode, Uri redirectUri, ClientAssertion clientAssertion, string resource)
        {
            return RunAsyncTask(this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientAssertion), resource, callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilent(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByAuthorizationCode(string authorizationCode, Uri redirectUri, ClientAssertionCertificate clientCertificate)
        {
            return RunAsyncTask(this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientCertificate, this.Authenticator), null, callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="AuthenticationContext.AcquireTokenSilent(string, string, UserIdentifier)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByAuthorizationCode(string authorizationCode, Uri redirectUri, ClientAssertionCertificate clientCertificate, string resource)
        {
            return RunAsyncTask(this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientCertificate, this.Authenticator), resource, callSync: true));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByRefreshToken(string refreshToken, string clientId)
        {
            return RunAsyncTask(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientId), null, callSync: true));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. If null, token is requested for the same resource refresh token was originally issued for.
        /// If passed, resource should match the original resource used to acquire refresh token unless token service supports refresh token for multiple resources.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByRefreshToken(string refreshToken, string clientId, string resource)
        {
            return RunAsyncTask(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientId), resource, callSync: true));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientCredential">The client credential used for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByRefreshToken(string refreshToken, ClientCredential clientCredential)
        {
            return RunAsyncTask(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientCredential), null, callSync: true));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientCredential">The client credential used for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. If null, token is requested for the same resource refresh token was originally issued for.
        /// If passed, resource should match the original resource used to acquire refresh token unless token service supports refresh token for multiple resources.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByRefreshToken(string refreshToken, ClientCredential clientCredential, string resource)
        {
            return RunAsyncTask(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientCredential), resource, callSync: true));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientAssertion">The client assertion used for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByRefreshToken(string refreshToken, ClientAssertion clientAssertion)
        {
            return RunAsyncTask(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientAssertion), null, callSync: true));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientAssertion">The client assertion used for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. If null, token is requested for the same resource refresh token was originally issued for.
        /// If passed, resource should match the original resource used to acquire refresh token unless token service supports refresh token for multiple resources.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByRefreshToken(string refreshToken, ClientAssertion clientAssertion, string resource)
        {
            return RunAsyncTask(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientAssertion), resource, callSync: true));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientCertificate">The client certificate used for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByRefreshToken(string refreshToken, ClientAssertionCertificate clientCertificate)
        {
            return RunAsyncTask(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientCertificate, this.Authenticator), null, callSync: true));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientCertificate">The client certificate used for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. If null, token is requested for the same resource refresh token was originally issued for.
        /// If passed, resource should match the original resource used to acquire refresh token unless token service supports refresh token for multiple resources.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByRefreshToken(string refreshToken, ClientAssertionCertificate clientCertificate, string resource)
        {
            return RunAsyncTask(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, new ClientKey(clientCertificate, this.Authenticator), resource, callSync: true));
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireToken(string resource, ClientCredential clientCredential, UserAssertion userAssertion)
        {
            return RunAsyncTask(this.AcquireTokenOnBehalfCommonAsync(resource, new ClientKey(clientCredential), userAssertion, callSync: true));
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireToken(string resource, ClientAssertionCertificate clientCertificate, UserAssertion userAssertion)
        {
            return RunAsyncTask(this.AcquireTokenOnBehalfCommonAsync(resource, new ClientKey(clientCertificate, this.Authenticator), userAssertion, callSync: true));
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireToken(string resource, ClientAssertion clientAssertion, UserAssertion userAssertion)
        {
            return RunAsyncTask(this.AcquireTokenOnBehalfCommonAsync(resource, new ClientKey(clientAssertion), userAssertion, callSync: true));
        }


        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public AuthenticationResult AcquireTokenSilent(string resource, string clientId)
        {
            return RunAsyncTask(this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientId), UserIdentifier.AnyUser, callSync: true));
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public AuthenticationResult AcquireTokenSilent(string resource, string clientId, UserIdentifier userId)
        {
            return RunAsyncTask(this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientId), userId, callSync: true));            
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public AuthenticationResult AcquireTokenSilent(string resource, ClientCredential clientCredential, UserIdentifier userId)
        {
            return RunAsyncTask(this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientCredential), userId, callSync: true));
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public AuthenticationResult AcquireTokenSilent(string resource, ClientAssertionCertificate clientCertificate, UserIdentifier userId)
        {
            return RunAsyncTask(this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientCertificate, this.Authenticator), userId, callSync: true));
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public AuthenticationResult AcquireTokenSilent(string resource, ClientAssertion clientAssertion, UserIdentifier userId)
        {
            return RunAsyncTask(this.AcquireTokenSilentCommonAsync(resource, new ClientKey(clientAssertion), userId, callSync: true));
        }

        /// <summary>
        /// Gets URL of the authorize endpoint including the query parameters.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be <see cref="UserIdentifier"/>.Any.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        public Uri GetAuthorizationRequestURL(string resource, string clientId, Uri redirectUri, UserIdentifier userId, string extraQueryParameters)
        {
            var handler = new AcquireTokenInteractiveHandler(this.Authenticator, this.TokenCache, resource, clientId, redirectUri, PromptBehavior.Auto, userId, extraQueryParameters, null, true);
            return RunAsyncTask(handler.CreateAuthorizationUriAsync(this.CorrelationId));            
        }

        private async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeCommonAsync(string authorizationCode, Uri redirectUri, ClientKey clientKey, string resource, bool callSync = false)
        {
            var handler = new AcquireTokenByAuthorizationCodeHandler(this.Authenticator, this.TokenCache, resource, clientKey, authorizationCode, redirectUri, callSync);
            return await handler.RunAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenForClientCommonAsync(string resource, ClientKey clientKey, bool callSync = false)
        {
            var handler = new AcquireTokenForClientHandler(this.Authenticator, this.TokenCache, resource, clientKey, callSync);
            return await handler.RunAsync();
        }

        private async Task<AuthenticationResult> AcquireTokenOnBehalfCommonAsync(string resource, ClientKey clientKey, UserAssertion userAssertion, bool callSync = false)
        {
            var handler = new AcquireTokenOnBehalfHandler(this.Authenticator, this.TokenCache, resource, clientKey, userAssertion, callSync);
            return await handler.RunAsync();
        }

        private static T RunAsyncTask<T>(Task<T> task)
        {
            try
            {
                return task.Result;
            }
            catch (AggregateException ae)
            {
                // Any exception thrown as a result of running task will cause AggregateException to be thrown with 
                // actual exception as inner.
                Exception innerException = ae.InnerExceptions[0];

                // In MTA case, AggregateException is two layer deep, so checking the InnerException for that.
                if (innerException is AggregateException)
                {
                    innerException = ((AggregateException)innerException).InnerExceptions[0];
                }

                throw innerException;
            }
        }

        internal IWebUI CreateWebAuthenticationDialog(PromptBehavior promptBehavior)
        {
            return NetworkPlugin.WebUIFactory.Create(promptBehavior, this.ownerWindow);
        }
    }
}