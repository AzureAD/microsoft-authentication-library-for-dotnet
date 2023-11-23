// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Methods for long-running or background processes in web APIs.
    /// </summary>
    public interface ILongRunningWebApi
    {
        /// <summary>
        /// Acquires an access token for this web API from the authority configured in the application,
        /// in order to access another downstream protected web API on behalf of a user using the OAuth 2.0 On-Behalf-Of flow.
        /// See <see href="https://aka.ms/msal-net-long-running-obo">Long-running OBO in MSAL.NET</see>.
        /// Pass an access token (not an ID token) which was used to call this confidential client application in the
        /// <paramref name="userToken">userToken</paramref> parameter.
        /// Use <seealso cref="ConfidentialClientApplicationExtensions.StopLongRunningProcessInWebApiAsync"/> to stop the long running process
        /// and remove the associated tokens from the cache.
        /// </summary>
        /// <remarks>
        /// This method should be called once when the long-running session is started.
        /// </remarks>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <param name="userToken">A JSON Web Token which was used to call this web API and contains the credential information
        /// about the user on behalf of whom to get a token.</param>
        /// <param name="longRunningProcessSessionKey">Key by which to look up the token in the cache.
        /// If null, it will be set to the assertion hash of the <paramref name="userToken">userToken</paramref> by default.</param>
        /// <returns>A builder enabling you to add other parameters before executing the token request.</returns>
        AcquireTokenOnBehalfOfParameterBuilder InitiateLongRunningProcessInWebApi(IEnumerable<string> scopes, string userToken, ref string longRunningProcessSessionKey);

        /// <summary>
        /// Retrieves an access token from the cache using the provided cache key that can be used to
        /// access another downstream protected web API on behalf of a user using the OAuth 2.0 On-Behalf-Of flow.
        /// See <see href="https://aka.ms/msal-net-long-running-obo">Long-running OBO in MSAL.NET</see>.
        /// Use <seealso cref="ConfidentialClientApplicationExtensions.StopLongRunningProcessInWebApiAsync"/> to stop the long running process
        /// and remove the associated tokens from the cache.
        /// </summary>
        /// <remarks>
        /// This method should be called during the long-running session to retrieve the token from the cache.
        /// </remarks>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <param name="longRunningProcessSessionKey">Key by which to look up the token in the cache.</param>
        /// <returns>A builder enabling you to add other parameters before executing the token request.</returns>
        /// <exception cref="MsalClientException"> The token cache does not contain a token
        /// with an OBO cache key that matches the <paramref name="longRunningProcessSessionKey"/>.</exception>
        AcquireTokenOnBehalfOfParameterBuilder AcquireTokenInLongRunningProcess(IEnumerable<string> scopes, string longRunningProcessSessionKey);
    }
}
