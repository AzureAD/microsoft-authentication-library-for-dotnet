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
using UIKit;

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
        /// Default constructor.
        /// </summary>
        public UIParent()
        {
            CoreUIParent = new CoreUIParent();

            if (CoreUIParent.CallerViewController == null)
            {
                CoreUIParent.CallerViewController = new UIViewController();
            }            
        }
        
        /// <summary>
        /// Constructor for iOS for directing the application to use the embedded webview instead of the
        /// system browser. See https://aka.ms/msal-net-uses-web-browser
        /// </summary>
        /// <remarks>This method is likely to be removed (replaced) before final release</remarks>
        public UIParent(bool useEmbeddedWebview) : this()
        {
            CoreUIParent.UseEmbeddedWebview = useEmbeddedWebview;
        }

#if iOS_RUNTIME
        /// <summary>
        /// Platform agnostic constructor that allows building a UIParent from a NetStandard assembly.
        /// On iOS, the parent is ignored, you can pass null.
        /// </summary>
        /// <remarks>This constructor is only avaiable at runtime, to provide support for NetStandard</remarks>
        /// <param name="parent">Ignored on iOS</param>
        /// <param name="useEmbeddedWebview">Flag to determine between embedded vs system browser. See https://aka.ms/msal-net-uses-web-browser </param>
        public UIParent(object parent, bool useEmbeddedWebview) :
            this(useEmbeddedWebview)
        {

        }
#endif

        /// <summary>
        /// Checks if the system weview can be used. 
        /// Currently, on iOS, only the embedded webview is available, so this always returns false.
        /// </summary>
        public static bool IsSystemWebviewAvailable() // This is part of the NetStandard "interface" 
        {
            return true;
        }

    }
}
