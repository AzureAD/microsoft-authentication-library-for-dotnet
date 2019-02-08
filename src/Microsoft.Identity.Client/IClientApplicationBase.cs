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
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.Identity.Client.AppConfig;

namespace Microsoft.Identity.Client
{
    /// <Summary>
    /// Abstract class containing common API methods and properties. Both <see cref="T:PublicClientApplication"/> and <see cref="T:ConfidentialClientApplication"/> 
    /// extend this class. For details see https://aka.ms/msal-net-client-applications
    /// </Summary>
    public partial interface IClientApplicationBase
    {
        /// <summary>
        /// Details on the configuration of the ClientApplication for debugging purposes.
        /// </summary>
        IAppConfig AppConfig { get; }

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
        ITokenCache UserTokenCache { get; }

        /// <Summary>
        /// Gets the URL of the authority, or the security token service (STS) from which MSAL.NET will acquire security tokens.
        /// The return value of this propety is either the value provided by the developer in the constructor of the application, or otherwise 
        /// the value of the <see cref="ClientApplicationBase.Authority"/> static member (that is <c>https://login.microsoftonline.com/common/</c>)
        /// </Summary>
        string Authority { get; }

        /// <summary>
        /// Gets the Client ID (also known as Application ID) of the application as registered in the application registration portal (https://aka.ms/msal-net-register-app)
        /// and as passed in the constructor of the application.
        /// </summary>
        string ClientId { get; }

        /// <summary>
        /// Returns all the available <see cref="IAccount">accounts</see> in the user token cache for the application.
        /// </summary>
        Task<IEnumerable<IAccount>> GetAccountsAsync();

        /// <summary>
        /// Get the <see cref="IAccount"/> by its identifier among the accounts available in the token cache.
        /// </summary>
        /// <param name="identifier">Account identifier. The value of the identifier will probably have been stored value from the
        /// value of the <see cref="AccountId.Identifier"/> property of <see cref="AccountId"/>. 
        /// You typically get the account id from an <see cref="IAccount"/> by using the <see cref="IAccount.HomeAccountId"/> property></param>
        Task<IAccount> GetAccountAsync(string identifier);

        /// <summary>
        /// Attempts to acquire an access token for the <paramref name="account"/> from the user token cache. 
        /// </summary> 
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account for which the token is requested. <see cref="IAccount"/></param>
        /// <returns>An <see cref="AuthenticationResult"/> containing the requested token</returns>
        /// <exception cref="MsalUiRequiredException">can be thrown in the case where an interaction is required with the end user of the application, 
        /// for instance so that the user consents, or re-signs-in (for instance if the password expirred), or performs two factor authentication</exception>
        /// <remarks>
        /// The access token is considered a match if it contains <b>at least</b> all the requested scopes.
        /// This means that an access token with more scopes than requested could be returned as well. If the access token is expired or 
        /// close to expiration (within 5 minute window), then the cached refresh token (if available) is used to acquire a new access token by making a silent network call.
        /// See https://aka.ms/msal-net-acuiretokensilent for more details
        /// </remarks>
        Task<AuthenticationResult> AcquireTokenSilentAsync(
            IEnumerable<string> scopes,
            IAccount account);

        /// <summary>
        /// Attempts to acquire and access token for the <paramref name="account"/> from the user token cache, with advanced parameters making a network call.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account for which the token is requested. <see cref="IAccount"/></param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured in the application constructor
        /// narrows down the selection of tenants for which to get a tenant, but does not change the configured value</param>
        /// <param name="forceRefresh">If <c>true</c>, the will ignore the access token in the cache and attempt to acquire new access token 
        /// using the refresh token for the account if this one is available. This can be useful in the case when the application developer wants to make
        /// sure that conditional access policies are applies immediately, rather than after the expiration of the access token</param>
        /// <returns>An <see cref="AuthenticationResult"/> containing the requested token</returns>
        /// <exception cref="MsalUiRequiredException">can be thrown in the case where an interaction is required with the end user of the application, 
        /// for instance, if no refresh token was in the cache, or the user needs to consents, or re-sign-in (for instance if the password expirred), 
        /// or performs two factor authentication</exception>
        /// <remarks>
        /// The access token is considered a match if it contains <b>at least</b> all the requested scopes. This means that an access token with more scopes than 
        /// requested could be returned as well. If the access token is expired or close to expiration (within 5 minute window), 
        /// then the cached refresh token (if available) is used to acquire a new access token by making a silent network call.
        /// See https://aka.ms/msal-net-acquiretokensilent for more details
        /// </remarks>
        Task<AuthenticationResult> AcquireTokenSilentAsync(
        IEnumerable<string> scopes,
            IAccount account,
            string authority,
            bool forceRefresh);

        /// <summary>
        /// Attempts to acquire an access token for the <paramref name="account"/> from the user token cache, 
        /// with advanced parameters controlling the network call. See https://aka.ms/msal-net-acquiretokensilent for more details
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account for which the token is requested. <see cref="IAccount"/></param>
        /// <returns>An <see cref="AcquireTokenSilentParameterBuilder"/> used to build the token request, adding optional
        /// parameters</returns>
        /// <exception cref="MsalUiRequiredException">will be thrown in the case where an interaction is required with the end user of the application,
        /// for instance, if no refresh token was in the cache,a or the user needs to consent, or re-sign-in (for instance if the password expired),
        /// or the user needs to perform two factor authentication</exception>
        /// <remarks>
        /// The access token is considered a match if it contains <b>at least</b> all the requested scopes. This means that an access token with more scopes than
        /// requested could be returned as well. If the access token is expired or close to expiration (within a 5 minute window),
        /// then the cached refresh token (if available) is used to acquire a new access token by making a silent network call.
        ///
        /// See also the additional parameters that you can set chain:
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/> or one of its
        /// overrides to request a token for a different authority than the one set at the application construction
        /// <see cref="AcquireTokenSilentParameterBuilder.WithForceRefresh(bool)"/> to bypass the user token cache and
        /// force refreshing the token, as well as
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/> to
        /// specify extra query parameters
        /// </remarks>
        AcquireTokenSilentParameterBuilder AcquireTokenSilent(IEnumerable<string> scopes, IAccount account = null);

        /// <summary>
        /// Removes all tokens in the cache for the specified account.
        /// </summary>
        /// <param name="account">instance of the account that needs to be removed</param>
        Task RemoveAsync(IAccount account);
   }
}
