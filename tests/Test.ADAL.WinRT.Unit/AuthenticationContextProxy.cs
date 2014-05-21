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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Clients.ActiveDirectory;

using Test.ADAL.Common;

namespace Test.ADAL.Common
{
    internal partial class AuthenticationContextProxy
    {
        public AuthenticationContextProxy(string authority, bool validateAuthority, TokenCacheStoreType tokenCacheStoreType)
        {
            IDictionary<TokenCacheKey, string> tokenCacheStore = null;
            if (tokenCacheStoreType == TokenCacheStoreType.InMemory)
            {
                tokenCacheStore = new Dictionary<TokenCacheKey, string>();
            }

            this.context = new AuthenticationContext(authority, validateAuthority, tokenCacheStore);
            this.context.CorrelationId = new Guid(FixedCorrelationId);
        }

        public static void SetEnvironmentVariable(string environmentVariable, string environmentVariableValue)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values[environmentVariable] = environmentVariableValue;
        }

        public static void SetCredentials(string userNameIn, string passwordIn)
        {
        }

        public static void InitializeTest()
        {
            ClearDefaultCache();
        }

        public static void ClearDefaultCache()
        {
            var dummyContext = new AuthenticationContext("https://dummy/dummy", false);
            dummyContext.TokenCacheStore.Clear();
        }

        public static void Delay(int sleepMilliSeconds)
        {
            }

        internal AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, UserIdentifier userId)
        {
            return GetAuthenticationResultProxy(this.context.AcquireTokenAsync(resource, clientId, redirectUri, userId).AsTask().Result);
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, UserIdentifier userId, string extraQueryParameters)
        {
            return GetAuthenticationResultProxy(this.context.AcquireTokenAsync(resource, clientId, redirectUri, userId, extraQueryParameters).AsTask().Result);
        }

        internal AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri)
        {
            return GetAuthenticationResultProxy(this.context.AcquireTokenAsync(resource, clientId, redirectUri).AsTask().Result);
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, PromptBehaviorProxy promptBehaviorProxy)
        {
            PromptBehavior promptBehavior = (promptBehaviorProxy == PromptBehaviorProxy.Always) ? PromptBehavior.Always : PromptBehavior.Auto;

            return GetAuthenticationResultProxy(this.context.AcquireTokenAsync(resource, clientId, redirectUri, promptBehavior).AsTask().Result);
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, UserIdentifier userId, PromptBehaviorProxy promptBehaviorProxy)
        {
            PromptBehavior promptBehavior = (promptBehaviorProxy == PromptBehaviorProxy.Always) ? PromptBehavior.Always : PromptBehavior.Auto;

            return GetAuthenticationResultProxy(this.context.AcquireTokenAsync(resource, clientId, redirectUri, userId, promptBehavior).AsTask().Result);
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId)
        {
            return GetAuthenticationResultProxy(await this.context.AcquireTokenByRefreshTokenAsync(refreshToken, clientId));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, string resource)
        {
            return GetAuthenticationResultProxy(await this.context.AcquireTokenByRefreshTokenAsync(refreshToken, clientId, resource));
        }

        private static AuthenticationResultProxy GetAuthenticationResultProxy(AuthenticationResult result)
        {
            return new AuthenticationResultProxy
            {
                AccessToken = result.AccessToken,
                AccessTokenType = result.AccessTokenType,
                ExpiresOn = result.ExpiresOn,
                IsMultipleResourceRefreshToken = result.IsMultipleResourceRefreshToken,
                RefreshToken = result.RefreshToken,
                TenantId = result.TenantId,
                UserInfo = result.UserInfo,
                Error = result.Error,
                ErrorDescription = result.ErrorDescription,
                Status = (result.Status == AuthenticationStatus.Success) ? AuthenticationStatusProxy.Success :
                    ((result.Status == AuthenticationStatus.ClientError) ? AuthenticationStatusProxy.ClientError : AuthenticationStatusProxy.ServiceError),                
                ExceptionStatusCode = result.StatusCode
            };
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string validResource, string validClientId, UserCredentialProxy credential)
        {
            return GetAuthenticationResultProxy(await this.context.AcquireTokenAsync(validResource, validClientId,
                (credential.Password == null) ?
                new UserCredential(credential.UserId) :
                new UserCredential(credential.UserId, credential.Password)));
        }
    }
}
