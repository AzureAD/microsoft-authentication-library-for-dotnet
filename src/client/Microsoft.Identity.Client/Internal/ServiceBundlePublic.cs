// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.WsTrust;

namespace Microsoft.Identity.Client.Internal
{
    internal class ServiceBundlePublic : ServiceBundle, IServiceBundlePublic
    {
        internal ServiceBundlePublic(
            ApplicationConfiguration config,
            bool shouldClearCaches = false) : base(config, shouldClearCaches)
        {
            PlatformProxy = config.PlatformProxy ?? config.PlatformProxyFactory.CreatePlatformProxy(ApplicationLogger);
            WsTrustWebRequestManager = new WsTrustWebRequestManager(HttpManager);
        }

        /// <inheritdoc />
        public IWsTrustWebRequestManager WsTrustWebRequestManager { get; }

        /// <inheritdoc />
        public IPlatformProxyPublic PlatformProxyPublic { get { return (IPlatformProxyPublic)PlatformProxy; } }

        /// <inheritdoc />
        public ApplicationConfigurationPublic ConfigPublic { get { return (ApplicationConfigurationPublic)Config; } }

        public static new ServiceBundle Create(ApplicationConfiguration config)
        {
            return new ServiceBundlePublic(config);
        }
    }
}
