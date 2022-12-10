// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.PlatformsCommon.Factories
{
    /// <summary>
    ///     Returns the platform / os specific implementation of a PlatformProxy.
    /// </summary>
    internal class PlatformProxyFactoryPublic : IPlatformProxyFactory
    {
        /// <summary>
        ///     Gets the platform proxy, which can be used to perform platform specific operations
        /// </summary>
        public IPlatformProxy CreatePlatformProxy(ILoggerAdapter logger)
        {
            var finalLogger = logger ?? LoggerHelper.NullLogger;

#if NET_CORE
            return new Microsoft.Identity.Client.Platforms.netcore.NetCorePlatformProxyPublic(finalLogger);
#elif NET6_WIN
            return new Microsoft.Identity.Client.Platforms.net6win.Net6WinPlatformProxy(finalLogger);
#elif ANDROID
            return new Microsoft.Identity.Client.Platforms.Android.AndroidPlatformProxy(finalLogger);
#elif iOS
            return new Microsoft.Identity.Client.Platforms.iOS.iOSPlatformProxy(finalLogger);
#elif MAC
            return new Platforms.Mac.MacPlatformProxy(finalLogger);
#elif WINDOWS_APP
            return new Microsoft.Identity.Client.Platforms.uap.UapPlatformProxy(finalLogger);
#elif NETSTANDARD
            return new Microsoft.Identity.Client.Platforms.netstandard.NetStandardPlatformProxyPublic(finalLogger);
#elif DESKTOP
            return new Microsoft.Identity.Client.Platforms.net45.NetDesktopPlatformProxyPublic(finalLogger);
#else
            throw new PlatformNotSupportedException();
#endif
        }
    }
}
