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

namespace Test.ADAL.Common
{
    internal partial class AuthenticationContextProxy
    {
        public AuthenticationContextProxy(string authority)
        {
            this.context = new AuthenticationContext(authority);
            this.context.CorrelationId = new Guid(FixedCorrelationId);
        }

        public AuthenticationContextProxy(string authority, bool validateAuthority)
        {
            this.context = new AuthenticationContext(authority, validateAuthority);
            this.context.CorrelationId = new Guid(FixedCorrelationId);
        }

        public AuthenticationContextProxy(string authority, bool validateAuthority, TokenCacheType tokenCacheType)
        {
            TokenCache tokenCache = null;
            if (tokenCacheType == TokenCacheType.InMemory)
            {
                tokenCache = new TokenCache();
            }

            this.context = new AuthenticationContext(authority, validateAuthority, tokenCache);
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
            dummyContext.TokenCache.Clear();
        }

        public static void Delay(int sleepMilliSeconds)
        {
        }

        private async Task<AuthenticationResultProxy> RunTaskAsync(Task<AuthenticationResult> task)
        {
            AuthenticationResultProxy resultProxy;

            try
            {
                AuthenticationResult result = await task;
                resultProxy = GetAuthenticationResultProxy(result);
            }
            catch (Exception ex)
            {
                resultProxy = GetAuthenticationResultProxy(ex);
            }

            return resultProxy;
        }
    }
}
