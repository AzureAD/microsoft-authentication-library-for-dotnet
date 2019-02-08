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
using Microsoft.Identity.Client.Internal.Requests;
using System.Linq;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.TelemetryCore;
using System.Threading;
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.PlatformsCommon.Factories;

namespace Microsoft.Identity.Client
{
#if !DESKTOP && !NET_CORE
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
#endif
    /// <Summary>
    /// Abstract class containing common API methods and properties. Both <see cref="Microsoft.Identity.Client.PublicClientApplication"/> and <see cref="Microsoft.Identity.Client.ConfidentialClientApplication"/>
    /// extend this class. For details see https://aka.ms/msal-net-client-applications
    /// </Summary>
    public abstract partial class ClientApplicationBase : IClientApplicationBase
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
    {
        /// <Summary>
        /// Default Authority used for interactive calls.
        /// </Summary>
        internal const string DefaultAuthority = "https://login.microsoftonline.com/common/";

        internal IServiceBundle ServiceBundle { get; }

        /// <summary>
        /// Details on the configuration of the ClientApplication for debugging purposes.
        /// </summary>
        public IAppConfig AppConfig => ServiceBundle.Config;

        /// <Summary>
        /// Gets the URL of the authority, or security token service (STS) from which MSAL.NET will acquire security tokens
        /// The return value of this property is either the value provided by the developer in the constructor of the application, or otherwise
        /// the value of the <see cref="DefaultAuthority"/> static member (that is <c>https://login.microsoftonline.com/common/</c>)
        /// </Summary>
        public string Authority => ServiceBundle.Config.AuthorityInfo.CanonicalAuthority;

        /// <summary>
        /// Gets the Client ID (also known as <i>Application ID</i>) of the application as registered in the application registration portal (https://aka.ms/msal-net-register-app)
        /// and as passed in the constructor of the application
        /// </summary>
        public string ClientId => AppConfig.ClientId;

        /// <Summary>
        /// User token cache. This case holds id tokens, access tokens and refresh tokens for accounts. It's used 
        /// and updated silently if needed when calling <see cref="AcquireTokenSilent(IEnumerable{string}, IAccount)"/>
        /// or one of the overrides of <see cref="AcquireTokenSilentAsync(IEnumerable{string}, IAccount)"/>.
        /// It is updated by each AcquireTokenXXX method, with the exception of <c>AcquireTokenForClient</c> which only uses the application
        /// cache (see <c>IConfidentialClientApplication</c>).
        /// </Summary>
        /// <remarks>On .NET Framework and .NET Core you can also customize the token cache serialization.
        /// See https://aka.ms/msal-net-token-cache-serialization. This is taken care of by MSAL.NET on other platforms.
        /// </remarks>
        public ITokenCache UserTokenCache => UserTokenCacheInternal;

        internal ITokenCacheInternal UserTokenCacheInternal { get; set; }

        /// <summary>
        /// Returns all the available <see cref="IAccount">accounts</see> in the user token cache for the application.
        /// </summary>
        public Task<IEnumerable<IAccount>> GetAccountsAsync()
        {
            RequestContext requestContext = CreateRequestContext();
            IEnumerable<IAccount> accounts = Enumerable.Empty<IAccount>();
            if (UserTokenCache == null)
            {
                requestContext.Logger.Info("Token cache is null or empty. Returning empty list of accounts.");
            }
            else
            {
                accounts = UserTokenCacheInternal.GetAccounts(Authority, requestContext);
            }

            return Task.FromResult(accounts);
        }

        /// <summary>
        /// Get the <see cref="IAccount"/> by its identifier among the accounts available in the token cache.
        /// </summary>
        /// <param name="accountId">Account identifier. The identifier is typically
        /// value of the <see cref="AccountId.Identifier"/> property of <see cref="AccountId"/>.
        /// You typically get the account id from an <see cref="IAccount"/> by using the <see cref="IAccount.HomeAccountId"/> property>
        /// </param>
        public async Task<IAccount> GetAccountAsync(string accountId)
        {
            var accounts = await GetAccountsAsync().ConfigureAwait(false);
            return accounts.FirstOrDefault(account => account.HomeAccountId.Identifier.Equals(accountId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Removes all tokens in the cache for the specified account.
        /// </summary>
        /// <param name="account">Instance of the account that needs to be removed</param>
        public Task RemoveAsync(IAccount account)
        {
            RequestContext requestContext = CreateRequestContext();
            if (account != null)
            {
                UserTokenCacheInternal?.RemoveAccount(account, requestContext);
            }

            return Task.FromResult(0);
        }

        internal Authority GetAuthority(IAccount account)
        {
            var authority = Instance.Authority.CreateAuthority(ServiceBundle);
            var tenantId = authority.GetTenantId();

            if (Instance.Authority.TenantlessTenantNames.Contains(tenantId)
                && account.HomeAccountId?.TenantId != null)
            {
                authority.UpdateTenantId(account.HomeAccountId.TenantId);
            }

            return authority;
        }

        internal virtual AuthenticationRequestParameters CreateRequestParameters(
            AcquireTokenCommonParameters commonParameters,
            ITokenCacheInternal cache,
            Authority customAuthority = null)
        {
            return new AuthenticationRequestParameters(
                ServiceBundle,
                customAuthority,
                cache,
                commonParameters,
                CreateRequestContext());
        }

        private RequestContext CreateRequestContext()
        {
            return new RequestContext(ClientId, MsalLogger.Create(Guid.NewGuid(), ServiceBundle.Config));
        }

        /// <summary>
        /// [V2 API] Attempts to acquire an access token for the <paramref name="account"/> from the user token cache, with advanced parameters controlling network call.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account for which the token is requested. <see cref="IAccount"/></param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured in the application constructor
        /// narrows down the selection to a specific tenant. This does not change the configured value in the application. This is specific
        /// to applications managing several accounts (like a mail client with several mailboxes)</param>
        /// <param name="forceRefresh">If <c>true</c>, ignore any access token in the cache and attempt to acquire new access token
        /// using the refresh token for the account if this one is available. This can be useful in the case when the application developer wants to make
        /// sure that conditional access policies are applied immediately, rather than after the expiration of the access token</param>
        /// <returns>An <see cref="AuthenticationResult"/> containing the requested access token</returns>
        /// <exception cref="MsalUiRequiredException">can be thrown in the case where an interaction is required with the end user of the application,
        /// for instance, if no refresh token was in the cache,a or the user needs to consent, or re-sign-in (for instance if the password expired),
        /// or performs two factor authentication</exception>
        /// <remarks>
        /// The access token is considered a match if it contains <b>at least</b> all the requested scopes. This means that an access token with more scopes than
        /// requested could be returned as well. If the access token is expired or close to expiration (within a 5 minute window),
        /// then the cached refresh token (if available) is used to acquire a new access token by making a silent network call.
        ///
        /// See https://aka.ms/msal-net-acquiretokensilent for more details
        /// </remarks>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(IEnumerable<string> scopes, IAccount account,
            string authority, bool forceRefresh)
        {
            var builder = AcquireTokenSilent(scopes, account)
                .WithForceRefresh(forceRefresh);
            if (!string.IsNullOrWhiteSpace(authority))
            {
                builder.WithAuthority(authority);
            }

            return await builder.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// [V2 API] Attempts to acquire an access token for the <paramref name="account"/> from the user token cache.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account for which the token is requested. <see cref="IAccount"/></param>
        /// <returns>An <see cref="AuthenticationResult"/> containing the requested token</returns>
        /// <exception cref="MsalUiRequiredException">can be thrown in the case where an interaction is required with the end user of the application,
        /// for instance so that the user consents, or re-signs-in (for instance if the password expired), or performs two factor authentication</exception>
        /// <remarks>
        /// The access token is considered a match if it contains <b>at least</b> all the requested scopes.
        /// This means that an access token with more scopes than requested could be returned as well. If the access token is expired or
        /// close to expiration (within a 5 minute window), then the cached refresh token (if available) is used to acquire a new access token by making a silent network call.
        ///
        /// See https://aka.ms/msal-net-acquiretokensilent for more details
        /// </remarks>
        public async Task<AuthenticationResult> AcquireTokenSilentAsync(IEnumerable<string> scopes, IAccount account)
        {
            return await AcquireTokenSilent(scopes, account)
                         .ExecuteAsync(CancellationToken.None)
                         .ConfigureAwait(false);
        }
    }
}
