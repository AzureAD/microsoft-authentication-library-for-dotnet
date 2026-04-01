// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Lab.Api.Core.Mocks
{
    /// <summary>
    /// Provides factory helpers for creating service bundles in MSAL tests.
    /// </summary>
    internal static class MockTestBundleHelper
    {
        internal const string ClientId = "d3adb33f-c0de-ed0c-c0de-deadb33fc0d3";

        /// <summary>
        /// Creates a service bundle backed by the supplied HTTP manager.
        /// </summary>
        public static IServiceBundle CreateServiceBundleWithCustomHttpManager(
            IHttpManager httpManager,
            LogCallback logCallback = null,
            string authority = ClientApplicationBase.DefaultAuthority,
            bool isExtendedTokenLifetimeEnabled = false,
            bool enablePiiLogging = false,
            string clientId = ClientId,
            bool clearCaches = true,
            bool validateAuthority = true,
            bool isLegacyCacheEnabled = true,
            bool isMultiCloudSupportEnabled = false,
            MsalClientType applicationType = MsalClientType.PublicClient,
            bool isInstanceDiscoveryEnabled = true,
            IPlatformProxy platformProxy = null)
        {
            var appConfig = new ApplicationConfiguration(applicationType)
            {
                ClientId = clientId,
                HttpManager = httpManager,
                RedirectUri = PlatformProxyFactory.CreatePlatformProxy(null).GetDefaultRedirectUri(clientId),
                LoggingCallback = logCallback,
                LogLevel = LogLevel.Verbose,
                EnablePiiLogging = enablePiiLogging,
                IsExtendedTokenLifetimeEnabled = isExtendedTokenLifetimeEnabled,
                Authority = Authority.CreateAuthority(authority, validateAuthority),
                LegacyCacheCompatibilityEnabled = isLegacyCacheEnabled,
                MultiCloudSupportEnabled = isMultiCloudSupportEnabled,
                IsInstanceDiscoveryEnabled = isInstanceDiscoveryEnabled,
                PlatformProxy = platformProxy,
                RetryPolicyFactory = new RetryPolicyFactory()
            };

            if (clearCaches)
            {
                ApplicationBase.ResetStateForTest();
            }

            return new ServiceBundle(appConfig);
        }
    }
}
