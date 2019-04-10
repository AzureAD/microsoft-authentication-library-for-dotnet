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
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Mats.Internal.Events;

#if iOS
using UIKit;
#endif

#if ANDROID
using Android.App;
#endif

#if DESKTOP
using System.Windows.Forms;
#endif

#if MAC
using AppKit;
#endif

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Builder for an Interactive token request. See https://aka.ms/msal-net-acquire-token-interactively
    /// </summary>
    public sealed class AcquireTokenInteractiveParameterBuilder :
        AbstractPublicClientAcquireTokenParameterBuilder<AcquireTokenInteractiveParameterBuilder>
    {
        private AcquireTokenInteractiveParameters Parameters { get; } = new AcquireTokenInteractiveParameters();

        internal override ApiTelemetryId ApiTelemetryId => ApiTelemetryId.AcquireTokenInteractive;

        internal AcquireTokenInteractiveParameterBuilder(IPublicClientApplicationExecutor publicClientApplicationExecutor)
            : base(publicClientApplicationExecutor)
        {
        }

        // This is internal so that we can configure this from the extension methods for ICustomWebUi
        internal void SetCustomWebUi(ICustomWebUi customWebUi)
        {
            Parameters.CustomWebUi = customWebUi;
        }

        internal static AcquireTokenInteractiveParameterBuilder Create(
            IPublicClientApplicationExecutor publicClientApplicationExecutor,
            IEnumerable<string> scopes)
        {
            return new AcquireTokenInteractiveParameterBuilder(publicClientApplicationExecutor)
                .WithCurrentSynchronizationContext()
                .WithScopes(scopes);
        }

        internal AcquireTokenInteractiveParameterBuilder WithCurrentSynchronizationContext()
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithCurrentSynchronizationContext);
            Parameters.UiParent.SynchronizationContext = SynchronizationContext.Current;
            return this;
        }

        /// <summary>
        /// Specifies if the public client application should used an embedded web browser
        /// or the system default browser
        /// </summary>
        /// <param name="useEmbeddedWebView">If <c>true</c>, will use an embedded web browser,
        /// otherwise will attempt to use a system web browser. The default depends on the platform:
        /// <c>false</c> for Xamarin.iOS and Xamarin.Android, and <c>true</c> for .NET Framework,
        /// and UWP</param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenInteractiveParameterBuilder WithUseEmbeddedWebView(bool useEmbeddedWebView)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithEmbeddedWebView);
            Parameters.UseEmbeddedWebView = useEmbeddedWebView;
            return this;
        }

        /// <summary>
        /// Sets the <paramref name="loginHint"/>, in order to avoid select account
        /// dialogs in the case the user is signed-in with several identities. This method is mutually exclusive
        /// with <see cref="WithAccount(IAccount)"/>. If both are used, an exception will be thrown
        /// </summary>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenInteractiveParameterBuilder WithLoginHint(string loginHint)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithLoginHint);
            Parameters.LoginHint = loginHint;
            return this;
        }

        /// <summary>
        /// Sets the account for which the token will be retrieved. This method is mutually exclusive
        /// with <see cref="WithLoginHint(string)"/>. If both are used, an exception will be thrown
        /// </summary>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenInteractiveParameterBuilder WithAccount(IAccount account)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithAccount);
            Parameters.Account = account;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="extraScopesToConsent">Scopes that you can request the end user to consent upfront,
        /// in addition to the scopes for the protected Web API for which you want to acquire a security token.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenInteractiveParameterBuilder WithExtraScopesToConsent(IEnumerable<string> extraScopesToConsent)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithExtraScopesToConsent);
            Parameters.ExtraScopesToConsent = extraScopesToConsent;
            return this;
        }

        /// <summary>
        /// Specifies the what the interactive experience is for the user.
        /// </summary>
        /// <param name="prompt">Requested interactive experience. The default is <see cref="Prompt.SelectAccount"/>
        /// </param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenInteractiveParameterBuilder WithPrompt(Prompt prompt)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithPrompt);
            Parameters.Prompt = prompt;
            return this;
        }

        #region WithParentActivityOrWindow

        /*
         * .WithParentActivityOrWindow is platform specific but we need a solution for
         * projects like XForms where code is shared from a netstandard assembly. So expose
         * a variant of .WithParentActivityOrWindow that allows users to inject the parent as an object,
         * since Activity, ViewController etc. do not exist in NetStandard.
         */

#if RUNTIME || NETSTANDARD_BUILDTIME 
        /// <summary>
        ///  Sets a reference to the ViewController (if using Xamarin.iOS), Activity (if using Xamarin.Android)
        ///  IWin32Window or IntPtr (if using .Net Framework). Used for invoking the browser.
        /// </summary>
        /// <remarks>Mandatory only on Android. Can also be set via the PublicClientApplcation builder.</remarks>
        /// <param name="parent">The parent as an object, so that it can be used from shared NetStandard assemblies</param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenInteractiveParameterBuilder WithParentActivityOrWindow(object parent)
        {
            return WithParentObject(parent);
        }
