// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;

namespace Microsoft.Identity.Client.Internal.Factories
{
    /// <summary>
    ///     Returns the platform / os specific implementation of a PlatformProxy.
    /// </summary>
    internal static class PcaPlatformProxyFactory
    {
        /// <summary>
        ///     Gets the platform proxy, which can be used to perform platform specific operations
        /// </summary>
        public static IPublicClientPlatformProxy CreatePlatformProxy(ICoreLogger logger)
        {
            var finalLogger = logger ?? MsalLogger.NullLogger;

#if NET_CORE
            return new Platforms.netstandardcore.NetPublicClientPlatformProxy(finalLogger);
#elif ANDROID
            return new Microsoft.Identity.Client.Platforms.Android.AndroidPlatformProxy(finalLogger);
#elif iOS
            return new Microsoft.Identity.Client.Platforms.iOS.iOSPlatformProxy(finalLogger);
#elif MAC
            return new Platforms.Mac.MacPlatformProxy(finalLogger);
#elif UWP
            return new Microsoft.Identity.Client.Platforms.uap.UapPlatformProxy(finalLogger);
#elif NETSTANDARD
            return new Platforms.netstandardcore.NetPublicClientPlatformProxy(finalLogger);
#elif DESKTOP
            return new Microsoft.Identity.Client.Platforms.net45.NetDesktopPublicClientPlatformProxy(finalLogger);
#else
            throw new PlatformNotSupportedException();
#endif
        }
    }
}
