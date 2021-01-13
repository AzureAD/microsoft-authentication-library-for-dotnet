// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;

#if iOS
using UIKit;
#endif

#if ANDROID
using Android.App;
#endif

#if DESKTOP || NET5_WIN
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

        internal AcquireTokenInteractiveParameterBuilder WithParentActivityOrWindowFunc(Func<object> parentActivityOrWindowFunc)
        {
            if (parentActivityOrWindowFunc != null)
            {
                WithParentActivityOrWindow(parentActivityOrWindowFunc());
            }

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
#if NET_CORE || NETSTANDARD 
            if (useEmbeddedWebView)
            {
                throw new MsalClientException(MsalError.WebviewUnavailable, "An embedded webview is not available on this platform. " +
                    "Please use WithUseEmbeddedWebView(false) or leave the default. " +
                    "See https://aka.ms/msal-net-os-browser for details about the system webview.");
            }
#elif WINDOWS_APP
            if (!useEmbeddedWebView)
            {
                throw new MsalClientException(
                   MsalError.WebviewUnavailable,
                   "On UWP, MSAL does not offer a system webview out of the box. Please set .WithUseEmbeddedWebview to true or leave the default. " +
                   "To use the UWP Web Authentication Manager (WAM) see https://aka.ms/msal-net-uwp-wam");
            }
#endif

            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithEmbeddedWebView, useEmbeddedWebView);
            Parameters.UseEmbeddedWebView = useEmbeddedWebView ?
                WebViewPreference.Embedded :
                WebViewPreference.System;
            return this;
        }

        // Remark: Default browser WebUI is not available on mobile (Android, iOS, UWP), but allow it at runtime
        // to avoid MissingMethodException
        /// <summary>
        /// Specifies options for using the system OS browser handle interactive authentication.
        /// </summary>
        /// <param name="options">Data object with options</param>
        /// <returns>The builder to chain the .With methods</returns>
#if !SUPPORTS_OS_SYSTEM_BROWSER
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] // hide everywhere but NetStandard
#endif
        public AcquireTokenInteractiveParameterBuilder WithSystemWebViewOptions(SystemWebViewOptions options)
        {
            SystemWebViewOptions.ValidatePlatformAvailability();

            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithSystemBrowserOptions);
            Parameters.UiParent.SystemWebViewOptions = options;
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

        
        /// <summary>
        ///  Sets a reference to the ViewController (if using Xamarin.iOS), Activity (if using Xamarin.Android)
        ///  IWin32Window or IntPtr (if using .Net Framework). Used for invoking the browser.
        /// </summary>
        /// <remarks>Mandatory only on Android. Can also be set via the PublicClientApplcation builder.</remarks>
        /// <param name="parent">The parent as an object, so that it can be used from shared NetStandard assemblies</param>
        /// <returns>The builder to chain the .With methods</returns>

#if !NETSTANDARD
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] // hide everywhere but NetStandard
#endif
        public AcquireTokenInteractiveParameterBuilder WithParentActivityOrWindow(object parent)
        {
            return WithParentObject(parent);
        }


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
            if (parent is UIViewController uiViewController)
            {
                Parameters.UiParent.CallerViewController = uiViewController;
            }
#elif MAC
            if (parent is NSWindow nsWindow)
            {
                Parameters.UiParent.CallerWindow = nsWindow;
            }

#elif DESKTOP || NET5_WIN
            if (parent is IWin32Window win32Window)
            {
                Parameters.UiParent.OwnerWindow = win32Window;
                return this;
            }
#endif
#if DESKTOP || NET5_WIN || NET_CORE

            if (parent is IntPtr intPtrWindow)
            {
                Parameters.UiParent.OwnerWindow = intPtrWindow;
            }
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

#if DESKTOP || NET5_WIN
        /// <summary>
        /// Sets a reference to the current IWin32Window that triggers the browser to be shown.
        /// Used to center the browser (embedded webview and Windows broker) that pop-up onto this window.        
        /// </summary>
        /// <param name="window">The current window as a IWin32Window</param>
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
#endif


#if DESKTOP || NET5_WIN || NET_CORE

        /// <summary>
        /// Sets a reference to the IntPtr to a window that triggers the browser to be shown.
        /// Used to center the browser that pop-up onto this window.
        /// The center of the screen or the foreground app if a value is configured.
        /// </summary>
        /// <param name="window">The current window as IntPtr</param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <remarks></remarks>
        [CLSCompliant(false)]
        public AcquireTokenInteractiveParameterBuilder WithParentActivityOrWindow(IntPtr window)
        {
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
            if (Parameters.UiParent.Activity == null)
            {
                throw new InvalidOperationException(MsalErrorMessage.ActivityRequiredForParentObjectAndroid);
            }
#endif
            if (Parameters.UiParent.SystemWebViewOptions != null &&
                Parameters.UseEmbeddedWebView == WebViewPreference.Embedded)
            {
                throw new MsalClientException(
                    MsalError.SystemWebviewOptionsNotApplicable,
                    MsalErrorMessage.EmbeddedWebviewDefaultBrowser);
            }

            if (Parameters.UiParent.SystemWebViewOptions != null &&
               Parameters.UseEmbeddedWebView == WebViewPreference.NotSpecified)
            {
                WithUseEmbeddedWebView(false);
            }

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
            return ApiEvent.ApiIds.AcquireTokenInteractive;
        }
    }
}
