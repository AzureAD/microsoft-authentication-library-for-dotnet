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
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client
{
    public partial class PublicClientApplication : IPublicClientApplicationExecutor
    {
        internal PublicClientApplication(ApplicationConfiguration configuration)
            : base(configuration)
        {
        }

        #region ParameterBuilders
        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The interactive window will be parented to the specified
        /// window. The user will be required to select an account
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>The user will be signed-in interactively if needed,
        /// and will consent to scopes and do multi-factor authentication if such a policy was enabled in the Azure AD tenant.
        /// 
        /// You can also pass optional parameters by calling:
        /// <see cref="AcquireTokenInteractiveParameterBuilder.WithUiBehavior(UIBehavior)"/> to specify the user experience
        /// when signing-in, <see cref="AcquireTokenInteractiveParameterBuilder.WithUseEmbeddedWebView(bool)"/> to specify
        /// if you want to use the embedded web browser or the system default browser, 
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAccount(IAccount)"/> or <see cref="AbstractAcquireTokenParameterBuilder{T}.WithLoginHint(string)"/>
        /// to prevent the select account dialog from appearing in the case you want to sign-in a specific account,
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraScopesToConsent(IEnumerable{string})"/> if you want to let the
        /// user pre-consent to additional scopes (which won't be returned in the access token),
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to pass 
        /// additional query parameters to the STS, and one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthorityOverride(string)"/>
        /// in order to override the default authority set at the application construction. Note that the overriding authority needs to be part
        /// of the known authorities added to the application construction
        /// </remarks>
        [CLSCompliant(false)]
        public AcquireTokenInteractiveParameterBuilder AcquireTokenInteractive(
            IEnumerable<string> scopes, 
            object parent)
        {
            return AcquireTokenInteractiveParameterBuilder.Create(this, scopes, parent);
        }

        /// <summary>
        /// Acquires a security token on a device without a Web browser, by letting the user authenticate on 
        /// another device. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="deviceCodeResultCallback">Callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>
        /// You can also pass optional parameters by calling:
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to pass 
        /// additional query parameters to the STS, and one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthorityOverride(string)"/>
        /// in order to override the default authority set at the application construction. Note that the overriding authority needs to be part
        /// of the known authorities added to the application construction
        /// 
        /// TODO: check if we could also pass login_hint or account (I would not think they are taken into account)
        /// 
        /// </remarks>
        public AcquireTokenWithDeviceCodeParameterBuilder AcquireTokenWithDeviceCode(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
        {
            return AcquireTokenWithDeviceCodeParameterBuilder.Create(this, scopes, deviceCodeResultCallback);
        }

        /// <summary>
        /// Non-interactive request to acquire a security token for the signed-in user in Windows, 
        /// via Integrated Windows Authentication. See https://aka.ms/msal-net-iwa.
        /// The account used in this overrides is pulled from the operating system as the current user principal name
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>
        /// You can also pass optional parameters by calling:
        /// <see cref="AcquireTokenWithIntegratedWindowsAuthParameterBuilder.WithUsername(string)"/> to pass the identifier 
        /// of the user account for which to acquire a token with Integrated Windows authentication. This is generally in 
        /// UserPrincipalName (UPN) format, e.g. john.doe@contoso.com. This is normally not needed, but some Windows administrators
        /// set policies preventing applications from looking-up the signed-in user in Windows, and in that case the username
        /// needs to be passed.
        /// You can also chain with
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to pass 
        /// additional query parameters to the STS, and one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthorityOverride(string)"/>
        /// in order to override the default authority set at the application construction. Note that the overriding authority needs to be part
        /// of the known authorities added to the application construction
        /// </remarks>
        public AcquireTokenWithIntegratedWindowsAuthParameterBuilder AcquireTokenWithIntegratedWindowsAuth(
            IEnumerable<string> scopes)
        {
            return AcquireTokenWithIntegratedWindowsAuthParameterBuilder.Create(this, scopes);
        }

        /// <summary>
        /// Non-interactive request to acquire a security token from the authority, via Username/Password Authentication.
        /// Available only on .net desktop and .net core. See https://aka.ms/msal-net-up for details.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="username">Identifier of the user application requests token on behalf.
        /// Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="password">User password as a secure string.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>You can also pass optional parameters by chaining the builder with:
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to pass 
        /// additional query parameters to the STS, and one of the overrides of <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthorityOverride(string)"/>
        /// in order to override the default authority set at the application construction. Note that the overriding authority needs to be part
        /// of the known authorities added to the application construction
        /// </remarks>
        public AcquireTokenWithUsernamePasswordParameterBuilder AcquireTokenWithUsernamePassword(
            IEnumerable<string> scopes,
            string username,
            SecureString password)
        {
            return AcquireTokenWithUsernamePasswordParameterBuilder.Create(this, scopes, username, password);
        }

        #endregion // ParameterBuilders

        #region ParameterExecutors

        async Task<AuthenticationResult> IPublicClientApplicationExecutor.ExecuteAsync(
            IAcquireTokenInteractiveParameters interactiveParameters,
            CancellationToken cancellationToken)
        {
            var requestParams = CreateRequestParameters(interactiveParameters, UserTokenCacheInternal);

            var handler = new InteractiveRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.AcquireTokenForClientWithScope, // TODO(migration): need to reconcile how to get this.  do we add this in at builder time to differentiate the various calling pattern types?
                interactiveParameters.ExtraScopesToConsent,
                string.IsNullOrWhiteSpace(interactiveParameters.LoginHint) ? requestParams.Account?.Username : interactiveParameters.LoginHint,
#if NET_CORE_BUILDTIME
                UIBehavior.SelectAccount,  // TODO(migration): fix this so we don't need the ifdef and make sure it's correct.
#else
                interactiveParameters.UiBehavior,
#endif
                CreateWebAuthenticationDialogEx(
                    interactiveParameters,
                    requestParams.RequestContext));

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        async Task<AuthenticationResult> IPublicClientApplicationExecutor.ExecuteAsync(
            IAcquireTokenWithDeviceCodeParameters deviceCodeParameters,
            CancellationToken cancellationToken)
        {
            var requestParams = CreateRequestParameters(deviceCodeParameters, UserTokenCacheInternal);

            var handler = new DeviceCodeRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.None,
                deviceCodeParameters.DeviceCodeResultCallback);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        async Task<AuthenticationResult> IPublicClientApplicationExecutor.ExecuteAsync(
            IAcquireTokenWithIntegratedWindowsAuthParameters integratedWindowsAuthParameters,
            CancellationToken cancellationToken)
        {
            var requestParams = CreateRequestParameters(integratedWindowsAuthParameters, UserTokenCacheInternal);
            var handler = new IntegratedWindowsAuthRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.AcquireTokenWithScopeUser,
                integratedWindowsAuthParameters.Username);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        async Task<AuthenticationResult> IPublicClientApplicationExecutor.ExecuteAsync(
            IAcquireTokenWithUsernamePasswordParameters usernamePasswordParameters,
            CancellationToken cancellationToken)
        {
            // TODO(migration):  proper ApiEvent.ApiIds value here

#if DESKTOP || NET_CORE
            var requestParams = CreateRequestParameters(usernamePasswordParameters, UserTokenCacheInternal);
            var handler = new UsernamePasswordRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.AcquireTokenWithScopeUser,
                usernamePasswordParameters.Username, 
                usernamePasswordParameters.Password);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
#else
            await Task.Delay(0, cancellationToken).ConfigureAwait(false);  // this is here to keep compiler from complaining that this method is async when it doesn't await...
            // TODO: need better wording and proper link to aka.ms
            throw new PlatformNotSupportedException(
                "Username Password is only supported on NetFramework and .NET Core." +
                "For more details see https://aka.ms/msal-net-iwa");
#endif
        }

        #endregion // ParameterExecutors

        private IWebUI CreateWebAuthenticationDialogEx(
            IAcquireTokenInteractiveParameters interactiveParameters,
            RequestContext requestContext)
        {
            var coreUiParent = interactiveParameters.UiParent.CoreUiParent;

            // TODO(migration): can we just make this a consistent property that happens to not be used on some platforms so we don't have to #ifdef this?
#if ANDROID || iOS
            coreUiParent.UseEmbeddedWebview = interactiveParameters.UseEmbeddedWebView;
#endif

#if WINDOWS_APP || DESKTOP
// hidden web view can be used in both WinRT and desktop applications.
            coreUiParent.UseHiddenBrowser = interactiveParameters.UiBehavior.Equals(UIBehavior.Never);
#if WINDOWS_APP
            coreUiParent.UseCorporateNetwork = UseCorporateNetwork;
#endif
#endif
            return ServiceBundle.PlatformProxy.GetWebUiFactory().CreateAuthenticationDialog(coreUiParent, requestContext);
        }
    }
}