// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Represents confidential client applications - web apps, web APIs, daemon applications.
    /// </summary>
    /// <remarks>
    /// Confidential client applications are typically applications which run on servers (web apps, web API, or even service/daemon applications).
    /// They are considered difficult to access, and therefore capable of keeping an application secret (hold configuration
    /// time secrets as these values would be difficult for end users to extract).
    /// A web app is the most common confidential client. The client ID is exposed through the web browser, but the secret is passed only in the back channel
    /// and never directly exposed. For details, see <see href="https://aka.ms/msal-net-client-applications">Client Applications</see>.
    /// </remarks>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public partial interface IConfidentialClientApplication : IClientApplicationBase
    {
        /// <summary>
        /// Application token cache which holds access tokens for this application. It's maintained
        /// and updated silently when calling <see cref="AcquireTokenForClient(IEnumerable{string})"/>
        /// </summary>
        /// <remarks>On .NET Framework and .NET Core you can also customize the token cache serialization.
        /// See <see href="https://aka.ms/msal-net-token-cache-serialization">Token Cache Serialization</see>. This is taken care of by MSAL.NET on other platforms.
        /// </remarks>
        ITokenCache AppTokenCache { get; }

        /// <summary>
        /// Acquires a token from the authority configured in the app using the authorization code
        /// previously received from the identity provider using the OAuth 2.0 authorization code flow.
        /// See <see href="https://aka.ms/msal-net-authorization-code">Authorization Code Flow</see>.
        /// This flow is usually used in web apps (for instance, ASP.NET and ASP.NET Core web apps)
        /// which sign-in users and can request an authorization code.
        /// This method does not look in the token cache, but stores the result in it. Before calling this method, use other methods 
        /// such as <see cref="IClientApplicationBase.AcquireTokenSilent(IEnumerable{string}, IAccount)"/> to check the token cache.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <param name="authorizationCode">The authorization code received from the service authorization endpoint.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request.</returns>
        AcquireTokenByAuthorizationCodeParameterBuilder AcquireTokenByAuthorizationCode(
            IEnumerable<string> scopes,
            string authorizationCode);

        /// <summary>
        /// Acquires a token from the authority configured in the app for the confidential client itself (not for a user)
        /// using the client credentials flow. See <see href="https://aka.ms/msal-net-client-credentials">Client Credentials Flow</see>.
        /// During this operation MSAL will first search in the cache for an unexpired token before acquiring a new one from Microsoft Entra ID.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API. For this flow (client credentials), the scopes
        /// should be in the form of "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are defined statically in the application registration
        /// in the portal, and cannot be overridden in the application.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request.</returns>
        AcquireTokenForClientParameterBuilder AcquireTokenForClient(IEnumerable<string> scopes);

        /// <summary>
        /// Acquires an access token for this application (usually a web API) from the authority configured in the application,
        /// in order to access another downstream protected web API on behalf of a user using the OAuth 2.0 On-Behalf-Of flow.
        /// During this operation MSAL will first search in the cache for an unexpired token before acquiring a new one from Microsoft Entra ID.
        /// See <see href="https://aka.ms/msal-net-on-behalf-of">On-Behalf-Of Flow</see>.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <param name="userAssertion">Instance of <see cref="UserAssertion"/> containing credential information about
        /// the user on behalf of whom to get a token.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request.</returns>
        /// <remarks>
        /// Pass an access token (not an ID token) which was used to access this application in the
        /// <paramref name="userAssertion">userAssertion</paramref> parameter.
        /// For long-running or background processes in web API, see <see href="https://aka.ms/msal-net-long-running-obo">Long-running OBO in MSAL.NET</see>.
        /// </remarks>
        AcquireTokenOnBehalfOfParameterBuilder AcquireTokenOnBehalfOf(IEnumerable<string> scopes, UserAssertion userAssertion);

        /// <summary>
        /// Computes the URL of the authorization request letting the user sign-in and consent to the application accessing specific scopes in
        /// the user's name. The URL targets the /authorize endpoint of the authority configured in the application.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request to get the
        /// URL of the authorization endpoint with the specified parameters.</returns>
        GetAuthorizationRequestUrlParameterBuilder GetAuthorizationRequestUrl(IEnumerable<string> scopes);

        /// <summary>
        /// In confidential client apps use <see cref="IClientApplicationBase.AcquireTokenSilent(IEnumerable{string}, IAccount)"/> instead.
        /// </summary>
        [Obsolete("In confidential client apps use AcquireTokenSilent(scopes, account) instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new AcquireTokenSilentParameterBuilder AcquireTokenSilent(IEnumerable<string> scopes, string loginHint);

        /// <summary>
        /// Use <see cref="IClientApplicationBase.GetAccountAsync(string)"/> in web apps and web APIs, and use a token cache serializer for better security and performance. See https://aka.ms/msal-net-cca-token-cache-serialization.
        /// </summary>
        [Obsolete("Use GetAccountAsync(identifier) in web apps and web APIs, and use a token cache serializer for better security and performance. See https://aka.ms/msal-net-cca-token-cache-serialization.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        new Task<IEnumerable<IAccount>> GetAccountsAsync();
    }
}
