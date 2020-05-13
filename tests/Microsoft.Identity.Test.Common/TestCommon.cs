// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit;
using NSubstitute;

namespace Microsoft.Identity.Test.Common
{
    internal static class TestCommon
    {
        public static void ResetInternalStaticCaches()
        {
            // This initializes the classes so that the statics inside them are fully initialized, and clears any cached content in them.
            new InstanceDiscoveryManager(
                Substitute.For<IHttpManager>(),
                Substitute.For<IMatsTelemetryManager>(),
                true, null, null);
            new AuthorityEndpointResolutionManager(null, true);
            SingletonThrottlingManager.GetInstance().ResetCache();
        }

        public static object GetPropValue(object src, string propName)
        {
            object result = null;
            try
            {
                result = src.GetType().GetProperty(propName).GetValue(src, null);
            }
            catch
            {
                Console.WriteLine($"Property with name {propName}");
            }

            return result;
        }

        public static IServiceBundle CreateServiceBundleWithCustomHttpManager(
            IHttpManager httpManager,
            TelemetryCallback telemetryCallback = null,
            LogCallback logCallback = null,
            string authority = ClientApplicationBase.DefaultAuthority,
            bool isExtendedTokenLifetimeEnabled = false,
            bool enablePiiLogging = false,
            string clientId = TestConstants.ClientId,
            bool clearCaches = true,
            bool validateAuthority = true)
        {
            var appConfig = new ApplicationConfiguration()
            {
                ClientId = clientId,
                HttpManager = httpManager,
                RedirectUri = PlatformProxyFactory.CreatePlatformProxy(null).GetDefaultRedirectUri(clientId),
                TelemetryCallback = telemetryCallback,
                LoggingCallback = logCallback,
                LogLevel = LogLevel.Verbose,
                EnablePiiLogging = enablePiiLogging,
                IsExtendedTokenLifetimeEnabled = isExtendedTokenLifetimeEnabled,
                AuthorityInfo = AuthorityInfo.FromAuthorityUri(authority, validateAuthority)
            };

            return new ServiceBundle(appConfig, clearCaches);
        }

        public static IServiceBundle CreateDefaultServiceBundle()
        {
            return CreateServiceBundleWithCustomHttpManager(null);
        }

        public static IServiceBundle CreateDefaultAdfsServiceBundle()
        {
            return CreateServiceBundleWithCustomHttpManager(null, authority: TestConstants.OnPremiseAuthority);
        }

        internal static void MockInstanceDiscoveryAndOpenIdRequest(MockHttpManager mockHttpManager)
        {
            mockHttpManager.AddInstanceDiscoveryMockHandler();
            mockHttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityHomeTenant);
        }
    }
}
