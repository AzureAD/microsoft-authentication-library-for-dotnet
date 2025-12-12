// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.Extensibility
{
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    /// <summary>
    /// Represents the result of a token acquisition attempt.
    /// Used by the execution observer configured via <see cref="ConfidentialClientApplicationBuilderExtensions.OnCompletion"/>.
    /// </summary>
    public class ExecutionResult
    {
        /// <summary>
        /// Internal constructor for ExecutionResult.
        /// </summary>
        internal ExecutionResult() { }

        /// <summary>
        /// Indicates whether the token acquisition was successful.
        /// </summary>
        /// <value>
        /// <c>true</c> if the token was successfully acquired; otherwise, <c>false</c>.
        /// </value>
        public bool Successful { get; internal set; }

        /// <summary>
        /// The authentication result if the token acquisition was successful.
        /// </summary>
        /// <value>
        /// An <see cref="AuthenticationResult"/> containing the access token and related metadata if <see cref="Successful"/> is <c>true</c>;
        /// otherwise, <c>null</c>.
        /// </value>
        public AuthenticationResult Result { get; internal set; }

        /// <summary>
        /// The exception that occurred if the token acquisition failed.
        /// </summary>
        /// <value>
        /// An <see cref="MsalException"/> describing the failure if <see cref="Successful"/> is <c>false</c>;
        /// otherwise, <c>null</c>.
        /// </value>
        public MsalException Exception { get; internal set; }

        /// <summary>
        /// The certificate used for authentication, if certificate-based authentication was used.
        /// </summary>
        /// <value>
        /// An <see cref="X509Certificate2"/> used to authenticate the client application;
        /// otherwise, <c>null</c> if certificate authentication was not used or if the certificate is not available.
        /// </value>
        /// <remarks>
        /// This property provides access to the certificate for logging and auditing purposes.
        /// The certificate may be disposed after the token acquisition completes, so accessing its properties
        /// may throw exceptions if the certificate has been disposed.
        /// </remarks>
        public X509Certificate2 Certificate { get; internal set; }
    }
}
