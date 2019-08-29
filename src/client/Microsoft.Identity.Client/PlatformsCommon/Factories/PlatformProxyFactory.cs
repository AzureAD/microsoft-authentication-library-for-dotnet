// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using System;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.PlatformsCommon.Factories
{
    /// <summary>
    ///     Returns the platform / os specific implementation of a PlatformProxy.
    /// </summary>
    internal static class PlatformProxyFactory
    {
        /// <summary>
        ///     Gets the platform proxy, which can be used to perform platform specific operations
        /// </summary>
        public static IPlatformProxy CreatePlatformProxy(ICoreLogger logger)
        {
            var finalLogger = logger ?? MsalLogger.NullLogger;

#if NET_CORE
            return new Microsoft.Identity.Client.Platforms.netcore.NetCorePlatformProxy(finalLogger);
#elif ANDROID
            return new Microsoft.Identity.Client.Platforms.Android.AndroidPlatformProxy(finalLogger);
#elif iOS
            return new Microsoft.Identity.Client.Platforms.iOS.iOSPlatformProxy(finalLogger);
#elif MAC
            return new Platforms.Mac.MacPlatformProxy(finalLogger);
#elif WINDOWS_APP
            return new Microsoft.Identity.Client.Platforms.uap.UapPlatformProxy(finalLogger);
#elif NETSTANDARD1_3
            return new Microsoft.Identity.Client.Platforms.netstandard13.Netstandard13PlatformProxy(finalLogger);
#elif DESKTOP
            return new Microsoft.Identity.Client.Platforms.net45.NetDesktopPlatformProxy(finalLogger);
#else
            throw new PlatformNotSupportedException();
#endif
        }
    }
}
