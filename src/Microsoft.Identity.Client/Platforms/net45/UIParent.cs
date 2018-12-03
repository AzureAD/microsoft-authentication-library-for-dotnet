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

using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;
using System.ComponentModel;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Contains UI properties for interactive flows, such as the parent window (on Windows), or the parent activity (on Xamarin.Android), and 
    /// which browser to use (on Xamarin.Android and Xamarin.iOS)
    /// </summary> 
    public sealed class UIParent
    {
        static UIParent()
        {
            ModuleInitializer.EnsureModuleInitialized();
        }

        internal CoreUIParent CoreUIParent { get; }

        /// <summary>
        /// Creates a UIParent that will configure the underlying embedded webview to be centered on the screen
        /// </summary>
        public UIParent()
        {
            CoreUIParent = new CoreUIParent();
        }

        /// <summary>
        /// Initializes an instance for a provided parent window.
        /// </summary>
        /// <param name="ownerWindow">Parent window object reference. OPTIONAL. The expected parent window
        /// are either of type <see cref="System.Windows.Forms.IWin32Window"/> or <see cref="System.IntPtr"/> (for window handle)</param>
        public UIParent(object ownerWindow)
        {
            CoreUIParent = new CoreUIParent(ownerWindow);
        }

#if DESKTOP_RUNTIME
        /// <summary>
        /// Platform agnostic constructor that allows building an UIParent from a NetStandard assembly.
        /// </summary>
        /// <remarks>This constructor is only avaiable at runtime, to provide support for NetStandard</remarks>
        /// <param name="parent">Parent window object reference. OPTIONAL. The expected parent window
        /// are either of type <see cref="System.Windows.Forms.IWin32Window"/> or <see cref="System.IntPtr"/> (for window handle)</param>
        /// <param name="useEmbeddedWebview">Ignored, on .net desktop an embedded webview is always used</param>
        public UIParent(object parent, bool useEmbeddedWebview) :
            this(parent)
        {
        }
#endif
        /// <summary>
        /// Checks if the system weview can be used. 
        /// Currently, on .NET Desktop, only the embedded webview can used, so this always returns false
        /// </summary>
        public static bool IsSystemWebviewAvailable() // This is part of the NetStandard "interface" 
        {
            return false;
        }

        /// <summary>
        /// Hidden webview can be used in both UWP and desktop applications.
        /// </summary>
        internal bool UseHiddenBrowser
        {
            get => CoreUIParent.UseHiddenBrowser;
            set => CoreUIParent.UseHiddenBrowser = value;
        }

    }
}