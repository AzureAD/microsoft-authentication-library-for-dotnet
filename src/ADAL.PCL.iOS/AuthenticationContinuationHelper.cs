//------------------------------------------------------------------------------
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
using Foundation;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Static class that consumes the response from the Authentication flow and continues token acquisition. This class should be called in ApplicationDelegate whenever app loads/reloads.
    /// </summary>
    public static class AuthenticationContinuationHelper
    {
        /// <summary>
        /// Returns if the response is from the broker app
        /// </summary>
        /// <param name="sourceApplication">application bundle id</param>
        /// <returns></returns>
        public static bool IsBrokerResponse(string sourceApplication)
        {
            return sourceApplication != null && sourceApplication.Equals("com.microsoft.azureauthenticator", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Sets broker response for continuing authentication flow.
        /// </summary>
        /// <param name="url"></param>
        public static void SetBrokerContinuationEventArgs(NSUrl url)
        {
            BrokerHelper.SetBrokerResponse(url);
        }

    }
}
