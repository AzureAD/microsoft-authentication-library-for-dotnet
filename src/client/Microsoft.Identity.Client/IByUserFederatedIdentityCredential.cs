// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Provides methods to acquire tokens using User Federated Identity Credential (UserFIC) flow.
    /// </summary>
    public interface IByUserFederatedIdentityCredential
    {
        /// <summary>
        /// Acquires a user-scoped token using federated identity credential assertion instead of a password.
        /// This method does not look in the token cache, but stores the result in it. Before calling this method, use
        /// <see cref="IClientApplicationBase.AcquireTokenSilent(IEnumerable{string}, IAccount)"/> to check the token cache.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <param name="username">Identifier of the user, application requests token on behalf of.
        /// Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="assertionCallback">
        /// Callback invoked to provide the federated identity credential assertion token.
        /// The callback is only invoked when acquiring new tokens (not from cache).
        /// </param>
        /// <param name="tokenExchangeScope">
        /// Scope for the assertion token. Defaults to <c>"api://AzureADTokenExchange/.default"</c> for public cloud.
        /// Use <c>"api://AzureADTokenExchangeUSGov/.default"</c> for US Gov,
        /// or <c>"api://AzureADTokenExchangeChina/.default"</c> for China.
        /// </param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request.</returns>
        AcquireTokenByUserFederatedIdentityCredentialParameterBuilder AcquireTokenByUserFederatedIdentityCredential(
            IEnumerable<string> scopes,
            string username,
            Func<Task<string>> assertionCallback,
            string tokenExchangeScope = "api://AzureADTokenExchange/.default");
    }
}
