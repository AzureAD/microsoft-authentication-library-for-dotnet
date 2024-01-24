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
    internal static class PlatformProxyFactory
    {
        /// <summary>
        ///     Gets the platform proxy, which can be used to perform platform specific operations
        /// </summary>
        public static IPlatformProxy CreatePlatformProxy(ILoggerAdapter logger)
        {
            var finalLogger = logger ?? LoggerHelper.NullLogger;

#if NET_CORE
            return new Microsoft.Identity.Client.Platforms.netcore.NetCorePlatformProxy(finalLogger);
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
            return new Microsoft.Identity.Client.Platforms.netstandard.NetStandardPlatformProxy(finalLogger);
#elif NETFRAMEWORK
            return new Microsoft.Identity.Client.Platforms.netdesktop.NetDesktopPlatformProxy(finalLogger);
#else
            throw new PlatformNotSupportedException();
#endif
        }
    }
}
