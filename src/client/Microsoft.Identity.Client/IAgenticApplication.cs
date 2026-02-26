// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Represents an agentic application that can acquire tokens on behalf of an agent identity.
    /// Supports both app-only (agent credential) and user-delegated (agent on behalf of user) token acquisition flows.
    /// </summary>
    /// <remarks>
    /// Agentic applications are used in scenarios where an AI agent or service needs to authenticate
    /// as itself or on behalf of a user using Federated Managed Identity (FMI) credentials.
    /// Use <see cref="AgenticApplicationBuilder"/> to create instances of this interface.
    ///
    /// <para>For app-only tokens, use <see cref="AcquireTokenForAgent"/>.</para>
    /// <para>For user-delegated tokens, use <see cref="AcquireTokenForAgentOnBehalfOfUser"/>.</para>
    /// <para>For cached tokens, use <see cref="AcquireTokenSilent"/> with an account from a previous result.</para>
    /// </remarks>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide on mobile
#endif
    public interface IAgenticApplication
    {
        /// <summary>
        /// Acquires an app-only token for the agent identity.
        /// Internally handles obtaining the FMI credential from the platform and using it as a client assertion
        /// to acquire a token for the requested scopes.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API. For example,
        /// <c>https://graph.microsoft.com/.default</c> or <c>https://myapi.contoso.com/.default</c>.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request.</returns>
        /// <remarks>
        /// You can also chain the following optional parameters:
        /// <see cref="AcquireTokenForAgentParameterBuilder.WithForceRefresh(bool)"/>
        /// <see cref="AcquireTokenForAgentParameterBuilder.WithCorrelationId(System.Guid)"/>
        /// </remarks>
        AcquireTokenForAgentParameterBuilder AcquireTokenForAgent(IEnumerable<string> scopes);

        /// <summary>
        /// Acquires a user-delegated token for the agent, acting on behalf of the specified user.
        /// Internally handles obtaining the FMI credential, the user federated identity credential (User FIC),
        /// and exchanging it for a user-delegated token via the user_fic grant type.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API. For example,
        /// <c>https://graph.microsoft.com/.default</c> or <c>https://myapi.contoso.com/.default</c>.</param>
        /// <param name="userPrincipalName">The UPN (User Principal Name) of the user on whose behalf the agent is acting.
        /// For example, <c>user@contoso.com</c>.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request.</returns>
        /// <remarks>
        /// You can also chain the following optional parameters:
        /// <see cref="AcquireTokenForAgentOnBehalfOfUserParameterBuilder.WithForceRefresh(bool)"/>
        /// <see cref="AcquireTokenForAgentOnBehalfOfUserParameterBuilder.WithCorrelationId(System.Guid)"/>
        /// </remarks>
        AcquireTokenForAgentOnBehalfOfUserParameterBuilder AcquireTokenForAgentOnBehalfOfUser(
            IEnumerable<string> scopes, string userPrincipalName);

        /// <summary>
        /// Gets an account by its identifier, for use with silent token acquisition.
        /// </summary>
        /// <param name="accountIdentifier">Account identifier, typically obtained from a previous authentication result
        /// via <see cref="AuthenticationResult.Account"/> and <see cref="IAccount.HomeAccountId"/>.</param>
        /// <returns>The account, or null if not found in the cache.</returns>
        Task<IAccount> GetAccountAsync(string accountIdentifier);

        /// <summary>
        /// Acquires a token silently from the cache for the specified account.
        /// Use this after a successful call to <see cref="AcquireTokenForAgentOnBehalfOfUser"/> to retrieve cached tokens
        /// without making a network request.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <param name="account">The account for which to acquire the token, typically obtained
        /// from a previous <see cref="AuthenticationResult.Account"/>.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request.</returns>
        AcquireTokenSilentParameterBuilder AcquireTokenSilent(IEnumerable<string> scopes, IAccount account);
    }
}
