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

namespace Microsoft.Identity.Client
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

    /// <summary>
    /// Component to be used with confidential client applications like Web Apps/API.
    /// </summary>
    public interface IConfidentialClientApplication : IClientApplicationBase
    {
        /// <Summary>
        /// Application token cache. This case holds access tokens and refresh tokens for the application. It's maintained 
        /// and updated silently if needed when <see cref="AcquireTokenForClient(IEnumerable{string})"/> or one
        /// of the overrides of <see cref="AcquireTokenForClientAsync(IEnumerable{string})"/>
        /// </Summary>
        /// <remarks>On .NET Framework and .NET Core you can also customize the token cache serialization.
        /// See https://aka.ms/msal-net-token-cache-serialization. This is taken care of by MSAL.NET on other platforms
        /// </remarks>
        ITokenCache AppTokenCache { get; }

        /// <summary>
        /// [V3 API] Acquires a security token from the authority configured in the app using the authorization code
        /// previously received from the STS.
        /// It uses the OAuth 2.0 authorization code flow (See https://aka.ms/msal-net-authorization-code).
        /// It's usually used in Web Apps (for instance ASP.NET / ASP.NET Core Web apps) which sign-in users,
        /// and can request an authorization code.
        /// This method does not lookup the token cache, but stores the result in it, so it can be looked up
        /// using other methods such as <see cref="IClientApplicationBase.AcquireTokenSilentAsync(IEnumerable{string}, IAccount)"/>.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="authorizationCode">The authorization code received from the service authorization endpoint.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>You can set optional parameters by chaining the builder with:
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithAuthority(string, bool)"/>,
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/>,
        /// </remarks>
        AcquireTokenByAuthorizationCodeParameterBuilder AcquireTokenByAuthorizationCode(
            IEnumerable<string> scopes,
            string authorizationCode);

        /// <summary>
        /// [V3 API] Acquires a token from the authority configured in the app, for the confidential client itself (in the name of no user)
        /// using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
        /// </summary>
        /// <param name="scopes">scopes requested to access a protected API. For this flow (client credentials), the scopes
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are really defined statically at application registration
        /// in the portal, and cannot be overriden in the application.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>You can also chain the following optional parameters:
        /// <see cref="AcquireTokenForClientParameterBuilder.WithForceRefresh(bool)"/>
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/>
        /// </remarks>
        AcquireTokenForClientParameterBuilder AcquireTokenForClient(IEnumerable<string> scopes);

        /// <summary>
        /// [V3 API] Acquires an access token for this application (usually a Web API) from the authority configured in the application,
        /// in order to access another downstream protected Web API on behalf of a user using the OAuth 2.0 On-Behalf-Of flow.
        /// (See https://aka.ms/msal-net-on-behalf-of).
        /// This confidential client application was itself called with a token which will be provided in the
        /// <paramref name="userAssertion">userAssertion</paramref> parameter.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="userAssertion">Instance of <see cref="UserAssertion"/> containing credential information about
        /// the user on behalf of whom to get a token.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>You can also chain the following optional parameters:
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/>
        /// </remarks>
        AcquireTokenOnBehalfOfParameterBuilder AcquireTokenOnBehalfOf(IEnumerable<string> scopes, UserAssertion userAssertion);

        /// <summary>
        /// [V3 API] Acquires token using On-Behalf-Of flow. (See https://aka.ms/msal-net-on-behalf-of)
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="userAssertion">Instance of UserAssertion containing user's token.</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(
            IEnumerable<string> scopes,
            UserAssertion userAssertion);

        /// <summary>
        /// [V3 API] Acquires token using On-Behalf-Of flow. (See https://aka.ms/msal-net-on-behalf-of)
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="userAssertion">Instance of UserAssertion containing user's token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(
            IEnumerable<string> scopes,
            UserAssertion userAssertion,
            string authority);

        /// <summary>
        /// [V2 API] Acquires security token from the authority using authorization code previously received.
        /// This method does not lookup token cache, but stores the result in it, so it can be looked up using other methods such as <see cref="IClientApplicationBase.AcquireTokenSilentAsync(System.Collections.Generic.IEnumerable{string}, IAccount)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(
            string authorizationCode,
            IEnumerable<string> scopes);

        /// <summary>
        /// [V2 API] Acquires token from the service for the confidential client. This method attempts to look up valid access token in the cache.
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <returns>Authentication result containing application token for the requested scopes</returns>
        Task<AuthenticationResult> AcquireTokenForClientAsync(
            IEnumerable<string> scopes);

        /// <summary>
        /// [V2 API] Acquires token from the service for the confidential client. This method attempts to look up valid access token in the cache.
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="forceRefresh">If TRUE, API will ignore the access token in the cache and attempt to acquire new access token using client credentials</param>
        /// <returns>Authentication result containing application token for the requested scopes</returns>
        Task<AuthenticationResult> AcquireTokenForClientAsync(
            IEnumerable<string> scopes,
            bool forceRefresh);

        /// <summary>
        /// [V3 API] Computes the URL of the authorization request letting the user sign-in and consent to the application accessing specific scopes in
        /// the user's name. The URL targets the /authorize endpoint of the authority configured in the application.
        /// This override enables you to specify a login hint and extra query parameter.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request to get the
        /// URL of the STS authorization endpoint parametrized with the parameters</returns>
        /// <remarks>You can also chain the following optional parameters:
        /// <see cref="GetAuthorizationRequestUrlParameterBuilder.WithRedirectUri(string)"/>
        /// <see cref="GetAuthorizationRequestUrlParameterBuilder.WithLoginHint(string)"/>
        /// <see cref="AbstractAcquireTokenParameterBuilder{T}.WithExtraQueryParameters(Dictionary{string, string})"/>
        /// <see cref="GetAuthorizationRequestUrlParameterBuilder.WithExtraScopesToConsent(IEnumerable{string})"/>
        /// </remarks>
        GetAuthorizationRequestUrlParameterBuilder GetAuthorizationRequestUrl(IEnumerable<string> scopes);


        /// <summary>
        /// [V2 API] URL of the authorize endpoint including the query parameters.
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        Task<Uri> GetAuthorizationRequestUrlAsync(
            IEnumerable<string> scopes,
            string loginHint,
            string extraQueryParameters);

        /// <summary>
        /// [V2 API] Gets URL of the authorize endpoint including the query parameters.
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Array of scopes for which a developer can request consent upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        Task<Uri> GetAuthorizationRequestUrlAsync(
            IEnumerable<string> scopes,
            string redirectUri,
            string loginHint,
            string extraQueryParameters, IEnumerable<string> extraScopesToConsent, string authority);
    }
#endif
}
