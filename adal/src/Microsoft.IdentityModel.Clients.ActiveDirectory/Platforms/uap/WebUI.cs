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

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.UI;
using Windows.ApplicationModel.Core;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform
{
    internal class WebUI : IWebUI
    {
        private const string MsAppScheme = "ms-app";

        protected RequestContext context;
        protected CoreUIParent uiParent;

        public WebUI(CoreUIParent uiParent, RequestContext context)
        {
            this.uiParent = uiParent;
            this.context = context;
        }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri, RequestContext requestContext)
        {
            bool ssoMode = ReferenceEquals(redirectUri, Constants.UapWEBRedirectUri);
            if (uiParent.UseHiddenBrowser && !ssoMode && redirectUri.Scheme != Constant.MsAppScheme)
            {
                throw new AdalException(AdalError.UapRedirectUriUnsupported);
            }

            WebAuthenticationResult webAuthenticationResult;
            WebAuthenticationOptions options = (uiParent.UseCorporateNetwork &&
                                                (ssoMode || redirectUri.Scheme == Constant.MsAppScheme))
                ? WebAuthenticationOptions.UseCorporateNetwork
                : WebAuthenticationOptions.None;

            if (uiParent.UseHiddenBrowser)
            {
                options |= WebAuthenticationOptions.SilentMode;
            }

            try
            {
                webAuthenticationResult = await CoreApplication.MainView.CoreWindow.Dispatcher.RunTaskAsync(
                    async () =>
                    {
                        if (ssoMode)
                        {
                            return await WebAuthenticationBroker.AuthenticateAsync(options, authorizationUri)
                                        .AsTask()
                                        .ConfigureAwait(false);
                        }
                        else
                        {
                            return await WebAuthenticationBroker.AuthenticateAsync(options, authorizationUri, redirectUri)
                                        .AsTask()
                                        .ConfigureAwait(false);
                        }

                    }).ConfigureAwait(false);
            }
            catch (FileNotFoundException ex)
            {
                throw new AdalException(AdalError.AuthenticationUiFailed, ex);
            }
            catch (Exception ex)
            {
                if (uiParent.UseHiddenBrowser)
                {
                    throw new AdalException(AdalError.UserInteractionRequired, ex);
                }

                throw new AdalException(AdalError.AuthenticationUiFailed, ex);
            }

            AuthorizationResult result = ProcessAuthorizationResult(webAuthenticationResult, requestContext);

            return result;
        }

        private static AuthorizationResult ProcessAuthorizationResult(WebAuthenticationResult webAuthenticationResult, RequestContext requestContext)
        {
            AuthorizationResult result;
            switch (webAuthenticationResult.ResponseStatus)
            {
                case WebAuthenticationStatus.Success:
                    result = new AuthorizationResult(AuthorizationStatus.Success, webAuthenticationResult.ResponseData);
                    break;
                case WebAuthenticationStatus.ErrorHttp:
                    result = new AuthorizationResult(AuthorizationStatus.ErrorHttp)
                    {
                        Error = AdalError.Unknown,
                        ErrorDescription =
                            webAuthenticationResult.ResponseErrorDetail.ToString(CultureInfo.InvariantCulture)
                    };
                    break;
                case WebAuthenticationStatus.UserCancel:
                    result = new AuthorizationResult(AuthorizationStatus.UserCancel, null);
                    break;
                default:
                    result = new AuthorizationResult(AuthorizationStatus.UnknownError, null);
                    break;
            }

            return result;
        }
    }
}
