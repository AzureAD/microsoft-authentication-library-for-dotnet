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
        /// Returns if the response is from the broker app
        /// </summary>
        /// <param name="sourceApplication">application bundle id of the broker</param>
        /// <returns>True if the response is from broker, False otherwise.</returns>
        public static bool IsBrokerResponse(string sourceApplication)
        {
            Debug.WriteLine("IsBrokerResponse Called with sourceApplication {0}", sourceApplication);
            return sourceApplication != null && sourceApplication.Equals("com.microsoft.azureauthenticator", StringComparison.OrdinalIgnoreCase);
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