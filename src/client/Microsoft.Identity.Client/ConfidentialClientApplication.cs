// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Class to be used for confidential client applications (web apps, web APIs, and daemon applications).
    /// </summary>
    /// <remarks>
    /// Confidential client applications are typically applications which run on servers (web apps, web API, or even service/daemon applications).
    /// They are considered difficult to access, and therefore capable of keeping an application secret (hold configuration
    /// time secrets as these values would be difficult for end users to extract).
    /// A web app is the most common confidential client. The clientId is exposed through the web browser, but the secret is passed only in the back channel
    /// and never directly exposed. For details see https://aka.ms/msal-net-client-applications
    /// </remarks>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public sealed partial class ConfidentialClientApplication
        : ClientApplicationBase,
            IConfidentialClientApplication,
            IConfidentialClientApplicationWithCertificate,
            IByRefreshToken,
            ILongRunningWebApi
    {
        /// <summary>
        /// Instructs MSAL to try to auto discover the Azure region.
        /// </summary>
        public const string AttemptRegionDiscovery = "TryAutoDetect";

        internal ConfidentialClientApplication(
            ApplicationConfiguration configuration)
            : base(configuration)
        {
            GuardMobileFrameworks();

            AppTokenCacheInternal = configuration.AppTokenCacheInternalForTest ?? new TokenCache(ServiceBundle, true);
            Certificate = configuration.ClientCredentialCertificate;

            this.ServiceBundle.ApplicationLogger.Verbose(()=>$"ConfidentialClientApplication {configuration.GetHashCode()} created");
        }

        /// <summary>
        /// Acquires a security token from the authority configured in the app using the authorization code
        /// previously received from the STS.
        /// It uses the OAuth 2.0 authorization code flow (See https://aka.ms/msal-net-authorization-code).
        /// It's usually used in web apps (for instance ASP.NET / ASP.NET Core web apps) which sign-in users,
        /// and can request an authorization code.
        /// This method does not lookup the token cache, but stores the result in it, so it can be looked up
        /// using other methods such as <see cref="IClientApplicationBase.AcquireTokenSilent(IEnumerable{string}, IAccount)"/>.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="authorizationCode">The authorization code received from the service authorization endpoint.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>You can set optional parameters by chaining the builder with other .With methods.
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
        /// using the client credentials flow. See https://aka.ms/msal-net-client-credentials.
        /// </summary>
        /// <param name="scopes">scopes requested to access a protected API. For this flow (client credentials), the scopes
        /// should be of the form "{ResourceIdUri/.default}" for instance <c>https://management.azure.net/.default</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c> as the requested scopes are defined statically with the application registration
        /// in the portal, and cannot be overridden in the application.</param>
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
        /// in order to access another downstream protected web API on behalf of a user using the OAuth 2.0 On-Behalf-Of flow.
        /// See https://aka.ms/msal-net-on-behalf-of.
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
            if (userAssertion == null)
            {
                ServiceBundle.ApplicationLogger.Error("User assertion for OBO request should not be null");
                throw new MsalClientException(MsalError.UserAssertionNullError);
            }

            return AcquireTokenOnBehalfOfParameterBuilder.Create(
                ClientExecutorFactory.CreateConfidentialClientExecutor(this),
                scopes,
                userAssertion);
        }

        /// <inheritdoc />
        public AcquireTokenOnBehalfOfParameterBuilder InitiateLongRunningProcessInWebApi(
            IEnumerable<string> scopes,
            string userToken,
            ref string longRunningProcessSessionKey)
        {
            if (string.IsNullOrEmpty(userToken))
            {
                throw new ArgumentNullException(nameof(userToken));
            }

            UserAssertion userAssertion = new UserAssertion(userToken);

            if (string.IsNullOrEmpty(longRunningProcessSessionKey))
            {
                longRunningProcessSessionKey = userAssertion.AssertionHash;
            }

            return AcquireTokenOnBehalfOfParameterBuilder.Create(
                ClientExecutorFactory.CreateConfidentialClientExecutor(this),
                scopes,
                userAssertion,
                longRunningProcessSessionKey);
        }

        /// <inheritdoc />
        public AcquireTokenOnBehalfOfParameterBuilder AcquireTokenInLongRunningProcess(
            IEnumerable<string> scopes,
            string longRunningProcessSessionKey)
        {
            if (string.IsNullOrEmpty(longRunningProcessSessionKey))
            {
                throw new ArgumentNullException(nameof(longRunningProcessSessionKey));
            }

            return AcquireTokenOnBehalfOfParameterBuilder.Create(
                ClientExecutorFactory.CreateConfidentialClientExecutor(this),
                scopes,
                longRunningProcessSessionKey);
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

        /// <summary>
        /// Application token cache. This case holds access tokens for the application. It's maintained
        /// and updated silently if needed when calling <see cref="AcquireTokenForClient(IEnumerable{string})"/>
        /// </summary>
        /// <remarks>On .NET Framework and .NET Core you can also customize the token cache serialization.
        /// See https://aka.ms/msal-net-token-cache-serialization. This is taken care of by MSAL.NET on other platforms
        /// </remarks>
        public ITokenCache AppTokenCache => AppTokenCacheInternal;

        /// <summary>
        /// The certificate used to create this <see cref="ConfidentialClientApplication"/>, if any.
        /// </summary>
        public X509Certificate2 Certificate { get; }

        // Stores all app tokens
        internal ITokenCacheInternal AppTokenCacheInternal { get; }

        internal override async Task<AuthenticationRequestParameters> CreateRequestParametersAsync(
            AcquireTokenCommonParameters commonParameters,
            RequestContext requestContext,
            ITokenCacheInternal cache)
        {
            AuthenticationRequestParameters requestParams = await base.CreateRequestParametersAsync(commonParameters, requestContext, cache).ConfigureAwait(false);
            return requestParams;
        }
    }
}
