// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Extensions.Msal.Providers.Exceptions
{
    /// <inheritdoc />
    ///  <summary>
    ///  BadManagedIdentityException occurs when a 400 is returned from the managed identity service
    ///  see: https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/how-to-use-vm-token#error-handling
    ///  </summary>
    public class BadRequestManagedIdentityException : MsalServiceException
    {
        private const string Code = "bad_managed_identity_error";
        private const string ErrorMessage = "invalid resource; the application was not found in the tenant.";

        /// <inheritdoc />
        /// <summary>
        /// Create a BadManagedIdentityException
        /// </summary>
        public BadRequestManagedIdentityException() : base(Code, ErrorMessage) { }

        /// <summary>
        /// Create a BadManagedIdentityException with an error message
        /// </summary>
        /// <param name="errorMessage">exception error message</param>
        public BadRequestManagedIdentityException(string errorMessage) : base(Code, errorMessage) { }
    }
}
