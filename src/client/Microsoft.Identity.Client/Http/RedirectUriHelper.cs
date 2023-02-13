// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Http
{
    internal class RedirectUriHelper
    {
        /// <summary>
        /// Check common redirect URI problems.
        /// Optionally check that the redirect URI is not the OAuth2 standard redirect URI https://login.microsoftonline.com/common/oauth2/nativeclientb
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

        public static void ValidateIosBrokerRedirectUri(Uri redirectUri, string bundleId, ILoggerAdapter logger)
        {
            string expectedRedirectUri = $"msauth.{bundleId}://auth";

            // It's important to use the original string here because the bundleId is case sensitive
            string actualRedirectUriString = redirectUri.OriginalString;

            // MSAL style redirect URI - case sensitive
            if (string.Equals(expectedRedirectUri, actualRedirectUriString.TrimEnd('/'), StringComparison.Ordinal))
            {
                logger.Verbose(() => "Valid MSAL style redirect Uri detected. ");
                return;
            }

            // ADAL style redirect URI - my_scheme://{bundleID} 
            if (redirectUri.Authority.Equals(bundleId, StringComparison.OrdinalIgnoreCase))
            {
                logger.Verbose(() => "Valid ADAL style redirect Uri detected. ");
                return;
            }

            throw new MsalClientException(
                MsalError.CannotInvokeBroker,
                $"The broker redirect URI is incorrect, it should be {expectedRedirectUri} or app_scheme ://{bundleId} - " +
                $"please visit https://aka.ms/msal-net-xamarin for details about redirect URIs. ");
        }
    }
}
