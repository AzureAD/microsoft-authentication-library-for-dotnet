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
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Telemetry;

namespace Microsoft.Identity.Client
{
#if !DESKTOP && !NET_CORE
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
#endif
    /// <Summary>
    /// Abstract class containing common API methods and properties. Both <see cref="Microsoft.Identity.Client.PublicClientApplication"/> and <see cref="Microsoft.Identity.Client.ConfidentialClientApplication"/> 
    /// extend this class. For details see https://aka.ms/msal-net-client-applications
    /// </Summary>
    public abstract partial class ClientApplicationBase
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
    {
        private TokenCache userTokenCache;

        /// <Summary>
        /// Default Authority used for interactive calls.
        /// </Summary>
        internal const string DefaultAuthority = "https://login.microsoftonline.com/common/";

        /// <summary>
        /// Constructor of the base application
        /// </summary>
        /// <param name="clientId">Client ID (also known as <i>Application ID</i>) of the application as registered in the 
        /// application registration portal (https://aka.ms/msal-net-register-app)</param>
        /// <param name="authority">URL of the security token service (STS) from which MSAL.NET will acquire the tokens.
        /// 
        /// Usual authorities endpoints for the Azure public Cloud are:
        /// <list type="bullet">
        /// <item><description><c>https://login.microsoftonline.com/tenant/</c> where <c>tenant</c> is the tenant ID of the Azure AD tenant
        /// or a domain associated with this Azure AD tenant, in order to sign-in users of a specific organization only</description></item>
        /// <item><description><c>https://login.microsoftonline.com/common/</c> to sign-in users with any work and school accounts or Microsoft personal account</description></item>
        /// <item><description><c>https://login.microsoftonline.com/organizations/</c> to sign-in users with any work and school accounts</description></item>
        /// <item><description><c>https://login.microsoftonline.com/consumers/</c> to sign-in users with only personal Microsoft accounts (live)</description></item>
        /// </list>
        /// Note that this setting needs to be consistent with what is declared in the application registration portal
        /// </param>
        /// <param name="redirectUri">also named <i>Reply URI</i>, the redirect URI is the URI where the STS will call back the application with the security token. For details see https://aka.ms/msal-net-client-applications</param>
        /// <param name="validateAuthority">Boolean telling MSAL.NET if the authority needs to be verified against a list of known authorities. 
        /// This should be set to <c>false</c> for Azure AD B2C authorities as those are customer specific (a list of known B2C authorities
        /// cannot be maintained by MSAL.NET</param>
        protected ClientApplicationBase(string clientId, string authority, string redirectUri,
            bool validateAuthority)
        {
            ClientId = clientId;
            Authority authorityInstance = Core.Instance.Authority.CreateAuthority(authority, validateAuthority);
            Authority = authorityInstance.CanonicalAuthority;
            RedirectUri = redirectUri;
            ValidateAuthority = validateAuthority;
            if (UserTokenCache != null)
            {
                UserTokenCache.ClientId = clientId;
            }

            RequestContext requestContext = new RequestContext(new MsalLogger(Guid.Empty, null));

            var msg = string.Format(CultureInfo.InvariantCulture,
                "MSAL {0} with assembly version '{1}', file version '{2}' and informational version '{3}' is running...",
                new PlatformInformation().GetProductName(), MsalIdHelper.GetMsalVersion(),
                MsalIdHelper.GetAssemblyFileVersion(), MsalIdHelper.GetAssemblyInformationalVersion());
            requestContext.Logger.Info(msg);
            requestContext.Logger.InfoPii(msg);
        }

        /// <summary>
        /// Identifier of the component (libraries/SDK) consuming MSAL.NET. 
        /// This will allow for disambiguation between MSAL usage by the app vs MSAL usage by component libraries.
        /// </summary>
        public string Component { get; set; }

        /// <Summary>
        /// Gets the URL of the authority, or security token service (STS) from which MSAL.NET will acquire security tokens
        /// The return value of this property is either the value provided by the developer in the constructor of the application, or otherwise 
        /// the value of the <see cref="DefaultAuthority"/> static member (that is <c>https://login.microsoftonline.com/common/</c>)
        /// </Summary>
        public string Authority { get; }

        /// <summary>
        /// Gets the Client ID (also knwon as <i>Application ID</i>) of the application as registered in the application registration portal (https://aka.ms/msal-net-register-app)
        /// and as passed in the constructor of the application
        /// </summary>
        public string ClientId { get; }

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
        /// <summary>
        /// The redirect URI (also known as Reply URI or Reply URL), is the URI at which Azure AD will contact back the application with the tokens. 
        /// This redirect URI needs to be registered in the app registration (https://aka.ms/msal-net-register-app).
        /// In MSAL.NET, <see cref="T:PublicClientApplication"/> define the following default RedirectUri values:
        /// <list type="bullet">
        /// <item><description><c>urn:ietf:wg:oauth:2.0:oob</c> for desktop (.NET Framework and .NET Core) applications</description></item>
        /// <item><description><c>msal{ClientId}</c> for Xamarin iOS and Xamarin Android (as this will be used by the system web browser by default on these
        /// platforms to call back the application)
        /// </description></item>
        /// </list>
        /// These default URIs could change in the future.
        /// In <see cref="Microsoft.Identity.Client.ConfidentialClientApplication"/>, this can be the URL of the Web application / Web API.
        /// </summary>
        /// <remarks>This is especially important when you deploy an application that you have initially tested locally; 
        /// you then need to add the reply URL of the deployed application in the application registration portal</remarks>
        public string RedirectUri { get; set; }
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved

        /// <summary>
        /// Sets or Gets a custom query parameters that may be sent to the STS for dogfood testing or debugging. This is a string of segments
        /// of the form <c>key=value</c> separated by an ampersand character.
        /// Unless requested otherwise by Microsoft support, this parameter should not be set by application developers as it may have adverse effect on the application.
        /// This property is also concatenated to the <c>extraQueryParameter</c> parameters of token acquisition operations.
        /// </summary>
        public string SliceParameters { get; set; }

        /// <Summary>
        /// Token Cache instance for storing User tokens.
        /// </Summary>
        internal TokenCache UserTokenCache
        {
            get { return userTokenCache; }
            set
            {
                userTokenCache = value;
                if (userTokenCache != null)
                {
                    userTokenCache.ClientId = ClientId;
                }
            }
        }

        /// <summary>
        /// Gets/sets a boolean value telling the application if the authority needs to be verified against a list of known authorities. The default
        /// value is <c>true</c>. It should currently be set to <c>false</c> for Azure AD B2C authorities as those are customer specific 
        /// (a list of known B2C authorities cannot be maintained by MSAL.NET). This property can be set just after the construction of the application
        /// and before an operation acquiring a token or interacting with the STS.
        /// </summary>
        public bool ValidateAuthority { get; set; }

        /// <summary>
        /// Returns all the available <see cref="IAccount">accounts</see> in the user token cache for the application.
        /// </summary>
        public async Task<IEnumerable<IAccount>> GetAccountsAsync()
        {
            RequestContext requestContext = new RequestContext(new MsalLogger(Guid.Empty, null));
            if (UserTokenCache == null)
            {
                const string msg = "Token cache is null or empty. Returning empty list of accounts.";
                requestContext.Logger.Info(msg);
                requestContext.Logger.InfoPii(msg);
                return Enumerable.Empty<Account>();
            }
            return await UserTokenCache.GetAccountsAsync(Authority, ValidateAuthority, requestContext).ConfigureAwait(false);
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
        /// Attempts to acquire an access token for the <paramref name="account"/> from the user token cache. 
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
            return
                await
                    AcquireTokenSilentCommonAsync(null, scopes, account, false, ApiEvent.ApiIds.AcquireTokenSilentWithoutAuthority)
                        .ConfigureAwait(false);
        }

        /// <summary>
        /// Attempts to acquire an access token for the <paramref name="account"/> from the user token cache, with advanced parameters controlling network call.
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
        /// for instance, if no refresh token was in the cache, or the user needs to consent, or re-sign-in (for instance if the password expired), 
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
            Authority authorityInstance = null;
            if (!string.IsNullOrEmpty(authority))
            {
                authorityInstance = Core.Instance.Authority.CreateAuthority(authority, ValidateAuthority);
            }

            return
                await
                    AcquireTokenSilentCommonAsync(authorityInstance, scopes, account,
                        forceRefresh, ApiEvent.ApiIds.AcquireTokenSilentWithAuthority).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes all tokens in the cache for the specified account.
        /// </summary>
        /// <param name="account">Instance of the account that needs to be removed</param>
        public async Task RemoveAsync(IAccount account)
        {
            RequestContext requestContext = CreateRequestContext(Guid.Empty);
            if (account == null || UserTokenCache == null)
            {
                return;
            }

            await UserTokenCache.RemoveAsync(Authority, ValidateAuthority, account, requestContext).ConfigureAwait(false);
        }

        internal Authority GetAuthority(IAccount account)
        {
            var authority = Core.Instance.Authority.CreateAuthority(Authority, ValidateAuthority);
            var tenantId = authority.GetTenantId();

            if (Core.Instance.Authority.TenantlessTenantNames.Contains(tenantId)
                && account.HomeAccountId?.TenantId != null)
            {
                authority.UpdateTenantId(account.HomeAccountId.TenantId);
            }

            return authority;
        }

        internal async Task<AuthenticationResult> AcquireTokenSilentCommonAsync(Authority authority,
            IEnumerable<string> scopes, IAccount account, bool forceRefresh, ApiEvent.ApiIds apiId)
        {
            if (account == null)
            {
                throw new MsalUiRequiredException(MsalUiRequiredException.UserNullError, MsalErrorMessage.MsalUiRequiredMessage);
            }

            if (authority == null)
            {
                authority = GetAuthority(account);
            }

            var handler = new SilentRequest(
                CreateRequestParameters(authority, scopes, account, UserTokenCache),
                forceRefresh)
            { ApiId = apiId };
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal virtual AuthenticationRequestParameters CreateRequestParameters(Authority authority,
            IEnumerable<string> scopes,
            IAccount account, TokenCache cache)
        {
            return new AuthenticationRequestParameters
            {
                SliceParameters = SliceParameters,
                Authority = authority,
                ClientId = ClientId,
                TokenCache = cache,
                Account = account,
                Scope = scopes.CreateSetFromEnumerable(),
                RedirectUri = new Uri(RedirectUri),
                RequestContext = CreateRequestContext(Guid.Empty),
                ValidateAuthority = ValidateAuthority
            };
        }

        internal RequestContext CreateRequestContext(Guid correlationId)
        {
            correlationId = (correlationId != Guid.Empty) ? correlationId : Guid.NewGuid();
            return new RequestContext(new MsalLogger(correlationId, Component));
        }
    }
}