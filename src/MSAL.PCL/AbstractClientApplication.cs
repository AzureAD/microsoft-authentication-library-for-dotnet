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
using Microsoft.Identity.Client.Handlers;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    public abstract class AbstractClientApplication
    {
        protected const string DefaultAuthority = "https://login.microsoftonline.com/common/";

        /// <summary>
        /// default false.
        /// </summary>
        public bool RestrictToSingleUser { get; set; }

        public string Authority { get; private set; }

        /// <summary>
        /// Will be a default value. Can be overriden by the developer. Once set, application will bind to the client Id.
        /// </summary>
        public string ClientId { get;  set; }

        /// <summary>
        /// Redirect Uri configured in the portal. Will have a default value. Not required, if the developer is using the default client Id.
        /// </summary>
        public string RedirectUri { get; set; }

        public TokenCache UserTokenCache { get; set; }

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
        /// .NET specific property that allows configuration of platform specific properties. For example, in iOS/Android it would include the flag to enable/disable broker.
        /// </summary>
        public IPlatformParameters PlatformParameters { get; set; }

        /// <summary>
        /// Returns a User centric view over the cache that provides a list of all the signed in users.
        /// </summary>
        public IEnumerable<User> Users
        {
            get
            {
                if (this.UserTokenCache == null || this.UserTokenCache.Count == 0)
                {
                    PlatformPlugin.Logger.Information(null, "Token cache is null or empty");
                    return new List<User>();
                }

                return this.UserTokenCache.GetUsers(this.ClientId);
            }
        }

        static AbstractClientApplication()
        {
            PlatformPlugin.Logger.Information(null, string.Format("MSAL {0} with assembly version '{1}', file version '{2}' and informational version '{3}' is running...",
                PlatformPlugin.PlatformInformation.GetProductName(), MsalIdHelper.GetMsalVersion(), MsalIdHelper.GetAssemblyFileVersion(), MsalIdHelper.GetAssemblyInformationalVersion()));
        }

        protected AbstractClientApplication(string authority, string clientId, string redirectUri, bool validateAuthority)
        {
            this.Authority = authority;
            this.ClientId = clientId;
            this.RedirectUri = redirectUri;
            this.ValidateAuthority = validateAuthority;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope)
        {
            Authenticator authenticator = new Authenticator(this.Authority, this.ValidateAuthority, this.CorrelationId);
            return await this.AcquireTokenSilentCommonAsync(authenticator, scope, (string)null, this.PlatformParameters, null, false).ConfigureAwait(false);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope, User user)
        {
            Authenticator authenticator = new Authenticator(this.Authority, this.ValidateAuthority, this.CorrelationId);
            return await this.AcquireTokenSilentCommonAsync(authenticator, scope, user, this.PlatformParameters, null, false).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="userIdentifier"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope, string userIdentifier)
        {
            Authenticator authenticator = new Authenticator(this.Authority, this.ValidateAuthority, this.CorrelationId);
            return await this.AcquireTokenSilentCommonAsync(authenticator, scope, userIdentifier, this.PlatformParameters, null, false).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="userIdentifier"></param>
        /// <param name="authority"></param>
        /// <param name="policy"></param>
        /// <param name="forceRefresh"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope, string userIdentifier,
            string authority, string policy, bool forceRefresh)
        {
            Authenticator authenticator = new Authenticator(authority, this.ValidateAuthority, this.CorrelationId);
            return await this.AcquireTokenSilentCommonAsync(authenticator, scope, userIdentifier, this.PlatformParameters, policy, forceRefresh).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="user"></param>
        /// <param name="authority"></param>
        /// <param name="policy"></param>
        /// <param name="forceRefresh"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scope, User user,
            string authority, string policy, bool forceRefresh)
        {
            Authenticator authenticator = new Authenticator(authority, this.ValidateAuthority, this.CorrelationId);
            return await this.AcquireTokenSilentCommonAsync(authenticator, scope, user, this.PlatformParameters, policy, forceRefresh).ConfigureAwait(false);
        }

        
        internal async Task<AuthenticationResult> AcquireTokenSilentCommonAsync(Authenticator authenticator, string[] scope, string userIdentifier, IPlatformParameters parameters, string policy, bool forceRefresh)
        {
            if (parameters == null)
            {
                parameters = PlatformPlugin.DefaultPlatformParameters;
            }

            var handler = new AcquireTokenSilentHandler(this.GetHandlerData(authenticator, scope, policy, this.UserTokenCache), userIdentifier,  parameters, forceRefresh);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal async Task<AuthenticationResult> AcquireTokenSilentCommonAsync(Authenticator authenticator, string[] scope, User user, IPlatformParameters parameters, string policy, bool forceRefresh)
        {
            if (parameters == null)
            {
                parameters = PlatformPlugin.DefaultPlatformParameters;
            }

            var handler = new AcquireTokenSilentHandler(this.GetHandlerData(authenticator, scope, policy, this.UserTokenCache), user, parameters, forceRefresh);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal virtual HandlerData GetHandlerData(Authenticator authenticator, string[] scope, string policy, TokenCache cache)
        {
            return new HandlerData
            {
                Authenticator = authenticator,
                Scope = scope,
                Policy = policy,
                RestrictToSingleUser = this.RestrictToSingleUser,
                TokenCache = cache
            };
        }
    }
}
