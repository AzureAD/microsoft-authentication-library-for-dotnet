// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
    /// <seealso cref="PublicClientApplication.AcquireTokenWithDeviceCode(IEnumerable{string}, Func{DeviceCodeResult, System.Threading.Tasks.Task})"> and
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
