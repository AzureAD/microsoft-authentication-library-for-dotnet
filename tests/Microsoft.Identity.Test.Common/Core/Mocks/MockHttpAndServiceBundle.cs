// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal class MockHttpAndServiceBundle : IDisposable
    {
        public MockHttpAndServiceBundle(
            TelemetryCallback telemetryCallback = null, 
            LogCallback logCallback = null,
            bool isExtendedTokenLifetimeEnabled = false)
        {
            HttpManager = new MockHttpManager();
            ServiceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(
                HttpManager, 
                telemetryCallback: telemetryCallback,
                logCallback: logCallback,
                isExtendedTokenLifetimeEnabled: isExtendedTokenLifetimeEnabled);
        }

        public IServiceBundle ServiceBundle { get; }
        public MockHttpManager HttpManager { get; }

        public void Dispose()
        {
            HttpManager.Dispose();
        }

        public AuthenticationRequestParameters CreateAuthenticationRequestParameters(
            string authority, 
            SortedSet<string> scopes, 
            ITokenCacheInternal tokenCache = null, 
            IAccount account = null,
            IDictionary<string, string> extraQueryParameters = null, 
            string claims = null)
        {
            var commonParameters = new AcquireTokenCommonParameters
            {
                Scopes = scopes ?? MsalTestConstants.Scope,
                ExtraQueryParameters = extraQueryParameters ?? new Dictionary<string, string>(),
                Claims = claims
            };

            return new AuthenticationRequestParameters(
                ServiceBundle,
                Authority.CreateAuthority(ServiceBundle, authority),
                tokenCache,
                commonParameters,
                RequestContext.CreateForTest(ServiceBundle))
            {
                Account = account
            };
        }
    }
}