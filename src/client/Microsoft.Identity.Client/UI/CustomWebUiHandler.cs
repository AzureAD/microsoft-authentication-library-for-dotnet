// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
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
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            requestContext.Logger.Info(LogMessages.CustomWebUiAcquiringAuthorizationCode);

            try
            {
                requestContext.Logger.InfoPii(LogMessages.CustomWebUiCallingAcquireAuthorizationCodePii(authorizationUri, redirectUri),
                                              LogMessages.CustomWebUiCallingAcquireAuthorizationCodeNoPii);
                var uri = await _customWebUi.AcquireAuthorizationCodeAsync(authorizationUri, redirectUri, cancellationToken)
                                            .ConfigureAwait(false);
                if (uri == null || String.IsNullOrWhiteSpace(uri.Query))
                {
                    throw new MsalClientException(
                        MsalError.CustomWebUiReturnedInvalidUri,
                        MsalErrorMessage.CustomWebUiReturnedInvalidUri);
                }

                if (uri.Authority.Equals(redirectUri.Authority, StringComparison.OrdinalIgnoreCase) &&
                    uri.AbsolutePath.Equals(redirectUri.AbsolutePath))
                {
                    requestContext.Logger.Info(LogMessages.CustomWebUiRedirectUriMatched);
                    return AuthorizationResult.FromUri(uri.OriginalString);
                }

                throw new MsalClientException(
                    MsalError.CustomWebUiRedirectUriMismatch,
                    MsalErrorMessage.RedirectUriMismatch(
                        uri.AbsolutePath,
                        redirectUri.AbsolutePath));
            }
            catch (OperationCanceledException)
            {
                requestContext.Logger.Info(LogMessages.CustomWebUiOperationCancelled);
                return AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);
            }
            catch (Exception ex)
            {
                requestContext.Logger.WarningPiiWithPrefix(ex, MsalErrorMessage.CustomWebUiAuthorizationCodeFailed);
                throw;
            }
        }

        /// <inheritdoc />
        public Uri UpdateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: false);
            return redirectUri;
        }
    }
}
