// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Extensions.Msal.Providers.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// NoResourceUriInScopesException is thrown when the managed identity token provider does not find a .default
    /// scope for a resource in the enumeration of scopes.
    /// </summary>
    public class NoResourceUriInScopesException : MsalClientException
    {
        private const string Code = "no_resource_uri_with_slash_.default_in_scopes";
        private const string ErrorMessage = "The scopes provided is either empty or none that end in `/.default`.";

        /// <inheritdoc />
        /// <summary>
        /// Create a NoResourceUriInScopesException
        /// </summary>
        public NoResourceUriInScopesException() : base(Code, ErrorMessage) { }

        /// <summary>
        /// Create a NoResourceUriInScopesException with an error message
        /// </summary>
        public NoResourceUriInScopesException(string errorMessage) : base(Code, errorMessage) { }
    }
}
