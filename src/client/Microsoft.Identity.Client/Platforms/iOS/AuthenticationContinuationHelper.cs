// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Foundation;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Platforms.iOS;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Static class that consumes the response from the Authentication flow and continues token acquisition. This class should be called in ApplicationDelegate whenever app loads/reloads.
    /// </summary>
    [CLSCompliant(false)]
    public static class AuthenticationContinuationHelper
    {
        /// <summary>
        /// Because this class needs to be static, we can only inject a logger from one request a time, making
        /// the correlation IDs reported unreliable in case multiple requests in parallel.
        /// </summary>
        internal static ILoggerAdapter LastRequestLogger { get; set; } // can be null

        /// <summary>
        /// Sets response for continuing authentication flow. This function will return true if the response was meant for MSAL, else it will return false.
        /// </summary>
        /// <param name="url">url used to invoke the application</param>
        public static bool SetAuthenticationContinuationEventArgs(NSUrl url)
        {
            LastRequestLogger?.InfoPii(
                () => "AuthenticationContinuationHelper - SetAuthenticationContinuationEventArgs url: " + url,
                () => "AuthenticationContinuationHelper - SetAuthenticationContinuationEventArgs ");            

            return WebviewBase.ContinueAuthentication(url.AbsoluteString, LastRequestLogger);
        }

        /// <summary>
        /// Returns if the response is from the broker app. See https://aka.ms/msal-net-ios-13-broker
        /// for more details.
        /// </summary>
        /// <param name="sourceApplication">application bundle id of the broker</param>
        /// <returns>True if the response is from broker, False otherwise.</returns>
        public static bool IsBrokerResponse(string sourceApplication)
        {
            LastRequestLogger?.Info(() => "IsBrokerResponse called with sourceApplication " + sourceApplication);

            if (string.Equals("com.microsoft.azureauthenticator", sourceApplication, StringComparison.OrdinalIgnoreCase))
            {
                LastRequestLogger?.Info("IsBrokerResponse returns true");
                return true;
            }

            if (string.IsNullOrEmpty(sourceApplication))
            {
                LastRequestLogger?.Info("IsBrokerResponse returns true (sourceApplication is null) - iOS 13+ ");

                // For iOS 13+, SourceApplication will not be returned
                // Customers will need to install iOS broker >= 6.3.19
                // MSAL.NET will generate a nonce (guid), which broker will
                // return in the response. MSAL.NET will validate a match in iOSBroker.cs
                // So if SourceApplication is null, just return, MSAL.NET will throw a 
                // specific error message if the nonce does not match.
                return true;
            }

            LastRequestLogger?.Info("IsBrokerResponse returns false");
            return false;
        }

        /// <summary>
        /// Sets broker response for continuing authentication flow.
        /// </summary>
        /// <param name="url"></param>
        public static void SetBrokerContinuationEventArgs(NSUrl url)
        {
            LastRequestLogger?.Info(() => "SetBrokercontinuationEventArgs Called with Url " + url);

            string urlString = url.AbsoluteString;
            
            if (urlString.Contains(iOSBrokerConstants.IdentifyiOSBrokerFromResponseUrl))
            {
                LastRequestLogger?.Info("SetBrokercontinuationEventArgs contains <broker> string" );

                iOSBroker.SetBrokerResponse(url);
            }
            else
            {
                LastRequestLogger?.Info("SetBrokercontinuationEventArgs does not contains <broker> string");
            }
        }
    }
}
