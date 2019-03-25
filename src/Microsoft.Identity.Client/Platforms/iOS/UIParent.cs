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
using System.ComponentModel;
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;
using UIKit;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Contains UI properties for interactive flows, such as the parent window (on Windows), or the parent activity (on Xamarin.Android), and 
    /// which browser to use (on Xamarin.Android and Xamarin.iOS). 
    /// Note that <c>UIParent</c> is only used in the overrides of 
    /// <see cref="IPublicClientApplication.AcquireTokenAsync(System.Collections.Generic.IEnumerable{string})"/>, not in the
    /// fluent API (<see cref="IPublicClientApplication.AcquireTokenInteractive(System.Collections.Generic.IEnumerable{string}, object)"/>
    /// where the parent window is passed explicity and the <see cref="AcquireTokenInteractiveParameterBuilder.WithUseEmbeddedWebView(bool)"/>
    /// can be used to set kind of web view to use.
    /// </summary>
    public sealed class UIParent
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        [Obsolete(MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public UIParent()
        {
            throw new NotImplementedException(MsalErrorMessage.AkaMsmsalnet3BreakingChanges);
        }
        
        /// <summary>
        /// Constructor for iOS for directing the application to use the embedded webview instead of the
        /// system browser. See https://aka.ms/msal-net-uses-web-browser
        /// </summary>
        /// <remarks>This method is likely to be removed (replaced) before final release</remarks>
        [Obsolete(MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public UIParent(bool useEmbeddedWebview)
            : this()
        {
            throw new NotImplementedException(MsalErrorMessage.AkaMsmsalnet3BreakingChanges);
        }

#if iOS_RUNTIME
        /// <summary>
        /// Platform agnostic constructor that allows building a UIParent from a NetStandard assembly.
        /// On iOS, the parent is ignored, you can pass null.
        /// </summary>
        /// <remarks>This constructor is only avaiable at runtime, to provide support for NetStandard</remarks>
        /// <param name="parent">Ignored on iOS</param>
        /// <param name="useEmbeddedWebview">Flag to determine between embedded vs system browser. See https://aka.ms/msal-net-uses-web-browser </param>
        [Obsolete(MsalErrorMessage.AkaMsmsalnet3BreakingChanges, true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public UIParent(object parent, bool useEmbeddedWebview) :
            this(useEmbeddedWebview)
        {
            throw new NotImplementedException(MsalErrorMessage.AkaMsmsalnet3BreakingChanges);
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
