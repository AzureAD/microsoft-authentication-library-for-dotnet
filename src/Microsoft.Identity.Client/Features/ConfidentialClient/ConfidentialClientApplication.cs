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
    public sealed class ConfidentialClientApplication : ClientApplicationBase, IConfidentialClientApplication, IConfidentialClientApplicationWithCertificate, IByRefreshToken
    {
        static ConfidentialClientApplication()
        {
            ModuleInitializer.EnsureModuleInitialized();
        }

        /// <summary>
        /// Constructor for a confidential client application requesting tokens with the default authority (<see cref="ClientApplicationBase.DefaultAuthority"/>)
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
        /// </remarks>
        /// <seealso cref="ConfidentialClientApplication"/> which 
        /// enables app developers to specify the authority
        public ConfidentialClientApplication(string clientId, string redirectUri,
            ClientCredential clientCredential, TokenCache userTokenCache, TokenCache appTokenCache)
            : this(clientId, DefaultAuthority, redirectUri, clientCredential, userTokenCache, appTokenCache)
        {
            GuardMobileFrameworks();
        }

        /// <summary>
        /// Constructor for a confidential client application requesting tokens with a specified authority
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
        /// </remarks>
        /// <seealso cref="ConfidentialClientApplication"/> which 
        /// enables app developers to create a confidential client application requesting tokens with the default authority.
        public ConfidentialClientApplication(string clientId, string authority, string redirectUri,
            ClientCredential clientCredential, TokenCache userTokenCache, TokenCache appTokenCache)
            : this(null, clientId, authority, redirectUri, clientCredential, userTokenCache, appTokenCache)
        {
            GuardMobileFrameworks();
        }

        internal ConfidentialClientApplication(IServiceBundle serviceBundle, string clientId, string authority, string redirectUri,
                                               ClientCredential clientCredential, TokenCache userTokenCache, TokenCache appTokenCache)
            : base(clientId, authority, redirectUri, true, serviceBundle)
        {
            GuardMobileFrameworks();

            ClientCredential = clientCredential;
            UserTokenCache = userTokenCache;
            AppTokenCache = appTokenCache;
        }

        /// <summary>
        /// Acquires an access token for this application (usually a Web API) from the authority configured in the application, in order to access 
        /// another downstream protected Web API on behalf of a user using the OAuth 2.0 On-Behalf-Of flow. (See https://aka.ms/msal-net-on-behalf-of). 
        /// This confidential client application was itself called with a token which will be provided in the 
        /// <paramref name="userAssertion">userAssertion</paramref> parameter.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="userAssertion">Instance of <see cref="UserAssertion"/> containing credential information about
        /// the user on behalf of whom to get a token.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <seealso cref="AcquireTokenOnBehalfOfAsync(IEnumerable{string}, UserAssertion, string)"/> for the on-behalf-of flow when specifying the authority
        public async Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(IEnumerable<string> scopes, UserAssertion userAssertion)
        {
            GuardMobileFrameworks();

            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle, Authority, ValidateAuthority);
            return
                await
                    AcquireTokenOnBehalfCommonAsync(authority, scopes, userAssertion, ApiEvent.ApiIds.AcquireTokenOnBehalfOfWithScopeUser, false)
                        .ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires an access token for this application (usually a Web API) from a specific authority, in order to access 
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
        public async Task<AuthenticationResult> AcquireTokenOnBehalfOfAsync(IEnumerable<string> scopes, UserAssertion userAssertion,
            string authority)
        {
            GuardMobileFrameworks();

            Authority authorityInstance = Instance.Authority.CreateAuthority(ServiceBundle, authority, ValidateAuthority);
            return
                await
                    AcquireTokenOnBehalfCommonAsync(authorityInstance, scopes, userAssertion, ApiEvent.ApiIds.AcquireTokenOnBehalfOfWithScopeUserAuthority, false)
                        .ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires an access token for this application (usually a Web API) from the authority configured in the application, in order to access 
        /// another downstream protected Web API on behalf of a user using the OAuth 2.0 On-Behalf-Of flow. (See https://aka.ms/msal-net-on-behalf-of). 
        /// This confidential client application was itself called with a token which will be provided in the 
        /// <paramref name="userAssertion">userAssertion</paramref> parameter.
        /// This override sends the certificate, which helps certificate rotation in Azure AD
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="userAssertion">Instance of <see cref="UserAssertion"/> containing credential information about
        /// the user on behalf of whom to get a token.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        async Task<AuthenticationResult> IConfidentialClientApplicationWithCertificate.AcquireTokenOnBehalfOfWithCertificateAsync(IEnumerable<string> scopes, UserAssertion userAssertion)
        {
            GuardMobileFrameworks();

            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle, Authority, ValidateAuthority);
            return
                await
                    AcquireTokenOnBehalfCommonAsync(authority, scopes, userAssertion, ApiEvent.ApiIds.AcquireTokenOnBehalfOfWithScopeUser, true)
                        .ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires an access token for this application (usually a Web API) from a specific authority, in order to access 
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
        async Task<AuthenticationResult> IConfidentialClientApplicationWithCertificate.AcquireTokenOnBehalfOfWithCertificateAsync(IEnumerable<string> scopes, UserAssertion userAssertion,
            string authority)
        {
            GuardMobileFrameworks();

            Authority authorityInstance = Instance.Authority.CreateAuthority(ServiceBundle, authority, ValidateAuthority);
            return
                await
                    AcquireTokenOnBehalfCommonAsync(authorityInstance, scopes, userAssertion, ApiEvent.ApiIds.AcquireTokenOnBehalfOfWithScopeUserAuthority, true)
                        .ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires a security token from the authority configured in the app using the authorization code previously received from the STS. It uses
        /// the OAuth 2.0 authorization code flow (See https://aka.ms/msal-net-authorization-code).
        /// It's usually used in Web Apps (for instance ASP.NET / ASP.NET Core Web apps) which sign-in users, and therefore receive an authorization code.
        /// This method does not lookup the token cache, but stores the result in it, so it can be looked up using other methods 
        /// such as <see cref="IClientApplicationBase.AcquireTokenSilentAsync(IEnumerable{string}, IAccount)"/>.
        /// </summary>
        /// <param name="authorizationCode">The authorization code received from service authorization endpoint.</param>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, IEnumerable<string> scopes)
        {
            GuardMobileFrameworks();

            return
                await
                    AcquireTokenByAuthorizationCodeCommonAsync(
                        authorizationCode, scopes, new Uri(RedirectUri),
                        ApiEvent.ApiIds.AcquireTokenByAuthorizationCodeWithCodeScope, false).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires a token from the authority configured in the app, for the confidential client itself (in the name of no user)
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

            return
                await
                    AcquireTokenForClientCommonAsync(scopes, false, ApiEvent.ApiIds.AcquireTokenForClientWithScope, false).ConfigureAwait(false);
        }
        /// <summary>
        /// Acquires a token from the authority configured in the app, for the confidential client itself (in the name of no user)
        /// using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API. For this flow (client credentials), the scopes
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are really defined statically at application registration 
        /// in the portal, and cannot be overriden in the application</param>
        /// <param name="forceRefresh">If <c>true</c>, API will ignore the access token in the cache and attempt to acquire new access token using client credentials.
        /// This override can be used in case the application knows that conditional access policies changed</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        public async Task<AuthenticationResult> AcquireTokenForClientAsync(IEnumerable<string> scopes, bool forceRefresh)
        {
            GuardMobileFrameworks();

            return
                await
                    AcquireTokenForClientCommonAsync(scopes, forceRefresh, ApiEvent.ApiIds.AcquireTokenForClientWithScopeRefresh, false).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires token from the service for the confidential client using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
        /// This method enables application developers to achieve easy certificate roll-over
        /// in Azure AD: this method will send the public certificate to Azure AD
        /// along with the token request, so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or powershell/CLI operation)
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>Authentication result containing application token for the requested scopes</returns>
        async Task<AuthenticationResult> IConfidentialClientApplicationWithCertificate.AcquireTokenForClientWithCertificateAsync(IEnumerable<string> scopes)
        {
            GuardMobileFrameworks();

            return
                await
                    AcquireTokenForClientCommonAsync(scopes, false, ApiEvent.ApiIds.AcquireTokenForClientWithScope, true).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires token from the service for the confidential client using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
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
        async Task<AuthenticationResult> IConfidentialClientApplicationWithCertificate.AcquireTokenForClientWithCertificateAsync(IEnumerable<string> scopes, bool forceRefresh)
        {
            GuardMobileFrameworks();

            return
                await
                    AcquireTokenForClientCommonAsync(scopes, forceRefresh, ApiEvent.ApiIds.AcquireTokenForClientWithScopeRefresh, true).ConfigureAwait(false);
        }

        /// <summary>
        /// Computes the URL of the authorization request letting the user sign-in and consent to the application accessing specific scopes in 
        /// the user's name. The URL targets the /authorize endpoint of the authority configured in the application. 
        /// This override enables you to specify a login hint and extra query parameter.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally a UPN. This can be empty</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. 
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <returns>URL of the authorize endpoint including the query parameters.</returns>
        public async Task<Uri> GetAuthorizationRequestUrlAsync(IEnumerable<string> scopes, string loginHint,
            string extraQueryParameters)
        {
            GuardMobileFrameworks();

            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle, Authority, ValidateAuthority);
            var requestParameters =
                CreateRequestParameters(authority, scopes, null, UserTokenCache);
            requestParameters.ClientId = ClientId;
            requestParameters.ExtraQueryParameters = extraQueryParameters;

            var handler =
                new InteractiveRequest(ServiceBundle, requestParameters, ApiEvent.ApiIds.None, null, loginHint, UIBehavior.SelectAccount, null);
            return await handler.CreateAuthorizationUriAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Computes the URL of the authorization request letting the user sign-in and consent to the application accessing specific scopes in 
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
        public async Task<Uri> GetAuthorizationRequestUrlAsync(IEnumerable<string> scopes, string redirectUri, string loginHint,
            string extraQueryParameters, IEnumerable<string> extraScopesToConsent, string authority)
        {
            GuardMobileFrameworks();

            Authority authorityInstance = Instance.Authority.CreateAuthority(ServiceBundle, authority, ValidateAuthority);
            var requestParameters = CreateRequestParameters(authorityInstance, scopes, null,
                UserTokenCache);
            requestParameters.RedirectUri = new Uri(redirectUri);
            requestParameters.ClientId = ClientId;
            requestParameters.ExtraQueryParameters = extraQueryParameters;

            var handler = new InteractiveRequest(
                ServiceBundle,
                requestParameters,
                ApiEvent.ApiIds.None,
                extraScopesToConsent,
                loginHint,
                UIBehavior.SelectAccount,
                null);

            return await handler.CreateAuthorizationUriAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// This method should be used when you have a solution using ADAL 2.x and 
        /// caching a refresh token, and you want to migrate to MSAL.NET.
        /// During the migration process, it enables you to store in MSAL.NET token
        ///  cache an access token and refresh token corresponding to <paramref="refreshToken"/>.
        /// From there you will be able to use MSAL.NET new API, in particular
        /// AcquireTokenSilentAsync() which will renew the user token.  
        /// </summary>
        async Task<AuthenticationResult> IByRefreshToken.AcquireTokenByRefreshTokenAsync(IEnumerable<string> scopes, string refreshToken)
        {
            GuardMobileFrameworks();

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentNullException(nameof(refreshToken), "A refresh token must be provided.");
            }

            return await ExchangeRefreshTokenAsync(scopes, refreshToken).ConfigureAwait(false);
        }

        internal ClientCredential ClientCredential { get; }

        private TokenCache _appTokenCache;
        internal TokenCache AppTokenCache
        {
            get => _appTokenCache;
            private set
            {
                _appTokenCache = value;
                if (_appTokenCache != null)
                {
                    _appTokenCache.ClientId = ClientId;
                    _appTokenCache.ServiceBundle = ServiceBundle;
                }
            }
        }

        private async Task<AuthenticationResult> AcquireTokenForClientCommonAsync(IEnumerable<string> scopes, bool forceRefresh, ApiEvent.ApiIds apiId, bool sendCertificate)
        {
            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle, Authority, ValidateAuthority);
            AuthenticationRequestParameters parameters = CreateRequestParameters(authority, scopes, null,
                AppTokenCache);
            parameters.IsClientCredentialRequest = true;
            parameters.SendCertificate = sendCertificate;
            var handler = new ClientCredentialRequest(
                ServiceBundle,
                parameters,
                apiId,
                forceRefresh);

            return await handler.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenOnBehalfCommonAsync(Authority authority,
            IEnumerable<string> scopes, UserAssertion userAssertion, ApiEvent.ApiIds apiId, bool sendCertificate)
        {
            var requestParams = CreateRequestParameters(authority, scopes, null, UserTokenCache);
            requestParams.UserAssertion = userAssertion;
            requestParams.SendCertificate = sendCertificate;
            var handler = new OnBehalfOfRequest(
                ServiceBundle,
                requestParams,
                apiId);
            return await handler.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeCommonAsync(string authorizationCode,
            IEnumerable<string> scopes, Uri redirectUri, ApiEvent.ApiIds apiId, bool sendCertificate)
        {
            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle, Authority, ValidateAuthority);
            var requestParams = CreateRequestParameters(authority, scopes, null, UserTokenCache);
            requestParams.AuthorizationCode = authorizationCode;
            requestParams.RedirectUri = redirectUri;
            requestParams.SendCertificate = sendCertificate;
            var handler = new AuthorizationCodeRequest(
                ServiceBundle,
                requestParams,
                apiId);
            return await handler.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }

        internal override AuthenticationRequestParameters CreateRequestParameters(Authority authority, IEnumerable<string> scopes, IAccount user, TokenCache cache)
        {
            AuthenticationRequestParameters parameters = base.CreateRequestParameters(authority, scopes, user, cache);
            parameters.ClientId = ClientId;
            parameters.ClientCredential = ClientCredential;

            return parameters;
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
