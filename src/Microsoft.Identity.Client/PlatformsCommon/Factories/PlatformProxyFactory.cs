// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

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
            var finalLogger = logger ?? MsalLogger.CreateNullLogger();

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