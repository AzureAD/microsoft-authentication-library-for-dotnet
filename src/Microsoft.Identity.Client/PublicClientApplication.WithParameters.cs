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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.CallConfig;
using Microsoft.Identity.Client.Config;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Features.DeviceCode;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.WsTrust;

namespace Microsoft.Identity.Client
{
    public partial class PublicClientApplication
    {
        /// <inheritdoc />
        public async Task<AuthenticationResult> AcquireTokenAsync(
            IAcquireTokenInteractiveParameters interactiveParameters,
            CancellationToken cancellationToken)
        {
            var requestParams = CreateRequestParameters(interactiveParameters, UserTokenCache);

            var handler = new InteractiveRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.AcquireTokenForClientWithScope, // TODO: need to reconcile how to get this.  do we add this in at builder time to differentiate the various calling pattern types?
                interactiveParameters.ExtraScopesToConsent,
                string.IsNullOrWhiteSpace(interactiveParameters.LoginHint) ? requestParams.Account?.Username : interactiveParameters.LoginHint,
#if NET_CORE_BUILDTIME
                UIBehavior.SelectAccount,  // todo: fix this so we don't need the ifdef and make sure it's correct.
#else
                interactiveParameters.UiBehavior,
#endif
                CreateWebAuthenticationDialogEx(
                    interactiveParameters,
                    requestParams.RequestContext));

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AuthenticationResult> AcquireTokenAsync(
            IAcquireTokenWithUsernamePasswordParameters usernamePasswordParameters,
            CancellationToken cancellationToken)
        {
#if DESKTOP || NET_CORE
            var requestParams = CreateRequestParameters(usernamePasswordParameters, UserTokenCache);
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

        /// <inheritdoc />
        public async Task<AuthenticationResult> AcquireTokenAsync(
            IAcquireTokenWithIntegratedWindowsAuthParameters integratedWindowsAuthParameters,
            CancellationToken cancellationToken)
        {
            var requestParams = CreateRequestParameters(integratedWindowsAuthParameters, UserTokenCache);
            var handler = new IntegratedWindowsAuthRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.AcquireTokenWithScopeUser,
                integratedWindowsAuthParameters.Username);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AuthenticationResult> AcquireTokenAsync(
            IAcquireTokenWithDeviceCodeParameters deviceCodeParameters,
            CancellationToken cancellationToken)
        {
            var requestParams = CreateRequestParameters(deviceCodeParameters, UserTokenCache);

            var handler = new DeviceCodeRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.None,
                deviceCodeParameters.DeviceCodeResultCallback);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        private IWebUI CreateWebAuthenticationDialogEx(
            IAcquireTokenInteractiveParameters interactiveParameters,
            RequestContext requestContext)
        {
            var coreUiParent = interactiveParameters.UiParent.CoreUiParent;

            // todo: can we just make this a consistent property that happens to not be used on some platforms so we don't have to #ifdef this?
#if ANDROID || iOS
            coreUiParent.UseEmbeddedWebView = interactiveParameters.UseEmbeddedWebView;
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