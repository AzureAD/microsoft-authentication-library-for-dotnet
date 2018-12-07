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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// This object is returned as part of the device code flow
    /// and has information intended to be shown to the user about
    /// where to navigate to login and what the device code needs
    /// to be entered on that device.
    /// See https://aka.ms/msal-device-code-flow.
    /// </summary>
    /// <seealso cref="PublicClientApplication.AcquireTokenWithDeviceCodeAsync(IEnumerable{string}, Func{DeviceCodeResult, System.Threading.Tasks.Task})"> and
    /// the other overrides
    /// </seealso>
    public class DeviceCodeResult
    {
        internal DeviceCodeResult(
            string userCode,
            string deviceCode,
            string verificationUrl,
            DateTimeOffset expiresOn,
            long interval,
            string message,
            string clientId,
            ISet<string> scopes)
        {
            UserCode = userCode;
            DeviceCode = deviceCode;
            VerificationUrl = verificationUrl;
            ExpiresOn = expiresOn;
            Interval = interval;
            Message = message;
            ClientId = clientId;
            Scopes = new ReadOnlyCollection<string>(scopes.AsEnumerable().ToList());
        }

        /// <summary>
        /// User code returned by the service
        /// </summary>
        public string UserCode { get; }

        /// <summary>
        /// Device code returned by the service
        /// </summary>
        public string DeviceCode { get; }

        /// <summary>
        /// Verification URL where the user must navigate to authenticate using the device code and credentials.
        /// </summary>
        public string VerificationUrl { get; }

        /// <summary>
        /// Time when the device code will expire.
        /// </summary>
        public DateTimeOffset ExpiresOn { get; }

        /// <summary>
        /// Polling interval time to check for completion of authentication flow.
        /// </summary>
        public long Interval { get; }

        /// <summary>
        /// User friendly text response that can be used for display purpose.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Identifier of the client requesting device code.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// List of the scopes that would be held by token.
        /// </summary>
        public IReadOnlyCollection<string> Scopes { get; }
    }
}
