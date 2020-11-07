// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.Platforms.netstandardcore;

namespace Microsoft.Identity.Client.Internal
{

    /// <summary>
    ///     Returns the platform / os specific implementation of a PlatformProxy.
    /// </summary>
    internal static class CcaPlatformProxyFactory
    {
        /// <summary>
        ///     Gets the platform proxy, which can be used to perform platform specific operations
        /// </summary>
        public static IPlatformProxy CreatePlatformProxy(ICoreLogger logger)
        {
            var finalLogger = logger ?? MsalLogger.NullLogger;

            // For CCA, we only support .NET Standard 2.0
            return new NetPlatformProxy(finalLogger);

        }
    }
}
