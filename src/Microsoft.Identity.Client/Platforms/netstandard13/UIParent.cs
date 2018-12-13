//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using System;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Contains UI properties for interactive flows, such as the parent window (on Windows), or the parent activity (on Xamarin.Android), and 
    /// which browser to use (on Xamarin.Android and Xamarin.iOS)
    /// </summary> 
    public sealed class UIParent
    {
        internal CoreUIParent CoreUIParent { get; }

        /// <summary>
        /// Platform agnostic default constructor.
        /// </summary>
        public UIParent()
        {
            CoreUIParent = new CoreUIParent();
        }

        /// <summary>
        /// Platform agnostic constructor that allows building an UIParent from a NetStandard assembly. 
        /// </summary>
        /// <remarks>Interactive auth is not currently implemented in .net core</remarks>
        /// <param name="parent">An owner window to which to attach the webview to.
        /// On Android, it is mandatory to pass in an Activity.
        /// On all other platforms, it is not required.
        /// On .net desktop, it is optional - you can either pass a System.Windows.Forms.IWin32Window or an System.IntPtr
        /// to a window handle or null. This is used to center the webview. </param>
        /// <param name="useEmbeddedWebview">Flag to determine between embedded vs system browser. Currently affects only iOS and Android. See https://aka.ms/msal-net-uses-web-browser </param>
        public UIParent(object parent, bool useEmbeddedWebview)
        {
            ThrowPlatformNotSupported();
        }

        /// <summary>
        /// Checks if the system weview can be used. 
        /// </summary>
        public static bool IsSystemWebviewAvailable() // This is part of the NetStandard "interface" 
        {
            ThrowPlatformNotSupported();
            return false;
        }

        /// <summary>
        /// For the rare case when an application actually uses the netstandard implementation
        /// i.e. other frameworks - e.g. Xamarin.MAC - or MSAL.netstandard loaded via reflection
        /// </summary>
        private static void ThrowPlatformNotSupported()
        {
            throw new PlatformNotSupportedException("Interactive Authentication flows are not supported when the NetStandard assembly is used at runtime. " +
                                                    "Consider using Device Code Flow https://aka.ms/msal-device-code-flow or " +
                                                    "Integrated Windows Auth https://aka.ms/msal-net-iwa");
        }
    }
}