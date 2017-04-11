//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Component to be used for confidential client applications like Web Apps/API.
    /// </summary>
    public interface IConfidentialClientApplication : IClientApplicationBase
    {
        /// <summary>
        /// Acquires token using On-Behalf-Of flow.
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="userAssertion">Instance of UserAssertion containing user's token.</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(
            IEnumerable<string> scope,
            UserAssertion userAssertion);

        /// <summary>
        /// Acquires token using On-Behalf-Of flow.
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="userAssertion">Instance of UserAssertion containing user's token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(
            IEnumerable<string> scope,
            UserAssertion userAssertion,
            string authority);

        /// <summary>
        /// Acquires security token from the authority using authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="IClientApplicationBase.AcquireTokenSilentAsync(System.Collections.Generic.IEnumerable{string}, User)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(
            string authorizationCode,
            IEnumerable<string> scope);

        /// <summary>
        /// Acquires token from the service for the confidential client. This method attempts to look up valid access token in the cache.
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <returns>Authentication result containing application token for the requested scopes</returns>
        Task<AuthenticationResult> AcquireTokenForClientAsync(
            IEnumerable<string> scope);

        /// <summary>
        /// Acquires token from the service for the confidential client. This method attempts to look up valid access token in the cache.
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="forceRefresh">If TRUE, API will ignore the access token in the cache and attempt to acquire new access token using client credentials</param>
        /// <returns>Authentication result containing application token for the requested scopes</returns>
        Task<AuthenticationResult> AcquireTokenForClientAsync(
            IEnumerable<string> scope,
            bool forceRefresh);

        /// <summary>
        /// Gets URL of the authorize endpoint including the query parameters.
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        Task<Uri> GetAuthorizationRequestUrlAsync(
            IEnumerable<string> scope,
            string loginHint,
            string extraQueryParameters);

        /// <summary>
        /// Gets URL of the authorize endpoint including the query parameters.
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="additionalScope">Array of scopes for which a developer can request consent upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        Task<Uri> GetAuthorizationRequestUrlAsync(
            IEnumerable<string> scope,
            string redirectUri,
            string loginHint,
            string extraQueryParameters, IEnumerable<string> additionalScope, string authority);
    }
}