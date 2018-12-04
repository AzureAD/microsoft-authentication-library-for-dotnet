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
        /// Default constructor.
        /// </summary>
        public UIParent()
        {
            CoreUIParent = new CoreUIParent();
        }

        #if WINDOWS_APP_RUNTIME

        /// <summary>
        /// Platform agnostic constructor that allows building an UIParent from a NetStandard assembly.
        /// On UWP, both arguments are currently ignored.
        /// </summary>
        /// <remarks>This constructor is only avaiable at runtime, to provide support for NetStandard</remarks>
        /// <param name="parent">Ignored on UWP</param>
        /// <param name="useEmbeddedWebview">UWP supports only embeddedWebview</param>
        public UIParent(object parent, bool useEmbeddedWebview) 
            : this()
        {
        }

        #endif

        // hidden webview can be used in both WinRT and desktop applications.
        internal bool UseHiddenBrowser
        {
            get => CoreUIParent.UseHiddenBrowser;
            set => CoreUIParent.UseHiddenBrowser = value;
        }

        internal bool UseCorporateNetwork
        {
            get => CoreUIParent.UseCorporateNetwork;
            set => CoreUIParent.UseCorporateNetwork = value;
        }
      
    }
}