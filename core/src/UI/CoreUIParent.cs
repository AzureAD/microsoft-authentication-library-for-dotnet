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

#if ANDROID
using System;
using Android.App;
#endif

namespace Microsoft.Identity.Core.UI
{
    internal class CoreUIParent
    {
        public CoreUIParent()
        {
        }

#if ANDROID || IOS
        internal bool UseEmbeddedWebview { get; set; }
#endif

#if ANDROID
        internal Activity Activity { get; set; }
        /// <summary>
        /// Initializes an instance for a provided activity.
        /// </summary>
        /// <param name="activity">parent activity for the call. REQUIRED.</param>
        public CoreUIParent(Activity activity)
        {
           if(activity == null)
           {		
                throw new ArgumentException("passed in activity is null", nameof(activity));		
           }	
           
            Activity = activity;
        }
#endif

#if DESKTOP || WINDOWS_APP
        //hidden webview can be used in both WinRT and desktop applications.
        internal bool UseHiddenBrowser { get; set; }
#endif

#if WINDOWS_APP
        internal bool UseCorporateNetwork { get; set; }
#endif

#if DESKTOP
        internal object OwnerWindow { get; set; }

        /// <summary>
        /// Initializes an instance for a provided parent window.
        /// </summary>
        /// <param name="ownerWindow">Parent window object reference. OPTIONAL.</param>
        public CoreUIParent(object ownerWindow)
        {
            OwnerWindow = ownerWindow;
        }
#endif
    }
}