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
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Test.ADAL.WinPhone.Unit;
using Windows.Foundation.Collections;

namespace Test.ADAL.Common
{
    internal partial class AuthenticationContextProxy
    {
        public AuthenticationContextProxy(string authority)
        {
            try
            {
                this.context = CreateAsync(authority, true, TokenCache.DefaultShared, new Guid(FixedCorrelationId)).Result;
            }
            catch (AggregateException ae)
            {
                throw ae.InnerExceptions[0];
            }
        }

        public AuthenticationContextProxy(string authority, bool validateAuthority)
        {
            try
            {
                this.context = CreateAsync(authority, validateAuthority, TokenCache.DefaultShared, new Guid(FixedCorrelationId)).Result;
            }
            catch (AggregateException ae)
            {
                throw ae.InnerExceptions[0];
            }
        }

        public AuthenticationContextProxy(string authority, bool validateAuthority, TokenCacheType tokenCacheType)
        {
            TokenCache tokenCache = null;
            if (tokenCacheType == TokenCacheType.InMemory)
            {
                tokenCache = new TokenCache();
            }

            try
            {
                this.context = CreateAsync(authority, validateAuthority, tokenCache, new Guid(FixedCorrelationId)).Result;
            }
            catch (AggregateException ae)
            {
                throw ae.InnerExceptions[0];
            }
        }

        public AuthenticationContextDelegate AuthenticationContextDelegate { get; set; }

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
            var dummyContext = CreateAsync("https://dummy/dummy", false).Result;
            dummyContext.TokenCache.Clear();
        }

        public static void Delay(int sleepMilliSeconds)
        {
        }

        internal AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri)
        {
            AuthenticationResult result = this.context.AcquireTokenSilentAsync(resource, clientId).AsTask().Result;
            if (result.Status != AuthenticationStatus.Success)
            {
                this.context.AcquireTokenAndContinue(resource, clientId, redirectUri, this.AuthenticationContextDelegate);
                result = this.ContinueAcquireTokenAsync().Result;
            }

            return GetAuthenticationResultProxy(result);
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, PromptBehaviorProxy promptBehaviorProxy)
        {
            return this.AcquireToken(resource, clientId, redirectUri);
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, PromptBehaviorProxy promptBehaviorProxy, UserIdentifier userId)
        {
            AuthenticationResult result = this.context.AcquireTokenSilentAsync(resource, clientId, userId).AsTask().Result;
            if (result.Status != AuthenticationStatus.Success)
            {
                this.context.AcquireTokenAndContinue(resource, clientId, redirectUri, userId, this.AuthenticationContextDelegate);
                result = this.ContinueAcquireTokenAsync().Result;
            }

            return GetAuthenticationResultProxy(result);
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, PromptBehaviorProxy promptBehaviorProxy, UserIdentifier userId, string extraQueryParameters)
        {
            AuthenticationResult result = this.context.AcquireTokenSilentAsync(resource, clientId, userId).AsTask().Result;
            if (result.Status != AuthenticationStatus.Success)
            {
                try
                {
                    this.context.AcquireTokenAndContinue(resource, clientId, redirectUri, userId, extraQueryParameters, this.AuthenticationContextDelegate);
                }
                catch (AdalException ex)
                {
                    return new AuthenticationResultProxy
                    {
                        Error = ex.ErrorCode,
                        ErrorDescription = ex.Message,
                        Status = AuthenticationStatusProxy.ClientError,
                    };                    
                }

                result = this.ContinueAcquireTokenAsync().Result;
            }

            return GetAuthenticationResultProxy(result);
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
                IdToken = result.IdToken,
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
            // Unsupported feature in ADAL.WinPhone
            throw new NotImplementedException();
        }

        private async Task<AuthenticationResult> ContinueAcquireTokenAsync()
        {
            ReplayerWebAuthenticationBrokerContinuationEventArgs args = new ReplayerWebAuthenticationBrokerContinuationEventArgs
            {
                AuthorizationResult = ReplayerWebUI.LastAuthorizationResult,
                ContinuationData  = new ValueSet()
            };

            foreach (KeyValuePair<string, object> kvp in ReplayerWebUI.LastHeadersMap)
            {
                args.ContinuationData[kvp.Key] = kvp.Value;
            }

            return await this.context.ContinueAcquireTokenAsync(args);
        }

        private static async Task<AuthenticationContext> CreateAsync(string authority, bool validateAuthority)
        {
            return await AuthenticationContext.CreateAsync(authority, validateAuthority);
        }

        private static async Task<AuthenticationContext> CreateAsync(string authority, bool validateAuthority, TokenCache tokenCache, Guid correlationId)
        {
            return await AuthenticationContext.CreateAsync(authority, validateAuthority, tokenCache, correlationId);
        }
    }
}
