// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client
{
    // Default browser WebUI is not available on mobile (Android, iOS, UWP), but allow it at runtime
    // to avoid MissingMethodException

    /// <summary>
    /// Options for using the default OS browser as a separate process to handle interactive auth.
    /// MSAL will be listening for the OS browser to finish authenticating, but it cannot close the browser.
    /// It can however respond with a 200 OK message or a 302 Redirect, which can be configured here.
    /// For more details see https://aka.ms/msal-net-os-browser
    /// </summary>
#if WINDOWS_APP
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
    public partial class SystemWebViewOptions
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SystemWebViewOptions()
        {
            ValidatePlatformAvailability();
        }

        /// <summary>
        /// When the user finishes authenticating, MSAL will respond with a 200 OK message,
        /// which the browser will show to the user. 
        /// </summary>
        public string HtmlMessageSuccess { get; set; }

        /// <summary>
        /// When the user finishes authenticating, but an error occurred,
        /// MSAL will respond with a 200 OK message, which the browser will show to the user.
        /// You can use a string format e.g. "An error has occurred: {0} details: {1}"
        /// </summary>
        public string HtmlMessageError { get; set; }

        /// <summary>
        /// When the user finishes authenticating, MSAL will redirect the browser to the given Uri
        /// </summary>
        /// <remarks>Takes precedence over <see cref="HtmlMessageSuccess"/></remarks>
        public Uri BrowserRedirectSuccess { get; set; }

        /// <summary>
        /// When the user finishes authenticating, but an error occurred, MSAL will redirect the browser to the given Uri
        /// </summary>
        /// <remarks>Takes precedence over <see cref="HtmlMessageError"/></remarks>
        public Uri BrowserRedirectError { get; set; }

        /// <summary>
        /// This hides the privacy prompt displayed on iOS Devices (ver 13.0+) when set to true.
        /// By default, it is false and displays the prompt.
        /// </summary>
        #if WINDOWS_APP
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        #endif
        public bool iOSHidePrivacyPrompt { get; set; } = false;

        /// <summary>
        /// Allows developers to implement their own logic for starting a browser and navigating to a specific Uri. MSAL
        /// will use this when opening the browser. Leave it null and the user configured browser will be used.
        /// Consider using the static helpers OpenWithEdgeBrowserAsync and OpenWithChromeEdgeBrowserAsync
        /// </summary>
        public Func<Uri, Task> OpenBrowserAsync { get; set; }

        internal void LogParameters(ILoggerAdapter logger)
        {
            logger.Info($"DefaultBrowserOptions configured. HidePrivacyPrompt {iOSHidePrivacyPrompt}");

            if (logger.IsLoggingEnabled(LogLevel.Verbose))
            {
                logger.VerbosePii(
                    () => "HtmlMessageSuccess " + HtmlMessageSuccess,
                    () => "HtmlMessageSuccess? " + !String.IsNullOrEmpty(HtmlMessageSuccess));
                logger.VerbosePii(
                    () => "HtmlMessageError " + HtmlMessageError,
                    () => "HtmlMessageError? " + !String.IsNullOrEmpty(HtmlMessageError));
                logger.VerbosePii(
                    () => "BrowserRedirectSuccess " + BrowserRedirectSuccess,
                    () => "BrowserRedirectSuccess? " + (BrowserRedirectSuccess != null));
                logger.VerbosePii(
                    () => "BrowserRedirectError " + BrowserRedirectError,
                    () => "BrowserRedirectError? " + (BrowserRedirectError != null));
            }
        }

        internal static void ValidatePlatformAvailability()
        {
            // This is supported only on .net core and iOS.
            // Kept the method being part of .net standard
        }
    }
}
