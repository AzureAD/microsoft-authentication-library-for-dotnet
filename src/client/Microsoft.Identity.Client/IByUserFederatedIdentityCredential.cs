// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Provides an interface for acquiring tokens using the User Federated Identity Credential (UserFIC) flow.
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public interface IByUserFederatedIdentityCredential
    {
        /// <summary>
        /// Acquires a token on behalf of a user using a federated identity credential assertion.
        /// This uses the <c>user_fic</c> grant type.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <param name="username">The UPN (User Principal Name) of the user, e.g. <c>john.doe@contoso.com</c>.</param>
        /// <param name="assertionProvider">
        /// A delegate that returns the federated identity credential assertion (JWT) for the user.
        /// Use <see cref="FederatedCredentialProvider"/> to create common implementations.
        /// </param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request.</returns>
        AcquireTokenByUserFederatedIdentityCredentialParameterBuilder AcquireTokenByUserFederatedIdentityCredential(
            IEnumerable<string> scopes,
            string username,
            Func<AssertionRequestOptions, Task<string>> assertionProvider);
    }
}
