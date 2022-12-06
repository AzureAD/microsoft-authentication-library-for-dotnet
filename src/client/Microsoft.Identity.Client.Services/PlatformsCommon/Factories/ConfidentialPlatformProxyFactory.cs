// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.PlatformsCommon.Factories
{
    /// <summary>
    ///     Returns the platform / os specific implementation of a PlatformProxy.
    /// </summary>
    internal class ConfidentialPlatformProxyFactory : IPlatformProxyFactory
    {
        /// <summary>
        ///     Gets the platform proxy, which can be used to perform platform specific operations
        /// </summary>
        public IPlatformProxy CreatePlatformProxy(ILoggerAdapter logger)
        {
            var finalLogger = logger ?? LoggerHelper.NullLogger;

#if NET_CORE
            return new Microsoft.Identity.Client.Platforms.netcore.NetCorePlatformProxy(finalLogger);
#elif NETSTANDARD
            return new Microsoft.Identity.Client.Platforms.netstandard.NetStandardPlatformProxy(finalLogger);
#elif DESKTOP
            return new Microsoft.Identity.Client.Platforms.net45.NetDesktopPlatformProxy(finalLogger);
#else
            throw new PlatformNotSupportedException();
#endif
        }
    }
}
