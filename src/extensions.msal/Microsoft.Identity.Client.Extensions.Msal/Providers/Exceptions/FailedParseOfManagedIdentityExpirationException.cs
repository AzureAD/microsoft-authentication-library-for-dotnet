// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Extensions.Msal.Providers.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// FailedParseOfManagedIdentityExpirationException is thrown when the managed identity service returns a token
    /// with an expiration we are unable to parse.
    /// </summary>
    public class FailedParseOfManagedIdentityExpirationException : MsalClientException
    {
        private const string Code = "failed_parse_of_managed_identity_token_expiry";
        private const string ErrorMessage = "managed identity service returned a response with an expiration we are unable to parse.";

        /// <inheritdoc />
        /// <summary>
        /// Create a FailedParseOfManagedIdentityExpirationException
        /// </summary>
        public FailedParseOfManagedIdentityExpirationException() : base(Code, ErrorMessage) { }

        /// <summary>
        /// Create a FailedParseOfManagedIdentityExpirationException with an error message
        /// </summary>
        public FailedParseOfManagedIdentityExpirationException(string errorMessage) : base(Code, errorMessage) { }
    }
}
