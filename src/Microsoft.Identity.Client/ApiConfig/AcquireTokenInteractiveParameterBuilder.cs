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
using System.Threading;
using System.Threading.Tasks;

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
        AbstractPcaAcquireTokenParameterBuilder<AcquireTokenInteractiveParameterBuilder>
    {
        private object _ownerWindow;

        /// <inheritdoc />
        internal AcquireTokenInteractiveParameterBuilder(IPublicClientApplication publicClientApplication)
            : base(publicClientApplication)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="publicClientApplication"></param>
        /// <param name="scopes"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        internal static AcquireTokenInteractiveParameterBuilder Create(
            IPublicClientApplication publicClientApplication, 
            IEnumerable<string> scopes,
            object parent)
        {
            return new AcquireTokenInteractiveParameterBuilder(publicClientApplication)
                .WithScopes(scopes)
                .WithParent(parent);
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

        private AcquireTokenInteractiveParameterBuilder WithParent(object parent)
        {
            _ownerWindow = parent;
            return this;
        }

        /// <inheritdoc />
        protected override void Validate()
        {
            base.Validate();
#if ANDROID
            if (_ownerWindow is Activity activity)
            {
                Parameters.UiParent.SetAndroidActivity(activity);
            }
            else
            {
                throw new InvalidOperationException(CoreErrorMessages.ActivityRequiredForParentObjectAndroid);
            }

#elif DESKTOP
            if (_ownerWindow is IWin32Window win32Window)
            {
                Parameters.UiParent.SetOwnerWindow(win32Window);
            }
            else if (_ownerWindow is IntPtr intPtrWindow)
            {
                Parameters.UiParent.SetOwnerWindow(intPtrWindow);
            }
            // It's ok on Windows Desktop to not have an owner window, the system will just center on the display
            // instead of a parent.
#else
            if (_ownerWindow != null)
            {
                // TODO: Someone set an owner window and we're going to ignore it.  Should we throw?
            }
#endif
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteAsync(IPublicClientApplicationExecutor executor, CancellationToken cancellationToken)
        {
            return executor.ExecuteAsync((IAcquireTokenInteractiveParameters)Parameters, cancellationToken);
        }
    }
}