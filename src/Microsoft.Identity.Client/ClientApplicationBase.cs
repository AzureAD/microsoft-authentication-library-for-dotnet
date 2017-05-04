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
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Instance;
using Microsoft.Identity.Client.Internal.Requests;
using System.Linq;
using Microsoft.Identity.Client.Internal.Telemetry;

namespace Microsoft.Identity.Client
{
    /// <Summary>
    /// Abstract class containing common API methods and properties. Both PublicClientApplication and ConfidentialClientApplication extend this class.
    /// </Summary>
    public abstract class ClientApplicationBase
    {
        private TokenCache _userTokenCache;

        /// <Summary>
        /// Default Authority used for interactive calls.
        /// </Summary>
        protected const string DefaultAuthority = "https://login.microsoftonline.com/common/";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="authority"></param>
        /// <param name="redirectUri"></param>
        /// <param name="validateAuthority"></param>
        protected ClientApplicationBase(string clientId, string authority, string redirectUri,
            bool validateAuthority)
        {
            ClientId = clientId;
            Authority authorityInstance = Internal.Instance.Authority.CreateAuthority(authority, validateAuthority);
            Authority = authorityInstance.CanonicalAuthority;
            RedirectUri = redirectUri;
            ValidateAuthority = validateAuthority;
            if (UserTokenCache != null)
            {
                UserTokenCache.ClientId = clientId;
            }

            RequestContext requestContext = new RequestContext(Guid.Empty, null);
            requestContext.Logger.Info(string.Format(CultureInfo.InvariantCulture,
                "MSAL {0} with assembly version '{1}', file version '{2}' and informational version '{3}' is running...",
                PlatformPlugin.PlatformInformation.GetProductName(), MsalIdHelper.GetMsalVersion(),
                MsalIdHelper.GetAssemblyFileVersion(), MsalIdHelper.GetAssemblyInformationalVersion()));
        }

        /// <summary>
        /// Identifier of the component consuming MSAL and it is intended for libraries/SDKs that consume MSAL. This will allow for disambiguation between MSAL usage by the app vs MSAL usage by component libraries.
        /// </summary>
        public string Component { get; set; }

        /// <Summary>
        /// Authority provided by the developer or default authority used by the library.
        /// </Summary>
        public string Authority { get; }

        /// <summary>
        /// Will be a default value. Can be overridden by the developer. Once set, application will bind to the client Id.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Redirect Uri configured in the app registration portal. PublicClientApplication has a default value of 
        /// urn:ietf:wg:oauth:2.0:oob.This default does not apply to iOS and Android as the library needs to leverage 
        /// system webview for authentication.
        /// </summary>
        public string RedirectUri { get; set; }

        /// <summary>
        /// Sets or Gets the custom query parameters that may be sent to the STS for dogfood testing. This parameter should not be set by the 
        /// developers as it may have adverse effect on the application.
        /// </summary>
        public string SliceParameters { get; set; }

        /// <Summary>
        /// Token Cache instance for storing User tokens.
        /// </Summary>
        internal TokenCache UserTokenCache
        {
            get { return _userTokenCache; }
            set
            {
                _userTokenCache = value;
                if (_userTokenCache != null)
                {
                    _userTokenCache.ClientId = ClientId;
                }
            }
        }

        /// <summary>
        /// Gets/sets a value indicating whether authority validation is ON or OFF. Value is true by default. 
        /// It should be set to false by the deveopers for B2C applications.
        /// </summary>
        public bool ValidateAuthority { get; set; }

        /// <summary>
        /// Returns a User centric view over the cache that provides a list of all the available users in the cache for the application.
        /// </summary>
        public IEnumerable<IUser> Users
        {
            get
            {
                RequestContext requestContext = new RequestContext(Guid.Empty, null);
                if (UserTokenCache == null)
                {
                    requestContext.Logger.Info("Token cache is null or empty. Returning empty list of users.");
                    return Enumerable.Empty<User>();
                }

                return UserTokenCache.GetUsers(new Uri(Authority).Host, requestContext);
            }
        }

        /// <summary>
        /// Get user by identifier from users available in the cache.
        /// </summary>
        /// <param name="identifier">user identifier</param>
        public IUser GetUser(string identifier)
        {
            return Users.FirstOrDefault(user => user.Identifier.Equals(identifier));
        }

        /// <summary>
        /// Attempts to acquire the access token from cache. Access token is considered a match if it AT LEAST contains all the requested scopes.
        /// This means that an access token with more scopes than requested could be returned as well. If access token is expired or 
        /// close to expiration (within 5 minute window), then refresh token (if available) is used to acquire a new access token by making a network call.
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="user">User for which the token is requested. <see cref="IUser"/></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(IEnumerable<string> scopes, IUser user)
        {
            return
                await
                    AcquireTokenSilentCommonAsync(null, scopes, user, false, ApiEvent.ApiIds.AcquireTokenSilentWithoutAuthority)
                        .ConfigureAwait(false);
        }

        /// <summary>
        /// Attempts to acquire the access token from cache. Access token is considered a match if it AT LEAST contains all the requested scopes.
        /// This means that an access token with more scopes than requested could be returned as well. If access token is expired or 
        /// close to expiration (within 5 minute window), then refresh token (if available) is used to acquire a new access token by making a network call.
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="user">User for which the token is requested <see cref="User"/></param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <param name="forceRefresh">If TRUE, API will ignore the access token in the cache and attempt to acquire new access token using the refresh token if available</param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(IEnumerable<string> scopes, IUser user,
            string authority, bool forceRefresh)
        {
            Authority authorityInstance = null;
            if (!string.IsNullOrEmpty(authority))
            {
                authorityInstance = Internal.Instance.Authority.CreateAuthority(authority, ValidateAuthority);
            }

            return
                await
                    AcquireTokenSilentCommonAsync(authorityInstance, scopes, user,
                        forceRefresh, ApiEvent.ApiIds.AcquireTokenSilentWithAuthority).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes all cached tokens for the specified user.
        /// </summary>
        /// <param name="user">instance of the user that needs to be removed</param>
        public void Remove(IUser user)
        {
            RequestContext requestContext = CreateRequestContext(Guid.Empty);
            if (user == null || UserTokenCache == null)
            {
                return;
            }

            UserTokenCache.Remove(user, requestContext);
        }

        internal async Task<AuthenticationResult> AcquireTokenSilentCommonAsync(Authority authority,
            IEnumerable<string> scopes, IUser user, bool forceRefresh, ApiEvent.ApiIds apiId)
        {
            var handler = new SilentRequest(
                CreateRequestParameters(authority, scopes, user, UserTokenCache),
                forceRefresh)
            { ApiId = apiId };
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal virtual AuthenticationRequestParameters CreateRequestParameters(Authority authority,
            IEnumerable<string> scopes,
            IUser user, TokenCache cache)
        {
            return new AuthenticationRequestParameters
            {
                SliceParameters = SliceParameters,
                Authority = authority,
                ClientId =  ClientId,
                TokenCache = cache,
                User = user,
                Scope = scopes.CreateSetFromEnumerable(),
                RedirectUri = new Uri(RedirectUri),
                RequestContext = CreateRequestContext(Guid.Empty),
                ValidateAuthority = ValidateAuthority
            };
        }

        internal RequestContext CreateRequestContext(Guid correlationId)
        {
            correlationId = (correlationId != Guid.Empty) ? correlationId : Guid.NewGuid();
            return new RequestContext(correlationId, Component);
        }
    }
}