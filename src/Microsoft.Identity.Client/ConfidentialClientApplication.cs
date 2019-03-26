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

using Microsoft.Identity.Client.Internal.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.TelemetryCore;
using System.Threading;
using Microsoft.Identity.Client.ApiConfig;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.Exceptions;

namespace Microsoft.Identity.Client
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

    /// <summary>
    /// Class to be used for confidential client applications (Web Apps, Web APIs, and daemon applications).
    /// </summary>
    /// <remarks>
    /// Confidential client applications are typically applications which run on servers (Web Apps, Web API, or even service/daemon applications).
    /// They are considered difficult to access, and therefore capable of keeping an application secret (hold configuration
    /// time secrets as these values would be difficult for end users to extract).
    /// A web app is the most common confidential client. The clientId is exposed through the web browser, but the secret is passed only in the back channel
    /// and never directly exposed. For details see https://aka.ms/msal-net-client-applications
    /// </remarks>
    public sealed partial class ConfidentialClientApplication
        : ClientApplicationBase,
            IConfidentialClientApplication,
            IConfidentialClientApplicationWithCertificate,
            IByRefreshToken
    {
        internal ConfidentialClientApplication(ApplicationConfiguration configuration)
            : base(configuration)
        {
            GuardMobileFrameworks();
            AppTokenCacheInternal = new TokenCache(ServiceBundle);
        }

        /// <summary>
        /// Acquires a security token from the authority configured in the app using the authorization code
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
        public AcquireTokenByAuthorizationCodeParameterBuilder AcquireTokenByAuthorizationCode(
            IEnumerable<string> scopes,
            string authorizationCode)
        {
            return AcquireTokenByAuthorizationCodeParameterBuilder.Create(
                ClientExecutorFactory.CreateConfidentialClientExecutor(this),
                scopes,
                authorizationCode);
        }

        /// <summary>
        /// Acquires a token from the authority configured in the app, for the confidential client itself (in the name of no user)
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
        public AcquireTokenForClientParameterBuilder AcquireTokenForClient(
            IEnumerable<string> scopes)
        {
            return AcquireTokenForClientParameterBuilder.Create(
                ClientExecutorFactory.CreateConfidentialClientExecutor(this),
                scopes);
        }

        /// <summary>
        /// Acquires an access token for this application (usually a Web API) from the authority configured in the application,
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
        public AcquireTokenOnBehalfOfParameterBuilder AcquireTokenOnBehalfOf(
            IEnumerable<string> scopes,
            UserAssertion userAssertion)
        {
            return AcquireTokenOnBehalfOfParameterBuilder.Create(
                ClientExecutorFactory.CreateConfidentialClientExecutor(this),
                scopes,
                userAssertion);
        }

        /// <summary>
        /// Computes the URL of the authorization request letting the user sign-in and consent to the application accessing specific scopes in
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
        public GetAuthorizationRequestUrlParameterBuilder GetAuthorizationRequestUrl(
            IEnumerable<string> scopes)
        {
            return GetAuthorizationRequestUrlParameterBuilder.Create(
                ClientExecutorFactory.CreateConfidentialClientExecutor(this),
                scopes);
        }

        AcquireTokenByRefreshTokenParameterBuilder IByRefreshToken.AcquireTokenByRefreshToken(
            IEnumerable<string> scopes,
            string refreshToken)
        {
            return AcquireTokenByRefreshTokenParameterBuilder.Create(
                ClientExecutorFactory.CreateClientApplicationBaseExecutor(this),
                scopes,
                refreshToken);
        }

        internal ClientCredentialWrapper ClientCredential => ServiceBundle.Config.ClientCredential;

        /// <Summary>
        /// Application token cache. This case holds access tokens and refresh tokens for the application. It's maintained 
        /// and updated silently if needed when <see cref="AcquireTokenForClient(IEnumerable{string})"/> or one
        /// of the overrides of <see cref="AcquireTokenForClientAsync(IEnumerable{string})"/>
        /// </Summary>
        /// <remarks>On .NET Framework and .NET Core you can also customize the token cache serialization.
        /// See https://aka.ms/msal-net-token-cache-serialization. This is taken care of by MSAL.NET on other platforms
        /// </remarks>
        public ITokenCache AppTokenCache => AppTokenCacheInternal;

        internal ITokenCacheInternal AppTokenCacheInternal { get; set; }

        internal override AuthenticationRequestParameters CreateRequestParameters(
            AcquireTokenCommonParameters commonParameters,
            ITokenCacheInternal cache,
            Authority customAuthority = null)
        {
            AuthenticationRequestParameters requestParams = base.CreateRequestParameters(commonParameters, cache, customAuthority);
            requestParams.ClientCredential = ServiceBundle.Config.ClientCredential;
            return requestParams;
        }

        internal static void GuardMobileFrameworks()
        {
#if ANDROID || iOS || WINDOWS_APP || MAC
            throw new PlatformNotSupportedException(
                "Confidential Client flows are not available on mobile platforms or on Mac." +
                "See https://aka.ms/msal-net-confidential-availability for details.");
#endif
        }
    }

#endif
}
