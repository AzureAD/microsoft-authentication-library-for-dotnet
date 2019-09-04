// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Extensions.Msal.Providers.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// NoProbesAvailableException is thrown when the chain of providers doesn't contain any token providers able to fetch a token
    /// </summary>
    public class NoProvidersAvailableException : MsalClientException
    {
        private const string Code = "no_providers_are_available";

        private const string ErrorMessage =
            "All of the ITokenProviders were unable to find the variables needed to successfully create a credential provider.";

        /// <inheritdoc />
        /// <summary>
        /// Create a NoProbesAvailableException
        /// </summary>
        public NoProvidersAvailableException() : base(Code, ErrorMessage)
        {
        }

        /// <summary>
        /// Create a NoProbesAvailableException with an error message
        /// </summary>
        public NoProvidersAvailableException(string errorMessage) : base(Code, errorMessage)
        {
        }
    }
}
