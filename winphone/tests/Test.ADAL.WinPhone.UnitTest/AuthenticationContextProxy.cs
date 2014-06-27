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
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Test.ADAL.WinPhone.UnitTest;

namespace Test.ADAL.Common
{
    internal partial class AuthenticationContextProxy
    {
        public async static Task<AuthenticationContextProxy> CreateProxyAsync(string authority, bool validateAuthority, TokenCacheStoreType tokenCacheStoreType)
        {
            AuthenticationContextProxy proxy = new AuthenticationContextProxy();

            IDictionary<TokenCacheKey, string> tokenCacheStore = null;
            if (tokenCacheStoreType == TokenCacheStoreType.InMemory)
            {
                tokenCacheStore = new Dictionary<TokenCacheKey, string>();
            }

            proxy.context = await AuthenticationContext.CreateAsync(authority, validateAuthority, tokenCacheStore);
            proxy.context.CorrelationId = new Guid(FixedCorrelationId);
            return proxy;
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

        public static async void ClearDefaultCache()
        {
            var dummyContext = await AuthenticationContext.CreateAsync("https://dummy/dummy", false);
            dummyContext.TokenCacheStore.Clear();
        }

        public static void Delay(int sleepMilliSeconds)
        {
            }

        internal AuthenticationResultProxy ContinueAcquireToken(string resource, string clientId, Uri redirectUri, string userId)
        {
            return this.ContinueAcquireToken(resource, clientId, redirectUri, userId, null);
        }

        public AuthenticationResultProxy ContinueAcquireToken(string resource, string clientId, Uri redirectUri, string userId, string extraQueryParameters)
        {
            var wabContArgs = new MockWebAuthenticationBrokerContinuationEventArgs();
            var valueSet = new ValueSet();
            valueSet["correlation_id"] = FixedCorrelationId;
            valueSet["resource"] = resource;
            valueSet["client_id"] = clientId;
            valueSet["redirect_uri"] = redirectUri.AbsoluteUri;
            valueSet["user_id"] = userId;
            wabContArgs.ContinuationData = valueSet;
            wabContArgs.Kind = ActivationKind.WebAuthenticationBrokerContinuation;
            NetworkPlugin.AdalWabResultHandler = new MockAdalWabResultHandler("https://login.windows.net/aaltests.onmicrosoft.com?code=AwABAAAAvPM1KaPlrEqdFSBzjqfTGEqB2qiVJz262oC8P62ECTquJzAX9kSEgRteZAK0BH3_1ZIg-MMDRX-4QDVQiEG_apiTCM_Sy86cuXY6RfML1xl8gkKJQPsAEXauak13OubGII7orHspp4N1XxTKDpjutUTZg5ZeN97n-eH7xif0v2wybSwr2YB126jnP0jdbQNG0rsjkXmhJNQa9o__A7tzOrZC7dmI9psraTRUhInwD1dKoKPjiqh58nUtA3D6KEZZcAyCaZfkVFRESgtGs0aaoezD6PrxEMeKKUL2xk5N14BcVziej-_angRA_sbyZnZtwNK7S1np916U7G198LHbMl7r3fow85FNZLXw4N3_jcV_uB5bK69SmJ9vpBQtagJXaQd4CpcoLG8KFUPbqudABSAA", 0, WebAuthenticationStatus.Success);
            return GetAuthenticationResultProxy(this.context.ContinueAcquireToken(wabContArgs).AsTask().Result);
        }

        internal AuthenticationResultProxy ContinueAcquireToken(string resource, string clientId, Uri redirectUri)
        {
            return this.ContinueAcquireToken(resource, clientId, redirectUri, null, null);
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
                UserInfo = (result.UserInfo != null) ? new UserInfoProxy
                    {
                        FamilyName = result.UserInfo.FamilyName,
                        GivenName = result.UserInfo.GivenName,
                        IdentityProvider = result.UserInfo.IdentityProvider,
                        IsUserIdDisplayable = result.UserInfo.IsUserIdDisplayable,
                        UserId = result.UserInfo.UserId
                    }
                    : null,
                Error = result.Error,
                ErrorDescription = result.ErrorDescription,
                Status = (result.Status == AuthenticationStatus.Succeeded) ? AuthenticationStatusProxy.Succeeded : AuthenticationStatusProxy.Failed
            };
        }

        public async Task<AuthenticationResultProxy> ContinueAcquireToken(string validResource, string validClientId,
            UserCredentialProxy credential)
        {
            return null;
             //return
             //   GetAuthenticationResultProxy(
             //       await
             //           this.context.ContinueAcquireToken(validResource, validClientId,
             //               new UserCredential(credential.UserId, credential.Password)));
        }
    }
}
