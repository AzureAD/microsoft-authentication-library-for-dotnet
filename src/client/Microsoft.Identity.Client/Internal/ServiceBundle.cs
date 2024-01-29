// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.Http;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.WsTrust;
#if NETSTANDARD
using Microsoft.Identity.Client.Platforms.netstandard;
#endif
#if NET451_OR_GREATER
using Microsoft.Identity.Client.Platforms.netdesktop;
#endif

namespace Microsoft.Identity.Client.Internal
{
    internal class ServiceBundle : IServiceBundle
    {
        internal ServiceBundle(
            ApplicationConfiguration config,
            bool shouldClearCaches = false)
        {
            Config = config;

            ApplicationLogger = LoggerHelper.CreateLogger(Guid.Empty, config);

            PlatformProxy = config.PlatformProxy ?? PlatformProxyFactory.CreatePlatformProxy(ApplicationLogger);

            HttpManager = config.HttpManager ?? 
                HttpManagerFactory.GetHttpManager(config.HttpClientFactory ?? PlatformProxy.CreateDefaultHttpClientFactory(), 
                config.RetryOnServerErrors, config.IsManagedIdentity);

            HttpTelemetryManager = new HttpTelemetryManager();

            InstanceDiscoveryManager = new InstanceDiscoveryManager(
                HttpManager,
                shouldClearCaches,
                config.CustomInstanceDiscoveryMetadata,
                config.CustomInstanceDiscoveryMetadataUri);

            WsTrustWebRequestManager = new WsTrustWebRequestManager(HttpManager);
            ThrottlingManager = SingletonThrottlingManager.GetInstance();
            DeviceAuthManager = config.DeviceAuthManagerForTest ?? PlatformProxy.CreateDeviceAuthManager();
            KeyMaterialManager = config.KeyMaterialManagerForTest ?? PlatformProxy.GetKeyMaterialManager();

            if (shouldClearCaches) // for test
            {
                AuthorityManager.ClearValidationCache();
                PoPProviderFactory.Reset();
            }
        }

        /// <summary>
        /// This logger does not contain a correlation ID and should be used only when the correlation ID is not available
        /// i.e. before a request exists
        /// </summary>
        public ILoggerAdapter ApplicationLogger { get; }

        /// <inheritdoc/>
        public IHttpManager HttpManager { get; }

        public IInstanceDiscoveryManager InstanceDiscoveryManager { get; }

        /// <inheritdoc/>
        public IWsTrustWebRequestManager WsTrustWebRequestManager { get; }

        /// <inheritdoc/>
        public IPlatformProxy PlatformProxy { get; private set; }

        /// <inheritdoc/>
        public ApplicationConfiguration Config { get; }

        public IDeviceAuthManager DeviceAuthManager { get; }

        public IKeyMaterialManager KeyMaterialManager { get; }

        public IHttpTelemetryManager HttpTelemetryManager { get; }

        public IThrottlingProvider ThrottlingManager { get; }

        public static ServiceBundle Create(ApplicationConfiguration config)
        {
            return new ServiceBundle(config);
        }

        public void SetPlatformProxyForTest(IPlatformProxy platformProxy)
        {
            PlatformProxy = platformProxy;
        }
    }
}
