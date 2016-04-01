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

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, string clientId, Uri redirectUri, IPlatformParameters parameters)
        {
            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, clientId, redirectUri, parameters));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, string clientId, Uri redirectUri, IPlatformParameters parameters, UserIdentifier userId)
        {
            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, clientId, redirectUri, parameters, userId));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, string clientId, Uri redirectUri, IPlatformParameters parameters, UserIdentifier userId, string extraQueryParameters)
        {
            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, clientId, redirectUri, parameters, userId, extraQueryParameters));
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
