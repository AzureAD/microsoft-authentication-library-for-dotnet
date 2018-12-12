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

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Config;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common.Core.Mocks;

namespace Microsoft.Identity.Test.Unit
{
    internal static class TestCommon
    {
        public static void ResetStateAndInitMsal()
        {
            ModuleInitializer.ForceModuleInitializationTestOnly();
            ResetState();
            new AadInstanceDiscovery(null, null, true);
            new ValidatedAuthoritiesCache(true);
            new AuthorityEndpointResolutionManager(null, true);
        }

        public static void ResetState()
        {
            Logger.LogCallback = null;
            Logger.PiiLoggingEnabled = false;
            Logger.Level = LogLevel.Info;
            Logger.DefaultLoggingEnabled = false;
        }

        public static IServiceBundle CreateServiceBundleWithCustomHttpManager(
            IHttpManager httpManager, 
            ITelemetryReceiver telemetryReceiver = null,
            string authority = ClientApplicationBase.DefaultAuthority,
            bool validateAuthority = true,
            string clientId = MsalTestConstants.ClientId)
        {
            var appConfig = new ApplicationConfiguration()
            {
                ClientId = clientId,
                HttpManager = httpManager,
                TelemetryReceiver = telemetryReceiver,
            };
            appConfig.AddAuthorityInfo(AuthorityInfo.FromAuthorityUri(authority, validateAuthority, true));

            return ServiceBundle.Create(appConfig);
        }

        public static IServiceBundle CreateDefaultServiceBundle()
        {
            return CreateServiceBundleWithCustomHttpManager(null);
        }


        public static void MockInstanceDiscoveryAndOpenIdRequest(MockHttpManager mockHttpManager)
        {
            mockHttpManager.AddInstanceDiscoveryMockHandler();
            mockHttpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
        }
    }
}