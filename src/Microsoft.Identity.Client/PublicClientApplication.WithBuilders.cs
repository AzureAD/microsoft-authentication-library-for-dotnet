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
        /// 
        /// </summary>
        /// <param name="scopes"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public AcquireTokenInteractiveParameterBuilder AcquireTokenInteractive(
            IEnumerable<string> scopes, 
            object parent)
        {
            return AcquireTokenInteractiveParameterBuilder.Create(this, scopes, parent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scopes"></param>
        /// <param name="deviceCodeResultCallback"></param>
        /// <returns></returns>
        public AcquireTokenWithDeviceCodeParameterBuilder AcquireTokenWithDeviceCode(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
        {
            return AcquireTokenWithDeviceCodeParameterBuilder.Create(this, scopes, deviceCodeResultCallback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public AcquireTokenWithIntegratedWindowsAuthParameterBuilder AcquireTokenWithIntegratedWindowsAuth(
            IEnumerable<string> scopes)
        {
            return AcquireTokenWithIntegratedWindowsAuthParameterBuilder.Create(this, scopes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scopes"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
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

            ApiEvent.ApiIds apiId = ApiEvent.ApiIds.AcquireTokenWithScope;
            if (!string.IsNullOrWhiteSpace(interactiveParameters.LoginHint))
            {
                apiId = ApiEvent.ApiIds.AcquireTokenWithScopeHint;
            }
            else if (requestParams.Account != null)
            {
                apiId = ApiEvent.ApiIds.AcquireTokenWithScopeUser;
            }

            var handler = new InteractiveRequest(
                ServiceBundle,
                requestParams,
                apiId,
                interactiveParameters.ExtraScopesToConsent,
                string.IsNullOrWhiteSpace(interactiveParameters.LoginHint) ? requestParams.Account?.Username : interactiveParameters.LoginHint,
#if NET_CORE_BUILDTIME
                Prompt.SelectAccount,  // TODO(migration): fix this so we don't need the ifdef and make sure it's correct.
#else
                interactiveParameters.Prompt,
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
            coreUiParent.UseHiddenBrowser = interactiveParameters.Prompt.Equals(Prompt.Never); // todo(migration): what do we do here?
#if WINDOWS_APP
            coreUiParent.UseCorporateNetwork = UseCorporateNetwork;
#endif
#endif
            return ServiceBundle.PlatformProxy.GetWebUiFactory().CreateAuthenticationDialog(coreUiParent, requestContext);
        }
    }
}