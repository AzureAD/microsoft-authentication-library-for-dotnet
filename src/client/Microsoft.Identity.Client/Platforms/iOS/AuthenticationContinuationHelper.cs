// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Foundation;
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
        /// Sets response for continuing authentication flow. This function will return true if the response was meant for MSAL, else it will return false.
        /// </summary>
        /// <param name="url">url used to invoke the application</param>
        public static bool SetAuthenticationContinuationEventArgs(NSUrl url)
        {
            return WebviewBase.ContinueAuthentication(url.AbsoluteString);
        }

        /// <summary>
        /// Returns if the response is from the broker app. See https://aka.ms/msal-net-ios-13-broker
        /// for more details.
        /// </summary>
        /// <param name="sourceApplication">application bundle id of the broker</param>
        /// <returns>True if the response is from broker, False otherwise.</returns>
        public static bool IsBrokerResponse(string sourceApplication)
        {
            Debug.WriteLine("IsBrokerResponse called with sourceApplication {0}", sourceApplication);

            if (sourceApplication != null && sourceApplication.Equals("com.microsoft.azureauthenticator", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (string.IsNullOrEmpty(sourceApplication))
            {
                // For iOS 13+, SourceApplication will not be returned
                // Customers will need to install iOS broker >= 6.3.19
                // MSAL.NET will generate a nonce (guid), which broker will
                // return in the response. MSAL.NET will validate a match in iOSBroker.cs
                // So if SourceApplication is null, just return, MSAL.NET will throw a 
                // specific error message if the nonce does not match.
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets broker response for continuing authentication flow.
        /// </summary>
        /// <param name="url"></param>
        public static void SetBrokerContinuationEventArgs(NSUrl url)
        {
            Debug.WriteLine("SetBrokercontinuationEventArgs Called with Url {0}", url);
            iOSBroker.SetBrokerResponse(url);
        }
    }
}
