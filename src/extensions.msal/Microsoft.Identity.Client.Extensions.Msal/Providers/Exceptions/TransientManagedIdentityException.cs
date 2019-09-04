// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Extensions.Msal.Providers.Exceptions
{
    /// <inheritdoc />
    ///  <summary>
    ///  TransientManagedIdentityException occurs when a 404, 429 or a 500 series error is encountered.
    ///  see: https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/how-to-use-vm-token#error-handling
    ///  </summary>
    public class TransientManagedIdentityException : MsalClientException
    {
        private const string Code = "transient_managed_identity_error";
        private const string ErrorMessage = "encountered a transient error response from the managed identity service";

        /// <summary>
        /// Create a TransientManagedIdentityException
        /// </summary>
        public TransientManagedIdentityException() : base(Code, ErrorMessage) { }

        /// <inheritdoc />
        /// <summary>
        /// Create a TransientManagedIdentityException with an error message
        /// </summary>
        public TransientManagedIdentityException(string errorMessage) : base(Code, errorMessage) { }
    }
}
