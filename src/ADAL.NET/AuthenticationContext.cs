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
using System.Threading;
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
        internal async Task<AuthenticationResult> AcquireTokenAsync(string resource, string clientId, UserAssertion userAssertion)
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
            return await this.AcquireTokenCommonAsync(resource, new ClientKey(clientCredential));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, ClientAssertionCertificate clientCertificate)
        {
            return await this.AcquireTokenCommonAsync(resource, new ClientKey(clientCertificate));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, ClientAssertion clientAssertion)
        {
            return await this.AcquireTokenCommonAsync(resource, new ClientKey(clientAssertion));
        }

        /// <summary>
        /// Acquires security token from the authority using authorization code previously received.
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
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientAssertionCertificate clientCertificate)
        {
            return await this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientCertificate), null);
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientAssertionCertificate clientCertificate, string resource)
        {
            return await this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientCertificate), resource);
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId)
        {
            return await this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, null);
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
            return await this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, resource);
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <param name="clientCredential">The client credential used for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, ClientCredential clientCredential)
        {
            return await this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, new ClientKey(clientCredential), null);
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <param name="clientCredential">The client credential used for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. If null, token is requested for the same resource refresh token was originally issued for.
        /// If passed, resource should match the original resource used to acquire refresh token unless token service supports refresh token for multiple resources.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, ClientCredential clientCredential, string resource)
        {
            return await this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, new ClientKey(clientCredential), resource);
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <param name="clientAssertion">The client assertion used for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, ClientAssertion clientAssertion)
        {
            return await this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, new ClientKey(clientAssertion), null);
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <param name="clientAssertion">The client assertion used for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. If null, token is requested for the same resource refresh token was originally issued for.
        /// If passed, resource should match the original resource used to acquire refresh token unless token service supports refresh token for multiple resources.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, ClientAssertion clientAssertion, string resource)
        {
            return await this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, new ClientKey(clientAssertion), resource);
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <param name="clientCertificate">The client certificate used for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, ClientAssertionCertificate clientCertificate)
        {
            return await this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, new ClientKey(clientCertificate), null);
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <param name="clientCertificate">The client certificate used for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. If null, token is requested for the same resource refresh token was originally issued for.
        /// If passed, resource should match the original resource used to acquire refresh token unless token service supports refresh token for multiple resources.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, ClientAssertionCertificate clientCertificate, string resource)
        {
            return await this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, new ClientKey(clientCertificate), null);
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, UserAssertion userAssertion, ClientCredential clientCredential)
        {
            return await this.AcquireTokenOnBehalfCommonAsync(resource, userAssertion, new ClientKey(clientCredential));
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, UserAssertion userAssertion, ClientAssertionCertificate clientCertificate)
        {
            return await this.AcquireTokenOnBehalfCommonAsync(resource, userAssertion, new ClientKey(clientCertificate));
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource, UserAssertion userAssertion, ClientAssertion clientAssertion)
        {
            return await this.AcquireTokenOnBehalfCommonAsync(resource, userAssertion, new ClientKey(clientAssertion));
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource, string clientId)
        {
            return await this.AcquireTokenSilentCommonAsync(resource, clientId, null);
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be null.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string resource, string clientId, UserIdentifier userId)
        {
            return await this.AcquireTokenSilentCommonAsync(resource, clientId, userId);
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
            return RunAsyncTask(this.AcquireTokenCommonAsync(resource, new ClientKey(clientCredential), callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public AuthenticationResult AcquireToken(string resource, ClientAssertionCertificate clientCertificate)
        {
            return RunAsyncTask(this.AcquireTokenCommonAsync(resource, new ClientKey(clientCertificate), callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time. Refresh Token property will be null for this overload.</returns>
        public AuthenticationResult AcquireToken(string resource, ClientAssertion clientAssertion)
        {
            return RunAsyncTask(this.AcquireTokenCommonAsync(resource, new ClientKey(clientAssertion), callSync: true));
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
            return RunAsyncTask(this.AcquireTokenCommonAsync(resource, clientId, redirectUri, promptBehavior: PromptBehavior.Auto, userId: null, extraQueryParameters: null, callSync: true));
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
            return RunAsyncTask(this.AcquireTokenCommonAsync(resource, clientId, redirectUri, promptBehavior: promptBehavior, userId: null, extraQueryParameters: null, callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="promptBehavior">If <see cref="PromptBehavior.Always"/>, asks service to show user the authentication page which gives them chance to authenticate as a different user.</param>
        /// <param name="userId">Identifier of the user token is requested for. If created from DisplayableId, this parameter will be used to pre-populate the username field in the authentication form. Please note that the end user can still edit the username field and authenticate as a different user. 
        /// If you want to be notified of such change with an exception, create UserIdentifier with type RequiredDisplayableId. This parameter can be null.</param>
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
        /// If you want to be notified of such change with an exception, create UserIdentifier with type RequiredDisplayableId. This parameter can be null.</param>
        /// <param name="promptBehavior">If <see cref="PromptBehavior.Always"/>, asks service to show user the authentication page which gives them chance to authenticate as a different user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireToken(string resource, string clientId, Uri redirectUri, PromptBehavior promptBehavior, UserIdentifier userId, string extraQueryParameters)
        {
            return RunAsyncTask(this.AcquireTokenCommonAsync(resource, clientId, redirectUri, promptBehavior: promptBehavior, userId: userId, extraQueryParameters: extraQueryParameters, callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority using authorization code previously received.
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
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByAuthorizationCode(string authorizationCode, Uri redirectUri, ClientAssertionCertificate clientCertificate)
        {
            return RunAsyncTask(this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientCertificate), null, callSync: true));
        }

        /// <summary>
        /// Acquires security token from the authority using an authorization code previously received.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="redirectUri">The redirect address used for obtaining authorization code.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. It can be null if provided earlier to acquire authorizationCode.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByAuthorizationCode(string authorizationCode, Uri redirectUri, ClientAssertionCertificate clientCertificate, string resource)
        {
            return RunAsyncTask(this.AcquireTokenByAuthorizationCodeCommonAsync(authorizationCode, redirectUri, new ClientKey(clientCertificate), resource, callSync: true));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByRefreshToken(string refreshToken, string clientId)
        {
            return RunAsyncTask(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, null, callSync: true));
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
            return RunAsyncTask(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, resource, callSync: true));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <param name="clientCredential">The client credential used for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByRefreshToken(string refreshToken, string clientId, ClientCredential clientCredential)
        {
            return RunAsyncTask(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, new ClientKey(clientCredential), null, callSync: true));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <param name="clientCredential">The client credential used for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. If null, token is requested for the same resource refresh token was originally issued for.
        /// If passed, resource should match the original resource used to acquire refresh token unless token service supports refresh token for multiple resources.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByRefreshToken(string refreshToken, string clientId, ClientCredential clientCredential, string resource)
        {
            return RunAsyncTask(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, new ClientKey(clientCredential), resource, callSync: true));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <param name="clientAssertion">The client assertion used for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByRefreshToken(string refreshToken, string clientId, ClientAssertion clientAssertion)
        {
            return RunAsyncTask(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, new ClientKey(clientAssertion), null, callSync: true));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <param name="clientAssertion">The client assertion used for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. If null, token is requested for the same resource refresh token was originally issued for.
        /// If passed, resource should match the original resource used to acquire refresh token unless token service supports refresh token for multiple resources.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByRefreshToken(string refreshToken, string clientId, ClientAssertion clientAssertion, string resource)
        {
            return RunAsyncTask(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, new ClientKey(clientAssertion), resource, callSync: true));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <param name="clientCertificate">The client certificate used for token acquisition.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByRefreshToken(string refreshToken, string clientId, ClientAssertionCertificate clientCertificate)
        {
            return RunAsyncTask(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, new ClientKey(clientCertificate), null, callSync: true));
        }

        /// <summary>
        /// Acquires a security token from the authority using a Refresh Token previously received.
        /// </summary>
        /// <param name="refreshToken">Refresh Token to use in the refresh flow.</param>
        /// <param name="clientId">Name or ID of the client requesting the token.</param>
        /// <param name="clientCertificate">The client certificate used for token acquisition.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token. If null, token is requested for the same resource refresh token was originally issued for.
        /// If passed, resource should match the original resource used to acquire refresh token unless token service supports refresh token for multiple resources.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireTokenByRefreshToken(string refreshToken, string clientId, ClientAssertionCertificate clientCertificate, string resource)
        {
            return RunAsyncTask(this.AcquireTokenByRefreshTokenCommonAsync(refreshToken, clientId, new ClientKey(clientCertificate), null, callSync: true));
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <param name="clientCredential">The client credential to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireToken(string resource, UserAssertion userAssertion, ClientCredential clientCredential)
        {
            return RunAsyncTask(this.AcquireTokenOnBehalfCommonAsync(resource, userAssertion, new ClientKey(clientCredential), callSync: true));
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <param name="clientCertificate">The client certificate to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireToken(string resource, UserAssertion userAssertion, ClientAssertionCertificate clientCertificate)
        {
            return RunAsyncTask(this.AcquireTokenOnBehalfCommonAsync(resource, userAssertion, new ClientKey(clientCertificate), callSync: true));
        }

        /// <summary>
        /// Acquires an access token from the authority on behalf of a user. It requires using a user token previously received.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="userAssertion">The user assertion (token) to use for token acquisition.</param>
        /// <param name="clientAssertion">The client assertion to use for token acquisition.</param>
        /// <returns>It contains Access Token and the Access Token's expiration time.</returns>
        public AuthenticationResult AcquireToken(string resource, UserAssertion userAssertion, ClientAssertion clientAssertion)
        {
            return RunAsyncTask(this.AcquireTokenOnBehalfCommonAsync(resource, userAssertion, new ClientKey(clientAssertion), callSync: true));
        }


        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public AuthenticationResult AcquireTokenSilent(string resource, string clientId)
        {
            return RunAsyncTask(this.AcquireTokenSilentCommonAsync(resource, clientId, null));
        }

        /// <summary>
        /// Acquires security token without asking for user credential.
        /// </summary>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="userId">Identifier of the user token is requested for. This parameter can be null.</param>
        /// <returns>It contains Access Token, Refresh Token and the Access Token's expiration time. If acquiring token without user credential is not possible, the method throws AdalException.</returns>
        public AuthenticationResult AcquireTokenSilent(string resource, string clientId, UserIdentifier userId)
        {
            return RunAsyncTask(this.AcquireTokenSilentCommonAsync(resource, clientId, userId));            
        }

        internal AuthorizationResult SendAuthorizeRequest(string resource, string clientId, Uri redirectUri, UserIdentifier userId, PromptBehavior promptBehavior, string extraQueryParameters, CallState callState)
        {
            return OAuth2Request.SendAuthorizeRequest(this.Authenticator, resource, redirectUri, clientId, userId, promptBehavior, extraQueryParameters, this.CreateWebAuthenticationDialog(promptBehavior), callState);
        }

        private async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeCommonAsync(string authorizationCode, Uri redirectUri, ClientKey clientKey, string resource, bool callSync = false)
        {
            CallState callState = this.CreateCallState(callSync);
            this.ValidateAuthorityType(callState, AuthorityType.AAD);

            if (string.IsNullOrWhiteSpace(authorizationCode))
            {
                throw new ArgumentNullException("authorizationCode");
            }

            if (redirectUri == null)
            {
                throw new ArgumentNullException("redirectUri");
            }

            await this.CreateAuthenticatorAsync(callState);

            // clientId is null if clientKey contains client assertion.
            string clientId = clientKey.GetClientId();

            try
            {
                this.NotifyBeforeAccessCache(resource, clientId, null, null);
                AuthenticationResult result = await this.tokenCacheManager.LoadFromCacheAndRefreshIfNeededAsync(resource, callState, clientId);
                if (result == null)
                {
                    clientKey.Audience = this.Authenticator.SelfSignedJwtAudience;

                    result = await OAuth2Request.SendTokenRequestAsync(this.Authenticator.TokenUri, authorizationCode, redirectUri, resource, clientKey, Authenticator.SelfSignedJwtAudience, callState);

                    await this.UpdateAuthorityTenantAsync(result.TenantId, callState);

                    this.tokenCacheManager.StoreToCache(result, resource, clientId);
                }

                LogReturnedToken(result, callState);
                return result;
            }
            finally
            {
                this.NotifyAfterAccessCache(resource, clientId, null, null);
            }
        }

        private async Task<AuthenticationResult> AcquireTokenCommonAsync(string resource, ClientKey clientKey, bool callSync = false)
        {
            CallState callState = this.CreateCallState(callSync);
            this.ValidateAuthorityType(callState, AuthorityType.AAD);

            if (string.IsNullOrWhiteSpace(resource))
            {
                throw new ArgumentNullException("resource");
            }

            await this.CreateAuthenticatorAsync(callState);

            // clientId is null if clientKey contains client assertion.
            string clientId = clientKey.GetClientId();

            try
            {
                this.NotifyBeforeAccessCache(resource, clientKey.GetClientId(), null, null);
                AuthenticationResult result = await this.tokenCacheManager.LoadFromCacheAndRefreshIfNeededAsync(resource, callState, clientId);
                if (result == null)
                {
                    clientKey.Audience = this.Authenticator.SelfSignedJwtAudience;

                    result = await OAuth2Request.SendTokenRequestAsync(this.Authenticator.TokenUri, resource, clientKey, callState);

                    await this.UpdateAuthorityTenantAsync(result.TenantId, callState);

                    this.tokenCacheManager.StoreToCache(result, resource, clientId);
                }

                LogReturnedToken(result, callState);
                return result;
            }
            finally
            {
                this.NotifyAfterAccessCache(resource, clientKey.GetClientId(), null, null);
            }
        }

        private async Task<AuthenticationResult> AcquireTokenByRefreshTokenCommonAsync(string refreshToken, string clientId, ClientKey clientKey, string resource, bool callSync = false)
        {
            CallState callState = this.CreateCallState(callSync);
            this.ValidateAuthorityType(callState, AuthorityType.AAD);

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentNullException("refreshToken");
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException("clientId");
            }

            await this.CreateAuthenticatorAsync(callState);
            clientKey.Audience = this.Authenticator.SelfSignedJwtAudience;

            AuthenticationResult result = await OAuth2Request.SendTokenRequestByRefreshTokenAsync(Authenticator.TokenUri, resource, refreshToken, clientId, clientKey, callState);
            await this.UpdateAuthorityTenantAsync(result.TenantId, callState);
            LogReturnedToken(result, callState);
            return result;
        }

        private async Task<AuthenticationResult> AcquireTokenOnBehalfCommonAsync(string resource, UserAssertion userAssertion, ClientKey clientKey, bool callSync = false)
        {
            CallState callState = this.CreateCallState(callSync);
            this.ValidateAuthorityType(callState, AuthorityType.AAD);

            if (string.IsNullOrWhiteSpace(resource))
            {
                throw new ArgumentNullException("resource");
            }

            if (userAssertion == null)
            {
                throw new ArgumentNullException("userAssertion");
            }

            await this.CreateAuthenticatorAsync(callState);
            clientKey.Audience = this.Authenticator.SelfSignedJwtAudience;

            // clientId is null if clientKey contains client assertion.
            string clientId = clientKey.GetClientId();

            try
            {
                this.NotifyBeforeAccessCache(resource, clientKey.GetClientId(), null, userAssertion.UserName);
                AuthenticationResult result = await this.tokenCacheManager.LoadFromCacheAndRefreshIfNeededAsync(resource, callState, clientId, userAssertion.UserName);

                result = result ?? await OAuth2Request.SendTokenRequestOnBehalfAsync(this.Authenticator.TokenUri, resource, userAssertion, clientKey, callState);
                await this.UpdateAuthorityTenantAsync(result.TenantId, callState);
                this.tokenCacheManager.StoreToCache(result, resource, clientId);
                LogReturnedToken(result, callState);
                return result;
            }
            finally
            {
                this.NotifyAfterAccessCache(resource, clientKey.GetClientId(), null, userAssertion.UserName);
            }
        }

        private IWebUI CreateWebAuthenticationDialog(PromptBehavior promptBehavior)
        {
            return NetworkPlugin.WebUIFactory.Create(promptBehavior, this.ownerWindow);
        }

        private AuthorizationResult AcquireAuthorization(string resource, string clientId, Uri redirectUri, UserIdentifier userId, PromptBehavior promptBehavior, string extraQueryParameters, CallState callState)
        {
            if (redirectUri == null)
            {
                throw new ArgumentNullException("redirectUri");
            }
            
            AuthorizationResult authorizationResult = null;

            var sendAuthorizeRequest = new Action(
                delegate
                {
                    authorizationResult = this.SendAuthorizeRequest(resource, clientId, redirectUri, userId, promptBehavior, extraQueryParameters, callState);
                });

            // If the thread is MTA, it cannot create or comunicate with WebBrowser which is a COM control.
            // In this case, we have to create the browser in an STA thread via StaTaskScheduler object.
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
            {
                using (var staTaskScheduler = new StaTaskScheduler(1))
                {
                    Task.Factory.StartNew(sendAuthorizeRequest, CancellationToken.None, TaskCreationOptions.None, staTaskScheduler).Wait();
                }
            }
            else
            {
                sendAuthorizeRequest();
            }

            return authorizationResult;
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
                throw ae.InnerExceptions[0];
            }
        }
    }
}