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
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.UI
{
    internal class CustomWebUiHandler : IWebUI
    {
        private readonly ICustomWebUi _customWebUi;

        public CustomWebUiHandler(ICustomWebUi customWebUi)
        {
            _customWebUi = customWebUi;
        }

        /// <inheritdoc />
        public async Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext)
        {
            requestContext.Logger.Info(LogMessages.CustomWebUiAcquiringAuthorizationCode);

            try
            {
                requestContext.Logger.InfoPii(LogMessages.CustomWebUiCallingAcquireAuthorizationCodePii(authorizationUri, redirectUri),
                                              LogMessages.CustomWebUiCallingAcquireAuthorizationCodeNoPii);
                var uri = await _customWebUi.AcquireAuthorizationCodeAsync(authorizationUri, redirectUri)
                                            .ConfigureAwait(false);
                if (uri == null)
                {
                    throw new MsalCustomWebUiFailedException(CoreErrorMessages.CustomWebUiReturnedNullUri);
                }

                if (uri.Authority.Equals(redirectUri.Authority, StringComparison.OrdinalIgnoreCase) &&
                    uri.AbsolutePath.Equals(redirectUri.AbsolutePath))
                {
                    IDictionary<string, string> inputQp = CoreHelpers.ParseKeyValueList(
                        authorizationUri.Query.Substring(1),
                        '&',
                        true,
                        null);

                    requestContext.Logger.Info(LogMessages.CustomWebUiRedirectUriMatched);
                    return new AuthorizationResult(AuthorizationStatus.Success, uri.OriginalString)
                    {
                        State = inputQp[OAuth2Parameter.State]
                    };
                }

                throw new MsalCustomWebUiFailedException(CoreErrorMessages.CustomWebUiRedirectUriWasNotMatchedToProperUri(uri.AbsolutePath, redirectUri.AbsolutePath));
            }
            catch (Exception ex)
            {
                var authStatus = AuthorizationStatus.UnknownError;

                if (ex is OperationCanceledException)
                {
                    requestContext.Logger.Info(LogMessages.CustomWebUiOperationCancelled);
                    authStatus = AuthorizationStatus.UserCancel;
                }
                else
                {
                    requestContext.Logger.WarningPiiWithPrefix(ex, CoreErrorMessages.CustomWebUiAuthorizationCodeFailed);
                }

                return new AuthorizationResult(authStatus, null);
            }
        }

        /// <inheritdoc />
        public void ValidateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: false);
        }
    }
}
