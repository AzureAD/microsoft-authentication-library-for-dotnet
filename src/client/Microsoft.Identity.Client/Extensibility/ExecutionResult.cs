// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Represents the result of a token acquisition attempt.
    /// Used by the execution observer configured via <see cref="ConfidentialClientApplicationBuilderExtensions.WithObserver"/>.
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
}
}
