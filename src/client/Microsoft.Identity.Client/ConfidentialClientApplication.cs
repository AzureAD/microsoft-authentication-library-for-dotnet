// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using static Microsoft.Identity.Client.TelemetryCore.Internal.Events.ApiEvent;

namespace Microsoft.Identity.Client
{
    /// <inheritdoc cref="IConfidentialClientApplication"/>
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

            this.ServiceBundle.ApplicationLogger.Verbose(() => $"ConfidentialClientApplication {configuration.GetHashCode()} created");
        }

        /// <inheritdoc/>
        public AcquireTokenByAuthorizationCodeParameterBuilder AcquireTokenByAuthorizationCode(
            IEnumerable<string> scopes,
            string authorizationCode)
        {
            return AcquireTokenByAuthorizationCodeParameterBuilder.Create(
                ClientExecutorFactory.CreateConfidentialClientExecutor(this),
                scopes,
                authorizationCode);
        }

        /// <inheritdoc/>
        public AcquireTokenForClientParameterBuilder AcquireTokenForClient(
            IEnumerable<string> scopes)
        {
            return AcquireTokenForClientParameterBuilder.Create(
                ClientExecutorFactory.CreateConfidentialClientExecutor(this),
                scopes);
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
        /// Stops an in-progress long-running on-behalf-of session by removing the tokens associated with the provided cache key.
        /// See <see href="https://aka.ms/msal-net-long-running-obo">Long-running OBO in MSAL.NET</see>.
        /// </summary>
        /// <param name="longRunningProcessSessionKey">OBO cache key used to remove the tokens.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if tokens are removed from the cache; false, otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="longRunningProcessSessionKey"/> is not set.</exception>
        public async Task<bool> StopLongRunningProcessInWebApiAsync(string longRunningProcessSessionKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(longRunningProcessSessionKey))
            {
                throw new ArgumentNullException(nameof(longRunningProcessSessionKey));
            }

            Guid correlationId = Guid.NewGuid();
            RequestContext requestContext = base.CreateRequestContext(correlationId, cancellationToken);
            requestContext.ApiEvent = new ApiEvent(correlationId);
            requestContext.ApiEvent.ApiId = ApiIds.RemoveOboTokens;

            var authority = await Instance.Authority.CreateAuthorityForRequestAsync(
              requestContext,
              null).ConfigureAwait(false);

            var authParameters = new AuthenticationRequestParameters(
                   ServiceBundle,
                   UserTokenCacheInternal,
                   new AcquireTokenCommonParameters() { ApiId = requestContext.ApiEvent.ApiId },
                   requestContext,
                   authority);

            if (UserTokenCacheInternal != null)
            {
                return await UserTokenCacheInternal.StopLongRunningOboProcessAsync(longRunningProcessSessionKey, authParameters).ConfigureAwait(false);
            }

            return false;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
