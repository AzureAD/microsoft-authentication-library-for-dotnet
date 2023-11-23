// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <inheritdoc/>
    public partial interface IClientApplicationBase : IApplicationBase
    {
        /// <summary>
        /// Details on the configuration of the ClientApplication for debugging purposes.
        /// </summary>
        IAppConfig AppConfig { get; }

        /// <summary>
        /// User token cache which holds ID tokens, access tokens, and refresh tokens for accounts. It's used
        /// and updated silently if needed when calling <see cref="ClientApplicationBase.AcquireTokenSilent(IEnumerable{string}, IAccount)"/>
        /// It is updated by each acquire token method, with the exception of <see cref="IConfidentialClientApplication.AcquireTokenForClient(IEnumerable{string})"/> which only uses the application
        /// cache (see <see cref="IConfidentialClientApplication.AppTokenCache"/>).
        /// </summary>
        /// <remarks>On .NET Framework and .NET Core you can also customize the token cache serialization.
        /// See https://aka.ms/msal-net-token-cache-serialization. This is taken care of by MSAL.NET on mobile platforms and on UWP.
        /// It is recommended to use token cache serialization for web site and web api scenarios.
        /// </remarks>
        ITokenCache UserTokenCache { get; }

        /// <summary>
        /// Gets the URL of the authority, or the security token service (STS) from which MSAL.NET will acquire security tokens.
        /// The return value of this property is either the value provided by the developer in the constructor of the application, or otherwise
        /// the value of the <see cref="ApplicationBase.DefaultAuthority"/> static member (that is <c>https://login.microsoftonline.com/common/</c>)
        /// </summary>
        // TODO: move to IAppConfig like ClientId?
        string Authority { get; }

        /// <summary>
        /// Returns all the available <see cref="IAccount">accounts</see> in the user token cache for the application.
        /// </summary>
        Task<IEnumerable<IAccount>> GetAccountsAsync();

        /// <summary>
        /// Get the <see cref="IAccount"/> by its identifier among the accounts available in the token cache and of the same
        /// environment (authority host) as <see cref="Authority"/>.
        /// </summary>
        /// <param name="identifier">Account identifier. The value of the identifier will probably have been stored value from the
        /// value of the <see cref="AccountId.Identifier"/> property of <see cref="AccountId"/>.
        /// You typically get the account ID from an <see cref="IAccount"/> by using the <see cref="IAccount.HomeAccountId"/> property.</param>
        Task<IAccount> GetAccountAsync(string identifier);

        /// <summary>
        /// Only for Azure AD B2C scenarios, get the <see cref="IAccount"/> collection by its identifier
        /// among the accounts available in the token cache based on the user flow.
        /// </summary>
        /// <param name="userFlow">The identifier is the user flow being targeted by the specific B2C authority.</param>
        Task<IEnumerable<IAccount>> GetAccountsAsync(string userFlow);

        /// <summary>
        /// Attempts to acquire an access token for the <paramref name="account"/> from the user token cache,
        /// with advanced parameters controlling the network call. See <see href="https://aka.ms/msal-net-acquiretokensilent">Acquire tokens silently</see>.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <param name="account">Account for which the token is requested. <see cref="IAccount"/></param>
        /// <returns>An <see cref="AcquireTokenSilentParameterBuilder"/> used to build the token request, adding optional
        /// parameters.</returns>
        /// <exception cref="MsalUiRequiredException">When an interaction is required with the end user of the application,
        /// for instance, if no refresh token was in the cache; the user needs to consent or to re-sign-in (for instance if the password expired);
        /// or the user needs to perform two factor authentication.</exception>
        /// <remarks>
        /// The access token is considered a match if it contains <b>at least</b> all the requested scopes. This means that an access token with more scopes than
        /// requested could be returned as well. If the access token is expired or close to expiration (within a 5 minute window),
        /// then the cached refresh token (if available) is used to acquire a new access token by making a silent network call.
        /// </remarks>
        AcquireTokenSilentParameterBuilder AcquireTokenSilent(IEnumerable<string> scopes, IAccount account);

        /// <summary>
        /// Attempts to acquire an access token for the <paramref name="loginHint"/> from the user token cache,
        /// with advanced parameters controlling the network call. See <see href="https://aka.ms/msal-net-acquiretokensilent">Acquire tokens silently</see>.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <param name="loginHint">Typically the username, in UPN format, e.g. <c>johnd@contoso.com</c>.</param>
        /// <returns>An <see cref="AcquireTokenSilentParameterBuilder"/> used to build the token request, adding optional
        /// parameters.</returns>
        /// <exception cref="MsalUiRequiredException">When an interaction is required with the end user of the application,
        /// for instance, if no refresh token was in the cache; the user needs to consent or to re-sign-in (for instance if the password expired);
        /// or the user needs to perform two factor authentication.</exception>
        /// <remarks>
        /// The access token is considered a match if it contains <b>at least</b> all the requested scopes. This means that an access token with more scopes than
        /// requested could be returned as well. If the access token is expired or close to expiration (within a 5 minute window),
        /// then the cached refresh token (if available) is used to acquire a new access token by making a silent network call.
        /// </remarks>
        AcquireTokenSilentParameterBuilder AcquireTokenSilent(IEnumerable<string> scopes, string loginHint);

        /// <summary>
        /// Removes all tokens in the cache for the specified account.
        /// </summary>
        /// <param name="account">Instance of the account that needs to be removed.</param>
        Task RemoveAsync(IAccount account);
   }
}
