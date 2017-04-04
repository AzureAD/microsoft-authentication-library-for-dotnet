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

namespace Microsoft.Identity.Client
{
    /// <Summary>
    /// Abstract class containing common API methods and properties. Both PublicClientApplication and ConfidentialClientApplication extend this class.
    /// </Summary>
    public abstract class ClientApplicationBase
    {
        /// <Summary>
        /// DefaultAuthority
        /// </Summary>
        protected const string DefaultAuthority = "https://login.microsoftonline.com/common/";
        
        /// <Summary>
        /// ClientApplicationBase
        /// </Summary>
        protected ClientApplicationBase(string authority, string clientId, string redirectUri,
            bool validateAuthority)
        {
            Authority authorityInstance = Internal.Instance.Authority.CreateAuthority(authority, validateAuthority);
            Authority = authorityInstance.CanonicalAuthority;
            ClientId = clientId;
            RedirectUri = redirectUri;
            ValidateAuthority = validateAuthority;
            if (UserTokenCache != null)
            {
                UserTokenCache.ClientId = clientId;
            }

            RequestContext requestContext = new RequestContext(Guid.Empty);

            requestContext.Logger.Info(string.Format(CultureInfo.InvariantCulture,
                "MSAL {0} with assembly version '{1}', file version '{2}' and informational version '{3}' is running...",
                PlatformPlugin.PlatformInformation.GetProductName(), MsalIdHelper.GetMsalVersion(),
                MsalIdHelper.GetAssemblyFileVersion(), MsalIdHelper.GetAssemblyInformationalVersion()));
        }

        /// <Summary>
        /// Authority provided by the developer
        /// </Summary>
        internal string Authority { get; }

        /// <summary>
        /// Will be a default value. Can be overridden by the developer. Once set, application will bind to the client Id.
        /// </summary>
        internal string ClientId { get; }

        /// <summary>
        /// Redirect Uri configured in the portal. Will have a default value. Not required, if the developer is using the
        /// default client Id.
        /// </summary>
        public string RedirectUri { get; set; }

        /// <Summary>
        /// Token Cache instance for storing User tokens.
        /// </Summary>
        internal TokenCache UserTokenCache { get; set; }

        /// <summary>
        /// Gets or sets correlation Id which would be sent to the service with the next request.
        /// Correlation Id is to be used for diagnostics purposes.
        /// </summary>
        public Guid CorrelationId { get; set; }
        
        /// <summary>
        /// Gets a value indicating whether address validation is ON or OFF.
        /// </summary>
        public bool ValidateAuthority { get; set; }

        /// <summary>
        /// Returns a User centric view over the cache that provides a list of all the available users in the cache.
        /// </summary>
        public IEnumerable<IUser> Users
        {
            get
            {
                if (UserTokenCache == null)
                {
                    RequestContext requestContext = new RequestContext(CorrelationId);
                    requestContext.Logger.Info("Token cache is null or empty");
                    return Enumerable.Empty<User>();
                }

                return UserTokenCache.GetUsers(new Uri(Authority).Host);
            }
        }

        /// <summary>
        /// Attempts to acquire the access token from cache. Access token is considered a match if it AT LEAST contains all the requested scopes.
        /// This means that an access token with more scopes than requested could be returned as well. If access token is expired or 
        /// close to expiration (within 5 minute window), then refresh token (if available) is used to acquire a new access token by making a network call.
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="user">User for which the token is requested. <see cref="User"/></param>
        /// <returns></returns>
        public async Task<IAuthenticationResult> AcquireTokenSilentAsync(IEnumerable<string> scope, IUser user)
        {
            Authority authority = Internal.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            return
                await
                    AcquireTokenSilentCommonAsync(authority, scope, user, false)
                        .ConfigureAwait(false);
        }

        /// <summary>
        /// Attempts to acquire the access token from cache. Access token is considered a match if it AT LEAST contains all the requested scopes.
        /// This means that an access token with more scopes than requested could be returned as well. If access token is expired or 
        /// close to expiration (within 5 minute window), then refresh token (if available) is used to acquire a new access token by making a network call.
        /// </summary>
        /// <param name="scope">Array of scopes requested for resource</param>
        /// <param name="user">User for which the token is requested <see cref="User"/></param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <param name="forceRefresh">If TRUE, API will ignore the access token in the cache and attempt to acquire new access token using the refresh token if available</param>
        /// <returns></returns>
        public async Task<IAuthenticationResult> AcquireTokenSilentAsync(IEnumerable<string> scope, IUser user,
            string authority, bool forceRefresh)
        {
            Authority authorityInstance = Internal.Instance.Authority.CreateAuthority(authority, ValidateAuthority);
            return
                await
                    AcquireTokenSilentCommonAsync(authorityInstance, scope, user,
                        forceRefresh).ConfigureAwait(false);
        }
        
        /// <summary>
        /// </summary>
        public void Remove(IUser user)
        {
            if(user == null || UserTokenCache == null)
            {
                return;
            }

            UserTokenCache.Remove(user);
        }

        internal async Task<AuthenticationResult> AcquireTokenSilentCommonAsync(Authority authority,
            IEnumerable<string> scope, IUser user, bool forceRefresh)
        {
            var handler = new SilentRequest(
                CreateRequestParameters(authority, scope, user, UserTokenCache),
                forceRefresh);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal virtual AuthenticationRequestParameters CreateRequestParameters(Authority authority, IEnumerable<string> scope,
            IUser user, TokenCache cache)
        {
            return new AuthenticationRequestParameters
            {
                Authority = authority,
                TokenCache = cache,
                User = user,
                Scope = scope.CreateSetFromEnumerable(),
                RedirectUri = new Uri(RedirectUri),
                RequestContext = CreateRequestContext(CorrelationId)
            };
        }

        internal RequestContext CreateRequestContext(Guid correlationId)
        {
            correlationId = (correlationId != Guid.Empty) ? correlationId : Guid.NewGuid();
            return new RequestContext(correlationId);
        }
    }
}