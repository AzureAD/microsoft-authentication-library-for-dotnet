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
    /// Component containing common validation methods
    /// </summary>
    public interface IClientApplicationBase
    {
        /// <summary>
        /// Identifier of the component consuming MSAL and it is intended for libraries/SDKs that consume MSAL. This will allow for 
        /// disambiguation between MSAL usage by the app vs MSAL usage by component libraries.
        /// </summary>
        string Component { get; set; }

        /// <Summary>
        /// Authority provided by the developer or default authority used by the library.
        /// </Summary>
        string Authority { get; }

        /// <summary>
        /// Will be a default value. Can be overridden by the developer. Once set, application will bind to the client Id.
        /// </summary>
        string ClientId { get; }

        /// <summary>
        /// Redirect Uri configured in the portal. Will have a default value. Not required if the developer is using the
        /// default client Id.
        /// </summary>
        string RedirectUri { get; set; }

        /// <summary>
        /// Gets a value indicating whether address validation is ON or OFF.
        /// </summary>
        bool ValidateAuthority { get; }

        /// <summary>
        /// Returns a user-centric view over the cache that provides a list of all the available users in the cache.
        /// </summary>
        IEnumerable<IUser> Users { get; }

        /// <summary>
        /// Sets or Gets the custom query parameters that may be sent to the STS for dogfood testing. This parameter should not be set by the 
        /// developers as it may have adverse effect on the application.
        /// </summary>
        string SliceParameters { get; set; }

        /// <summary>
        /// Get user by identifier from users available in the cache.
        /// </summary>
        /// <param name="identifier">user identifier</param>
        IUser GetUser(string identifier);

        /// <summary>
        /// Attempts to acquire the access token from cache. Access token is considered a match if it AT LEAST contains all the requested scopes.
        /// This means that an access token with more scopes than requested could be returned as well. If access token is expired or 
        /// close to expiration (within 5 minute window), then refresh token (if available) is used to acquire a new access token by making a network call.
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="user">User for which the token is requested. <see cref="IUser"/></param>
        Task<AuthenticationResult> AcquireTokenSilentAsync(
            IEnumerable<string> scopes,
            IUser user);

        /// <summary>
        /// Attempts to acquire the access token from cache. Access token is considered a match if it AT LEAST contains all the requested scopes.
        /// This means that an access token with more scopes than requested could be returned as well. If access token is expired or 
        /// close to expiration (within 5 minute window), then refresh token (if available) is used to acquire a new access token by making a network call.
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="user">User for which the token is requested <see cref="IUser"/></param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <param name="forceRefresh">If TRUE, API will ignore the access token in the cache and attempt to acquire new access token using the refresh token if available</param>
        Task<AuthenticationResult> AcquireTokenSilentAsync(
            IEnumerable<string> scopes,
            IUser user,
            string authority,
            bool forceRefresh);

        /// <summary>
        /// Removes all cached tokens for the specified user.
        /// </summary>
        /// <param name="user">instance of the user that needs to be removed</param>
        void Remove(IUser user);
   }
}
