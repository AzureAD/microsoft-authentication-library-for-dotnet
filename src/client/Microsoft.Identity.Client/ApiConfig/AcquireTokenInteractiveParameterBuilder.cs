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
using Microsoft.Identity.Client.AppConfig;
using System.Net.Http;
using System.ComponentModel;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

#if iOS
using UIKit;
#endif

#if ANDROID
using Android.App;
#endif

#if NETFRAMEWORK 
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
            // platform WebUI factories will validate this setting

            Parameters.UseEmbeddedWebView = useEmbeddedWebView ?
                WebViewPreference.Embedded :
                WebViewPreference.System;
            return this;
        }

        // Remark: Default browser WebUI is not available on mobile (Android, UWP), but allow it at runtime
        // to avoid MissingMethodException
        /// <summary>
        /// Specifies options for using the system OS browser handle interactive authentication.
        /// </summary>
        /// <param name="options">Data object with options</param>
        /// <returns>The builder to chain the .With methods</returns>
#if WINDOWS_APP
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] 
#endif
        public AcquireTokenInteractiveParameterBuilder WithSystemWebViewOptions(SystemWebViewOptions options)
        {
            SystemWebViewOptions.ValidatePlatformAvailability();

            Parameters.UiParent.SystemWebViewOptions = options;
            return this;
        }

        /// <summary>
        /// Specifies options for using the embedded web view for interactive authentication.
        /// </summary>
        /// <param name="options">Data object with options</param>
        /// <returns>The builder to chain the .With methods</returns>
#if !SUPPORTS_WIN32 // currently only WebView2 allows customization
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        public AcquireTokenInteractiveParameterBuilder WithEmbeddedWebViewOptions(
            EmbeddedWebViewOptions options)
        {
            EmbeddedWebViewOptions.ValidatePlatformAvailability();

            Parameters.UiParent.EmbeddedWebviewOptions = options;
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
        /// <remarks>Mandatory only on Android. Can also be set via the PublicClientApplication builder.</remarks>
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

#elif NETFRAMEWORK 
            if (parent is IWin32Window win32Window)
            {
                Parameters.UiParent.OwnerWindow = win32Window.Handle;
                return this;
            }
#endif
#if NETFRAMEWORK ||  NET_CORE || NETSTANDARD

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

#if NETFRAMEWORK 
        /// <summary>
        /// Sets a reference to the current IWin32Window that triggers the browser to be shown.
        /// Used to center the browser (embedded web view and Windows broker) that pop-up onto this window.        
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

#if NETFRAMEWORK || NET_CORE || NETSTANDARD

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
            return WithParentObject(window);
        }
#endif

#if MAC
        /// <summary>
        /// Sets a reference to the current NSWindow. The browser pop-up will be centered on it. If omitted,
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

        /// <summary>
        ///  Modifies the token acquisition request so that the acquired token is a Proof-of-Possession token (PoP), rather than a Bearer token. 
        ///  PoP tokens are similar to Bearer tokens, but are bound to the HTTP request and to a cryptographic key, which MSAL can manage on Windows.
        ///  Note that only the host and path parts of the request URI will be bound.
        ///  See https://aka.ms/msal-net-pop
        /// </summary>
        /// <param name="nonce">Nonce of the protected resource which will be published as part of the WWW-Authenticate header associated with a 401 HTTP response
        /// or as part of the AuthorityInfo header associated with 200 response. Set it here to make it part of the Signed HTTP Request part of the PoP token.</param>
        /// <param name="httpMethod">The HTTP method ("GET", "POST" etc.) method that will be bound to the token. If set to null, the PoP token will not be bound to the method.
        /// Corresponds to the "m" part of the a signed HTTP request.</param>
        /// <param name="requestUri">The URI to bind the signed HTTP request to.</param>
        /// <returns>The builder.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>An Authentication header is automatically added to the request.</description></item>
        /// <item><description>The PoP token is bound to the HTTP request, more specifically to the HTTP method (GET, POST, etc.) and to the Uri (path and query, but not query parameters).</description></item>
        /// <item><description>Broker is required to use Proof-of-Possession on public clients.</description></item>
        /// </list>
        /// </remarks>
#if iOS || ANDROID || WINDOWS_UWP
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public AcquireTokenInteractiveParameterBuilder WithProofOfPossession(string nonce, HttpMethod httpMethod, Uri requestUri)
        {
            ClientApplicationBase.GuardMobileFrameworks();

            if (!ServiceBundle.Config.IsBrokerEnabled)
            {
                throw new MsalClientException(MsalError.BrokerRequiredForPop, MsalErrorMessage.BrokerRequiredForPop);
            }

            var broker = ServiceBundle.PlatformProxy.CreateBroker(ServiceBundle.Config, null);

            if (!broker.IsPopSupported)
            {
                throw new MsalClientException(MsalError.BrokerDoesNotSupportPop, MsalErrorMessage.BrokerDoesNotSupportPop);
            }

            PoPAuthenticationConfiguration popConfig = new PoPAuthenticationConfiguration(requestUri);

            if (string.IsNullOrEmpty(nonce))
            {
                throw new ArgumentNullException(nameof(nonce));
            }

            popConfig.Nonce = nonce;
            popConfig.HttpMethod = httpMethod;

            CommonParameters.PopAuthenticationConfiguration = popConfig;
            CommonParameters.AuthenticationScheme = new PopBrokerAuthenticationScheme();

            return this;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
