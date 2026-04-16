// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Represents the outcome of a token acquisition operation.
    /// Carries either a successful <see cref="AuthenticationResult"/> or the
    /// <see cref="Exception"/> that caused the request to fail — never both at the same time.
    /// </summary>
    public sealed class TokenAcquisitionResult
    {
        /// <summary>
        /// The authentication result. Non-null on success, null on failure.
        /// </summary>
        public AuthenticationResult AuthenticationResult { get; internal set; }

        /// <summary>
        /// The exception that caused the request to fail. Non-null on failure, null on success.
        /// </summary>
        public Exception Exception { get; internal set; }

        /// <summary>
        /// <c>true</c> when <see cref="AuthenticationResult"/> is set (successful request).
        /// </summary>
        public bool IsSuccess => AuthenticationResult != null;
    }
}
