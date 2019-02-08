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
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;

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
        /// <summary>
        /// [V2 API] Constructor for a confidential client application requesting tokens with the default authority (<see cref="ClientApplicationBase.DefaultAuthority"/>)
        /// </summary>
        /// <param name="clientId">Client ID (also known as App ID) of the application as registered in the
        /// application registration portal (https://aka.ms/msal-net-register-app)/. REQUIRED</param>
        /// <param name="redirectUri">URL where the STS will call back the application with the security token. REQUIRED</param>
        /// <param name="clientCredential">Credential, previously shared with Azure AD during the application registration and proving the identity
        /// of the application. An instance of <see cref="ClientCredential"/> can be created either from an application secret, or a certificate. REQUIRED.</param>
        /// <param name="userTokenCache">Token cache for saving user tokens. Can be set to null if the confidential client
        /// application only uses the Client Credentials grants (that is requests token in its own name and not in the name of users).
        /// Otherwise should be provided. REQUIRED</param>
        /// <param name="appTokenCache">Token cache for saving application (that is client token). Can be set to <c>null</c> except if the application
        /// uses the client credentials grants</param>
        /// <remarks>
        /// See https://aka.ms/msal-net-client-applications for a description of confidential client applications (and public client applications)
        /// Client credential grants are overrides of <see cref="ConfidentialClientApplication.AcquireTokenForClientAsync(IEnumerable{string})"/>
        /// 
        /// See also <see cref="T:ConfidentialClientApplicationBuilder"/> for the V3 API way of building a confidential client application
        /// with a builder pattern. It offers building the application from configuration options, and a more fluid way of providing parameters.
        /// </remarks>
        /// <seealso cref="ConfidentialClientApplication"/> which
        /// enables app developers to specify the authority
        public ConfidentialClientApplication(string clientId, string redirectUri,
            ClientCredential clientCredential, TokenCache userTokenCache, TokenCache appTokenCache)
            : this(ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(new Uri(DefaultAuthority), true)
                .WithRedirectUri(redirectUri)
                .WithClientCredential(clientCredential)
                .BuildConfiguration())
        {
            GuardMobileFrameworks();

            SetTokenCaches(userTokenCache, appTokenCache);
        }

        private void SetTokenCaches(TokenCache userTokenCache, TokenCache appTokenCache)
        {
            if (userTokenCache == null)
            {
                userTokenCache = new TokenCache(ServiceBundle);
            }
            userTokenCache.SetServiceBundle(ServiceBundle);
            UserTokenCacheInternal = userTokenCache;

            if (appTokenCache == null)
            {
                appTokenCache = new TokenCache(ServiceBundle);
            }

            appTokenCache.SetServiceBundle(ServiceBundle);
            AppTokenCacheInternal = appTokenCache;
        }

        /// <summary>
        /// [V2 API] Constructor for a confidential client application requesting tokens with a specified authority
        /// </summary>
        /// <param name="clientId">Client ID (also named Application ID) of the application as registered in the
        /// application registration portal (https://aka.ms/msal-net-register-app)/. REQUIRED</param>
        /// <param name="authority">Authority of the security token service (STS) from which MSAL.NET will acquire the tokens.
        /// Usual authorities are:
        /// <list type="bullet">
        /// <item><description><c>https://login.microsoftonline.com/tenant/</c>, where <c>tenant</c> is the tenant ID of the Azure AD tenant
        /// or a domain associated with this Azure AD tenant, in order to sign-in users of a specific organization only</description></item>
        /// <item><description><c>https://login.microsoftonline.com/common/</c> to sign-in users with any work and school accounts or Microsoft personal accounts</description></item>
        /// <item><description><c>https://login.microsoftonline.com/organizations/</c> to sign-in users with any work and school accounts</description></item>
        /// <item><description><c>https://login.microsoftonline.com/consumers/</c> to sign-in users with only personal Microsoft accounts(live)</description></item>
        /// </list>
        /// Note that this setting needs to be consistent with what is declared in the application registration portal
        /// </param>
        /// <param name="redirectUri">URL where the STS will call back the application with the security token. REQUIRED</param>
        /// <param name="clientCredential">Credential, previously shared with Azure AD during the application registration and proving the identity
        /// of the application. An instance of <see cref="ClientCredential"/> can be created either from an application secret, or a certificate. REQUIRED.</param>
        /// <param name="userTokenCache">Token cache for saving user tokens. Can be set to null if the confidential client
        /// application only uses the Client Credentials grants (that is requests token in its own name and not in the name of users).
        /// Otherwise should be provided. REQUIRED</param>
        /// <param name="appTokenCache">Token cache for saving application (that is client token). Can be set to <c>null</c> except if the application
        /// uses the client credentials grants</param>
        /// <remarks>
        /// See https://aka.ms/msal-net-client-applications for a description of confidential client applications (and public client applications)
        /// Client credential grants are overrides of <see cref="ConfidentialClientApplication.AcquireTokenForClientAsync(IEnumerable{string})"/>
        /// 
        /// See also <see cref="T:ConfidentialClientApplicationBuilder"/> for the V3 API way of building a confidential client application
        /// with a builder pattern. It offers building the application from configuration options, and a more fluid way of providing parameters.
        /// </remarks>
        /// <seealso cref="ConfidentialClientApplication"/> which
        /// enables app developers to create a confidential client application requesting tokens with the default authority.
        public ConfidentialClientApplication(string clientId, string authority, string redirectUri,
            ClientCredential clientCredential, TokenCache userTokenCache, TokenCache appTokenCache)
            : this(ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(new Uri(authority), true)
                .WithRedirectUri(redirectUri)
                .WithClientCredential(clientCredential)
                .BuildConfiguration())
        {
            GuardMobileFrameworks();
            SetTokenCaches(userTokenCache, appTokenCache);
        }

        /// <summary>
        /// [V2 API] Acquires an access token for this application (usually a Web API) from the authority configured in the application, in order to access
        /// another downstream protected Web API on behalf of a user using the OAuth 2.0 On-Behalf-Of flow. (See https://aka.ms/msal-net-on-behalf-of).
        /// This confidential client application was itself called with a token which will be provided in the
        /// <paramref name="userAssertion">userAssertion</paramref> parameter.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="userAssertion">Instance of <see cref="UserAssertion"/> containing credential information about
        /// the user on behalf of whom to get a token.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <seealso cref="AcquireTokenOnBehalfOfAsync(IEnumerable{string}, UserAssertion, string)"/> for the on-behalf-of flow when specifying the authority
        /// <seealso cref="AcquireTokenOnBehalfOf(IEnumerable{string}, UserAssertion)"/> which is the corresponding V3 API.
        public async Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(IEnumerable<string> scopes, UserAssertion userAssertion)
        {
            GuardMobileFrameworks();
            
            return await AcquireTokenOnBehalfOf(scopes, userAssertion).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// [V2 API] Acquires an access token for this application (usually a Web API) from a specific authority, in order to access
        /// another downstream protected Web API on behalf of a user (See https://aka.ms/msal-net-on-behalf-of).
        /// This confidential client application was itself called with a token which will be provided in the
        /// <paramref name="userAssertion">userAssertion</paramref> parameter.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="userAssertion">Instance of <see cref="UserAssertion"/> containing credential information about
        /// the user on behalf of whom to get a token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <seealso cref="AcquireTokenOnBehalfOfAsync(IEnumerable{string}, UserAssertion)"/> for the on-behalf-of flow without specifying the authority
        /// <seealso cref="AcquireTokenOnBehalfOf(IEnumerable{string}, UserAssertion)"/> which is the corresponding V3 API.
        public async Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(IEnumerable<string> scopes, UserAssertion userAssertion,
            string authority)
        {
            GuardMobileFrameworks();
            return await AcquireTokenOnBehalfOf(scopes, userAssertion).WithAuthority(authority).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// [V2 API] Acquires an access token for this application (usually a Web API) from the authority configured in the application, in order to access
        /// another downstream protected Web API on behalf of a user using the OAuth 2.0 On-Behalf-Of flow. (See https://aka.ms/msal-net-on-behalf-of).
        /// This confidential client application was itself called with a token which will be provided in the
        /// <paramref name="userAssertion">userAssertion</paramref> parameter.
        /// This override sends the certificate, which helps certificate rotation in Azure AD
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="userAssertion">Instance of <see cref="UserAssertion"/> containing credential information about
        /// the user on behalf of whom to get a token.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <seealso cref="AcquireTokenOnBehalfOf(IEnumerable{string}, UserAssertion)"/> which is the corresponding V3 API
        async Task<AuthenticationResult> IConfidentialClientApplicationWithCertificate.AcquireTokenOnBehalfOfWithCertificateAsync(IEnumerable<string> scopes, UserAssertion userAssertion)
        {
            GuardMobileFrameworks();

            return await AcquireTokenOnBehalfOf(scopes, userAssertion).WithSendX5C(true).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// [V2 API] Acquires an access token for this application (usually a Web API) from a specific authority, in order to access
        /// another downstream protected Web API on behalf of a user (See https://aka.ms/msal-net-on-behalf-of).
        /// This confidential client application was itself called with a token which will be provided in the
        /// This override sends the certificate, which helps certificate rotation in Azure AD
        /// <paramref name="userAssertion">userAssertion</paramref> parameter.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="userAssertion">Instance of <see cref="UserAssertion"/> containing credential information about
        /// the user on behalf of whom to get a token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <seealso cref="AcquireTokenOnBehalfOf(IEnumerable{string}, UserAssertion)"/> which is the corresponding V3 API
        async Task<AuthenticationResult> IConfidentialClientApplicationWithCertificate.AcquireTokenOnBehalfOfWithCertificateAsync(IEnumerable<string> scopes, UserAssertion userAssertion,
            string authority)
        {
            GuardMobileFrameworks();

            return await AcquireTokenOnBehalfOf(scopes, userAssertion).WithAuthority(authority).WithSendX5C(true).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// [V2 API] Acquires a security token from the authority configured in the app using the authorization code previously received from the STS. It uses
        /// the OAuth 2.0 authorization code flow (See https://aka.ms/msal-net-authorization-code).
        /// It's usually used in Web Apps (for instance ASP.NET / ASP.NET Core Web apps) which sign-in users, and therefore receive an authorization code.
        /// This method does not lookup the token cache, but stores the result in it, so it can be looked up using other methods
        /// such as <see cref="IClientApplicationBase.AcquireTokenSilentAsync(IEnumerable{string}, IAccount)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        /// <seealso cref="AcquireTokenByAuthorizationCode(IEnumerable{string}, string)"/> which is the corresponding V2 API
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, IEnumerable<string> scopes)
        {
            GuardMobileFrameworks();

            return await AcquireTokenByAuthorizationCode(scopes, authorizationCode)
                .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// [V3 API] Acquires a token from the authority configured in the app, for the confidential client itself (in the name of no user)
        /// using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
        /// </summary>
        /// <param name="scopes">scopes requested to access a protected API. For this flow (client credentials), the scopes
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are really defined statically at application registration
        /// in the portal, and cannot be overriden in the application. See also </param>
        /// <returns>Authentication result containing the token of the user for the requested scopes</returns>
        public async Task<AuthenticationResult> AcquireTokenForClientAsync(IEnumerable<string> scopes)
        {
            GuardMobileFrameworks();

            return await AcquireTokenForClient(scopes)
                .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }
        /// <summary>
        /// [V2 API] Acquires a token from the authority configured in the app, for the confidential client itself (in the name of no user)
        /// using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API. For this flow (client credentials), the scopes
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are really defined statically at application registration
        /// in the portal, and cannot be overriden in the application</param>
        /// <param name="forceRefresh">If <c>true</c>, API will ignore the access token in the cache and attempt to acquire new access token using client credentials.
        /// This override can be used in case the application knows that conditional access policies changed</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        /// <seealso cref="AcquireTokenForClient(IEnumerable{string})"/> which is the corresponding V3 API
        public async Task<AuthenticationResult> AcquireTokenForClientAsync(IEnumerable<string> scopes, bool forceRefresh)
        {
            GuardMobileFrameworks();

            return await AcquireTokenForClient(scopes)
                .WithForceRefresh(forceRefresh)
                .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// [V2 API] Acquires token from the service for the confidential client using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
        /// This method enables application developers to achieve easy certificate roll-over
        /// in Azure AD: this method will send the public certificate to Azure AD
        /// along with the token request, so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or powershell/CLI operation)
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>Authentication result containing application token for the requested scopes</returns>
        /// <seealso cref="AcquireTokenForClient(IEnumerable{string})"/> which is the corresponding V3 API
        async Task<AuthenticationResult> IConfidentialClientApplicationWithCertificate.AcquireTokenForClientWithCertificateAsync(IEnumerable<string> scopes)
        {
            GuardMobileFrameworks();

            return await AcquireTokenForClient(scopes)
                .WithSendX5C(true)
                .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// [V2 API] Acquires token from the service for the confidential client using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
        /// This method attempts to look up valid access token in the cache unless<paramref name="forceRefresh"/> is true
        /// This method enables application developers to achieve easy certificate roll-over
        /// in Azure AD: this method will send the public certificate to Azure AD
        /// along with the token request, so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or powershell/CLI operation)
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="forceRefresh">If TRUE, API will ignore the access token in the cache and attempt to acquire new access token using client credentials</param>
        /// <returns>Authentication result containing application token for the requested scopes</returns>
        /// <seealso cref="AcquireTokenForClient(IEnumerable{string})"/> which is the corresponding V3 API
        async Task<AuthenticationResult> IConfidentialClientApplicationWithCertificate.AcquireTokenForClientWithCertificateAsync(IEnumerable<string> scopes, bool forceRefresh)
        {
            GuardMobileFrameworks();

            return await AcquireTokenForClient(scopes)
                .WithForceRefresh(forceRefresh)
                .WithSendX5C(true)
                .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires an access token from an existing refresh token and stores it and the refresh token into
        /// the application user token cache, where it will be available for further AcquireTokenSilentAsync calls.
        /// This method can be used in migration to MSAL from ADAL v2 and in various integration
        /// scenarios where you have a RefreshToken available.
        /// (see https://aka.ms/msal-net-migration-adal2-msal2)
        /// </summary>
        /// <param name="scopes">Scope to request from the token endpoint.
        /// Setting this to null or empty will request an access token, refresh token and ID token with default scopes</param>
        /// <param name="refreshToken">The refresh token (for example previously obtained from ADAL 2.x)</param>
        async Task<AuthenticationResult> IByRefreshToken.AcquireTokenByRefreshTokenAsync(IEnumerable<string> scopes, string refreshToken)
        {
            GuardMobileFrameworks();

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentNullException(nameof(refreshToken), CoreErrorMessages.NoRefreshTokenProvided);
            }

            return await ((IByRefreshToken)this).AcquireTokenByRefreshToken(scopes, refreshToken).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// [V2 API] Computes the URL of the authorization request letting the user sign-in and consent to the application accessing specific scopes in
        /// the user's name. The URL targets the /authorize endpoint of the authority configured in the application.
        /// This override enables you to specify a login hint and extra query parameter.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN. This can be empty</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        /// <seealso cref="GetAuthorizationRequestUrl(IEnumerable{string})"/> which is the corresponding V3 API
        public async Task<Uri> GetAuthorizationRequestUrlAsync(IEnumerable<string> scopes, string loginHint,
                                                               string extraQueryParameters)
        {
            return await GetAuthorizationRequestUrl(scopes).WithLoginHint(loginHint)
                                                           .WithExtraQueryParameters(extraQueryParameters).ExecuteAsync(CancellationToken.None)
                                                           .ConfigureAwait(false);
        }

        /// <summary>
        /// [V2 API] Computes the URL of the authorization request letting the user sign-in and consent to the application accessing specific scopes in
        /// the user's name. The URL targets the /authorize endpoint of the authority specified as the <paramref name="authority"/> parameter.
        /// This override enables you to specify a redirectUri, login hint extra query parameters, extra scope to consent (which are not for the
        /// same resource as the <paramref name="scopes"/>), and an authority.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API (a resource)</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Scopes for additional resources (other than the resource for which <paramref name="scopes"/> are requested),
        /// which a developer can request the user to consent to upfront.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        /// <seealso cref="GetAuthorizationRequestUrl(IEnumerable{string})"/> which is the corresponding V3 API
        public async Task<Uri> GetAuthorizationRequestUrlAsync(IEnumerable<string> scopes, string redirectUri, string loginHint,
            string extraQueryParameters, IEnumerable<string> extraScopesToConsent, string authority)
        {
            return await GetAuthorizationRequestUrl(scopes)
                .WithRedirectUri(redirectUri)
                .WithLoginHint(loginHint)
                .WithExtraQueryParameters(extraQueryParameters)
                .WithExtraScopesToConsent(extraScopesToConsent)
                .WithAuthority(authority)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
        }

        // TODO(migration):
        // - Why isn't this method public?  -- it can't be public, it's an explicit interface implementation.
        // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/interfaces/explicit-interface-implementation
        // - should it move to ClientApplicationBase to help PCA moving from ADAL V2 to MSAL V2+?
        AcquireTokenByRefreshTokenParameterBuilder IByRefreshToken.AcquireTokenByRefreshToken(
            IEnumerable<string> scopes,
            string refreshToken)
        {
            return AcquireTokenByRefreshTokenParameterBuilder.Create(this, scopes, refreshToken);
        }

        internal ClientCredential ClientCredential => ServiceBundle.Config.ClientCredential;

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
