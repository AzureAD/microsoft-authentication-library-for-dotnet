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
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.TelemetryCore;

#if iOS
using UIKit;
#endif

#if ANDROID
using Android.App;
#endif

#if DESKTOP
using System.Windows.Forms;
#endif

namespace Microsoft.Identity.Client.ApiConfig
{
    /// <summary>
    /// Builder for an Interactive token request
    /// </summary>
    public sealed class AcquireTokenInteractiveParameterBuilder :
        AbstractPublicClientAcquireTokenParameterBuilder<AcquireTokenInteractiveParameterBuilder>
    {
        private object _ownerWindow;
        private AcquireTokenInteractiveParameters Parameters { get; } = new AcquireTokenInteractiveParameters();

        /// <inheritdoc />
        internal AcquireTokenInteractiveParameterBuilder(IPublicClientApplication publicClientApplication)
            : base(publicClientApplication)
        {
        }

        // This is internal so that we can configure this from the extension methods for ICustomWebUi
        internal void SetCustomWebUi(ICustomWebUi customWebUi)
        {
            Parameters.CustomWebUi = customWebUi;
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
#elif iOS
            if(_ownerWindow is UIViewController uiViewController)
            {
                Parameters.UiParent.SetUIViewController(uiViewController);
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
#endif

            Parameters.LoginHint = string.IsNullOrWhiteSpace(Parameters.LoginHint)
                                          ? Parameters.Account?.Username
                                          : Parameters.LoginHint;

#if NET_CORE_BUILDTIME
            Parameters.Prompt = Prompt.SelectAccount;  // TODO(migration): fix this so we don't need the ifdef and make sure it's correct.
#endif
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteAsync(IPublicClientApplicationExecutor executor, CancellationToken cancellationToken)
        {
            return executor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
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