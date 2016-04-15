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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// This class represents the response from the service when requesting device code.
    /// </summary>
    public sealed class DeviceCodeResult
    {
        internal DeviceCodeResult()
        {
        }

        /// <summary>
        /// User code returned by the service
        /// </summary>
        public string UserCode { get; internal set; }

        /// <summary>
        /// Device code returned by the service
        /// </summary>
        public string DeviceCode { get; internal set; }

        /// <summary>
        /// Verification URL where the user must navigate to authenticate using the device code and credentials.
        /// </summary>
        public string VerificationUrl { get; internal set; }

        /// <summary>
        /// Time when the device code will expire.
        /// </summary>
        public DateTimeOffset ExpiresOn { get; internal set; }

        /// <summary>
        /// Polling interval time to check for completion of authentication flow.
        /// </summary>
        public long Interval { get; internal set; }

        /// <summary>
        /// User friendly text response that can be used for display purpose.
        /// </summary>
        public string Message { get; internal set; }

        /// <summary>
        /// Identifier of the client requesting device code.
        /// </summary>
        public string ClientId { get; internal set; }

        /// <summary>
        /// Identifier of the target resource that would be the recipient of the token.
        /// </summary>
        public string Resource { get; internal set; }
    }
}
