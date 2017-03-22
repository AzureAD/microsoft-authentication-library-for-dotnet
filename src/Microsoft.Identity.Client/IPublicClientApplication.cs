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
    /// Component to be used for native applications (Desktop/UWP/iOS/Android).
    /// </summary>
    public interface IPublicClientApplication
    {
        #region Common application members

        /// <summary>
        /// Redirect Uri configured in the portal. Will have a default value. Not required if the developer is using the
        /// default client Id.
        /// </summary>
        string RedirectUri { get; set; }

        /// <summary>
        /// Gets or sets correlation Id which would be sent to the service with the next request.
        /// Correlation Id is to be used for diagnostics purposes.
        /// </summary>
        Guid CorrelationId { get; set; }

        /// <summary>
        /// Gets a value indicating whether address validation is ON or OFF.
        /// </summary>
        bool ValidateAuthority { get; }

        /// <summary>
        /// Returns a user-centric view over the cache that provides a list of all the available users in the cache.
        /// </summary>
        IEnumerable<User> Users { get; }

        /// <summary>
        /// Attempts to acquire the access token from cache. Access token is considered a match if it AT LEAST contains all the requested scopes.
        /// This means that an access token with more scopes than requested could be returned as well. If access token is expired or 
        /// close to expiration (within 5 minute window), then refresh token (if available) is used to acquire a new access token by making a network call.
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="user">User for which the token is requested. <see cref="User"/></param>
        Task<IAuthenticationResult> AcquireTokenSilentAsync(
            string[] scope,
            User user);

        /// <summary>
        /// Attempts to acquire the access token from cache. Access token is considered a match if it AT LEAST contains all the requested scopes.
        /// This means that an access token with more scopes than requested could be returned as well. If access token is expired or 
        /// close to expiration (within 5 minute window), then refresh token (if available) is used to acquire a new access token by making a network call.
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="user">User for which the token is requested <see cref="User"/></param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <param name="forceRefresh">If TRUE, API will ignore the access token in the cache and attempt to acquire new access token using the refresh token if available</param>
        Task<IAuthenticationResult> AcquireTokenSilentAsync(
            string[] scope,
            User user,
            string authority,
            bool forceRefresh);

        /// <summary>
        /// Removes any cached token for the specified user
        /// </summary>
        void Remove(User user);

        #endregion Common application members

        #region Public client-only members

        /// <summary>
        /// .NET specific property that allows configuration of platform specific properties. For example, in iOS/Android it
        /// would include the flag to enable/disable broker.
        /// </summary>
        IPlatformParameters PlatformParameters { get; }

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<IAuthenticationResult> AcquireTokenAsync(string[] scope);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<IAuthenticationResult> AcquireTokenAsync(
            string[] scope,
            string loginHint);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<IAuthenticationResult> AcquireTokenAsync(
            string[] scope,
            string loginHint,
            UIBehavior behavior,
            string extraQueryParameters);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="user">User object to enforce the same user to be authenticated in the web UI.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<IAuthenticationResult> AcquireTokenAsync(
            string[] scope,
            User user,
            UIBehavior behavior,
            string extraQueryParameters);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="additionalScope">Array of scopes for which a developer can request consent upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<IAuthenticationResult> AcquireTokenAsync(string[] scope,
            string loginHint,
            UIBehavior behavior,
            string extraQueryParameters,
            string[] additionalScope, string authority);

        /// <summary>
        /// Interactive request to acquire token. 
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="user">User object to enforce the same user to be authenticated in the web UI.</param>
        /// <param name="behavior">Enumeration to control UI behavior.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="additionalScope">Array of scopes for which a developer can request consent upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing token of the user</returns>
        Task<IAuthenticationResult> AcquireTokenAsync(
            string[] scope,
            User user,
            UIBehavior behavior,
            string extraQueryParameters,
            string[] additionalScope,
            string authority);

        #endregion
    }
}