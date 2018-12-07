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

using System;

namespace Microsoft.Identity.Client.Core
{
    /// <summary>
    ///     Returns the platform / os specific implementation of a PlatformProxy.
    /// </summary>
    internal class PlatformProxyFactory
    {
        // thread safety ensured by implicit LazyThreadSafetyMode.ExecutionAndPublication
        private static readonly Lazy<IPlatformProxy> PlatformProxyLazy = new Lazy<IPlatformProxy>(
            () =>
#if NET_CORE
            new Microsoft.Identity.Client.Platforms.netcore.NetCorePlatformProxy()
#elif ANDROID
            new Microsoft.Identity.Client.Platforms.Android.AndroidPlatformProxy()
#elif iOS
            new Microsoft.Identity.Client.Platforms.iOS.iOSPlatformProxy()
#elif WINDOWS_APP
            new Microsoft.Identity.Client.Platforms.uap.UapPlatformProxy()
#elif FACADE
            new NetStandard11PlatformProxy(IsMsal())
#elif NETSTANDARD1_3
            new Microsoft.Identity.Client.Platforms.netstandard13.Netstandard13PlatformProxy()
#elif DESKTOP
            new Microsoft.Identity.Client.Platforms.net45.NetDesktopPlatformProxy()
#endif
        );

        private PlatformProxyFactory()
        {
        }

        /// <summary>
        ///     Gets the platform proxy, which can be used to perform platform specific operations
        /// </summary>
        public static IPlatformProxy GetPlatformProxy()
        {
            return PlatformProxyLazy.Value;
        }
    }
}