// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Http
{
    internal class RedirectUriHelper
    {
        /// <summary>
        /// Check common redirect uri problems.
        /// Optionally check that the redirect uri is not the OAuth2 standard redirect uri https://login.microsoftonline.com/common/oauth2/nativeclientb
        /// when using a system browser, because the browser cannot redirect back to the app.
        /// </summary>
        public static void Validate(Uri redirectUri, bool usesSystemBrowser = false)
        {
            if (redirectUri == null)
            {
                throw new MsalClientException(
                    MsalError.NoRedirectUri,
                    MsalErrorMessage.NoRedirectUri);

            }

            if (!string.IsNullOrWhiteSpace(redirectUri.Fragment))
            {
                throw new ArgumentException(
                    MsalErrorMessage.RedirectUriContainsFragment,
                    nameof(redirectUri));
            }

            if (usesSystemBrowser &&
                Constants.DefaultRedirectUri.Equals(redirectUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
            {
                throw new MsalClientException(
                    MsalError.DefaultRedirectUriIsInvalid,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        MsalErrorMessage.DefaultRedirectUriIsInvalid,
                        Constants.DefaultRedirectUri));
            }
        }

    }
}
