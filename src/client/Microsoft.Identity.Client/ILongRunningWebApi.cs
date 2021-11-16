// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Methods for long running processes in web APIs
    /// </summary>
    public interface ILongRunningWebApi
    {
        /// <summary>
        /// Acquires an access token for this web API from the authority configured in the application,
        /// in order to access another downstream protected web API on behalf of a user using the OAuth 2.0 On-Behalf-Of flow.
        /// See https://aka.ms/msal-net-long-running-obo .
        /// This confidential client application was itself called with a token which will be provided in the
        /// <paramref name="userToken">userToken</paramref> parameter.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="userToken">A JSON Web Token which was used to call the web API and contains the credential information
        /// about the user on behalf of whom to get a token.</param>
        /// <param name="longRunningProcessSessionKey">Key by which to look up the token in the cache.
        /// If null, it will be set to the assertion hash by default.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        AcquireTokenOnBehalfOfParameterBuilder InitiateLongRunningProcessInWebApi(IEnumerable<string> scopes, string userToken, ref string longRunningProcessSessionKey);

        /// <summary>
        /// Retrieves an access token from the cache using the provided cache key that can be used to
        /// access another downstream protected web API on behalf of a user using the OAuth 2.0 On-Behalf-Of flow.
        /// See https://aka.ms/msal-net-long-running-obo .
        /// </summary>
        /// <remarks>
        /// This method is intended to be used in the long running processes inside of web APIs.
        /// </remarks>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="longRunningProcessSessionKey">Key by which to look up the token in the cache</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <exception cref="MsalClientException"> is thrown if the token cache does not contain a token
        /// with an OBO cache key that matches the <paramref name="longRunningProcessSessionKey"/>.</exception>
        AcquireTokenOnBehalfOfParameterBuilder AcquireTokenInLongRunningProcess(IEnumerable<string> scopes, string longRunningProcessSessionKey);
    }
}
