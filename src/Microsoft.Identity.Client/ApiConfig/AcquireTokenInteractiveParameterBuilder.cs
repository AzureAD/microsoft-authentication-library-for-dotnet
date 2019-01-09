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
using System.Collections.Generic;

#if ANDROID
using Android.App;
#endif

#if DESKTOP
using System.Windows.Forms;
#endif

namespace Microsoft.Identity.Client.ApiConfig
{
    /// <summary>
    /// </summary>
    [CLSCompliant(false)]
    public sealed class AcquireTokenInteractiveParameterBuilder :
        AbstractAcquireTokenParameterBuilder<AcquireTokenInteractiveParameterBuilder, IAcquireTokenInteractiveParameters>
    {
        /// <summary>
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns></returns>
        internal static AcquireTokenInteractiveParameterBuilder Create(IEnumerable<string> scopes)
        {
            return new AcquireTokenInteractiveParameterBuilder().WithScopes(scopes);
        }

        /// <summary>
        /// </summary>
        /// <param name="useEmbeddedWebView"></param>
        /// <returns></returns>
        public AcquireTokenInteractiveParameterBuilder WithUseEmbeddedWebView(bool useEmbeddedWebView)
        {
            Parameters.UseEmbeddedWebView = useEmbeddedWebView;
            return this;
        }

        // TODO: UIBehavior struct is INTERNAL on .net core...  (can we change that?)
#if !NET_CORE_BUILDTIME
        /// <summary>
        /// </summary>
        /// <param name="behavior"></param>
        /// <returns></returns>
        public AcquireTokenInteractiveParameterBuilder WithUiBehavior(UIBehavior behavior)
        {
            Parameters.UiBehavior = behavior;
            return this;
        }
#endif
#if ANDROID
        /// <summary>
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        public AcquireTokenInteractiveParameterBuilder WithParentActivity(Activity activity)
        {
            Parameters.UiParent.SetAndroidActivity(activity);
            return this;
        }
#endif
#if DESKTOP
        /// <summary>
        /// </summary>
        /// <param name="ownerWindow"></param>
        /// <returns></returns>
        public AcquireTokenInteractiveParameterBuilder WithOwnerWindow(IWin32Window ownerWindow)
        {
            Parameters.UiParent.SetOwnerWindow(ownerWindow);
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="ownerWindow"></param>
        /// <returns></returns>
        public AcquireTokenInteractiveParameterBuilder WithOwnerWindow(IntPtr ownerWindow)
        {
            Parameters.UiParent.SetOwnerWindow(ownerWindow);
            return this;
        }
#endif
    }
}