#endif

        private AcquireTokenInteractiveParameterBuilder WithParentObject(object parent)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithParent);
#if ANDROID
            if (parent is Activity activity)
            {
                Parameters.UiParent.Activity = activity;
                Parameters.UiParent.CallerActivity = activity;
            }           
#elif iOS
            if(parent is UIViewController uiViewController)
            {
                Parameters.UiParent.CallerViewController = uiViewController;
            }
#elif MAC
            if (parent is NSWindow nsWindow)
            {
                Parameters.UiParent.CallerWindow = nsWindow;
            }

#elif DESKTOP
            if (parent is IWin32Window win32Window)
            {
                Parameters.UiParent.OwnerWindow = win32Window;
            }
            else if (parent is IntPtr intPtrWindow)
            {
                Parameters.UiParent.OwnerWindow = intPtrWindow;
            }
            // It's ok on Windows Desktop to not have an owner window, the system will just center on the display
            // instead of a parent.
#endif
            return this;
        }

#if ANDROID
        /// <summary>
        /// Sets a reference to the current Activity that triggers the browser to be shown. Required
        /// for MSAL to be able to show the browser when using Xamarin.Android
        /// </summary>
        /// <param name="activity">The current Activity</param>
        /// <returns>The builder to chain the .With methods</returns>
        [CLSCompliant(false)]
        public AcquireTokenInteractiveParameterBuilder WithParentActivityOrWindow(Activity activity)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            return WithParentObject((object)activity);
        }
#endif

#if iOS
        /// <summary>
        /// Sets a reference to the current ViewController that triggers the browser to be shown. 
        /// </summary>
        /// <param name="viewController">The current ViewController</param>
        /// <returns>The builder to chain the .With methods</returns>
        [CLSCompliant(false)]
        public AcquireTokenInteractiveParameterBuilder WithParentActivityOrWindow(UIViewController viewController)
        {
            if (viewController == null)
            {
                throw new ArgumentNullException(nameof(viewController));
            }

            return WithParentObject((object)viewController);
        }
#endif

#if DESKTOP
        /// <summary>
        /// Sets a reference to the current IWin32Window that triggers the browser to be shown.
        /// Used to center the browser that pop-up onto this window.
        /// </summary>
        /// <param name="window">The current window</param>
        /// <returns>The builder to chain the .With methods</returns>
        [CLSCompliant(false)]
        public AcquireTokenInteractiveParameterBuilder WithParentActivityOrWindow(IWin32Window window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            return WithParentObject((object)window);
        }

        /// <summary>
        /// Sets a reference to the IntPtr to a window that triggers the browser to be shown.
        /// Used to center the browser that pop-up onto this window.
        /// </summary>
        /// <param name="window">The current window</param>
        /// <returns>The builder to chain the .With methods</returns>
        [CLSCompliant(false)]
        public AcquireTokenInteractiveParameterBuilder WithParentActivityOrWindow(IntPtr window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            return WithParentObject((object)window);
        }
#endif

#if MAC
        /// <summary>
        /// Sets a reference to the current NSWindow. The browser pop-up will be centered on it. If ommited,
        /// it will be centered on the screen.
        /// </summary>
        /// <param name="nsWindow">The current NSWindow</param>
        /// <returns>The builder to chain the .With methods</returns>
        [CLSCompliant(false)]
        public AcquireTokenInteractiveParameterBuilder WithParentActivityOrWindow(NSWindow nsWindow)
        {
            if (nsWindow == null)
            {
                throw new ArgumentNullException(nameof(nsWindow));
            }

            return WithParentObject((object)nsWindow);
        }
#endif

        #endregion

        /// <inheritdoc />
        protected override void Validate()
        {
            base.Validate();

#if ANDROID
            if (Parameters.UiParent.Activity==null)
            {
                throw new InvalidOperationException(MsalErrorMessage.ActivityRequiredForParentObjectAndroid);
            }
#endif
            Parameters.LoginHint = string.IsNullOrWhiteSpace(Parameters.LoginHint)
                                          ? Parameters.Account?.Username
                                          : Parameters.LoginHint;
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return PublicClientApplicationExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            ApiEvent.ApiIds apiId = ApiEvent.ApiIds.AcquireTokenWithScope;
            if (Parameters.Account != null)
            {
                apiId = ApiEvent.ApiIds.AcquireTokenWithScopeUser;
            }
            else if (!string.IsNullOrWhiteSpace(Parameters.LoginHint))
            {
                apiId = ApiEvent.ApiIds.AcquireTokenWithScopeHint;
            }

            return apiId;
        }
    }
}
