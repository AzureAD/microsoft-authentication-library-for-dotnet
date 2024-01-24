// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client
{
    /// <inheritdoc/>
    /// <summary>
    /// Parameter builder for the <see cref="IClientApplicationBase.AcquireTokenSilent(IEnumerable{string}, IAccount)"/>
    /// operation. See https://aka.ms/msal-net-acquiretokensilent
    /// </summary>
    public sealed class AcquireTokenSilentParameterBuilder :
        AbstractClientAppBaseAcquireTokenParameterBuilder<AcquireTokenSilentParameterBuilder>
    {
        private AcquireTokenSilentParameters Parameters { get; } = new AcquireTokenSilentParameters();

        internal AcquireTokenSilentParameterBuilder(IClientApplicationBaseExecutor clientApplicationBaseExecutor)
            : base(clientApplicationBaseExecutor)
        {
        }

        internal static AcquireTokenSilentParameterBuilder Create(
            IClientApplicationBaseExecutor clientApplicationBaseExecutor,
            IEnumerable<string> scopes,
            IAccount account)
        {
            return new AcquireTokenSilentParameterBuilder(clientApplicationBaseExecutor).WithScopes(scopes).WithAccount(account);
        }

        internal static AcquireTokenSilentParameterBuilder Create(
            IClientApplicationBaseExecutor clientApplicationBaseExecutor,
            IEnumerable<string> scopes,
            string loginHint)
        {
            return new AcquireTokenSilentParameterBuilder(clientApplicationBaseExecutor).WithScopes(scopes).WithLoginHint(loginHint);
        }

        /// <summary>
        /// Sets the account for which the token will be retrieved. This method is mutually exclusive
        /// with <see cref="WithLoginHint(string)"/>. If both are used, an exception will be thrown
        /// </summary>
        /// <param name="account">Account to use for the silent token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <remarks>An exception will be thrown If AAD returns a different account than the one that is being requested for.</remarks>
        private AcquireTokenSilentParameterBuilder WithAccount(IAccount account)
        {
            Parameters.Account = account;
            return this;
        }

        private AcquireTokenSilentParameterBuilder WithLoginHint(string loginHint)
        {
            Parameters.LoginHint = loginHint;
            return this;
        }

        /// <summary>
        /// Specifies if the client application should force refreshing the
        /// token from the user token cache. By default the token is taken from the
        /// the user token cache (forceRefresh=false)
        /// </summary>
        /// <param name="forceRefresh">If <c>true</c>, ignore any access token in the user token cache
        /// and attempt to acquire new access token using the refresh token for the account
        /// if one is available. This can be useful in the case when the application developer wants to make
        /// sure that conditional access policies are applied immediately, rather than after the expiration of the access token.
        /// The default is <c>false</c></param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <remarks>Avoid unnecessarily setting <paramref name="forceRefresh"/> to <c>true</c> true in order to
        /// avoid negatively affecting the performance of your application</remarks>
        public AcquireTokenSilentParameterBuilder WithForceRefresh(bool forceRefresh)
        {
            Parameters.ForceRefresh = forceRefresh;
            return this;
        }

        /// <inheritdoc/>
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return ClientApplicationBaseExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();
            if (Parameters.SendX5C == null)
            {
                Parameters.SendX5C = this.ServiceBundle.Config.SendX5C;
            }

            // During AT Silent with no scopes, Unlike AAD, B2C will not issue an access token if no scopes are requested
            // And we don't want to refresh the RT on every ATS call
            // See https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/715 for details
            if (ServiceBundle.Config.Authority.AuthorityInfo.AuthorityType == AuthorityType.B2C &&
                (CommonParameters.Scopes == null ||
                 CommonParameters.Scopes.All(string.IsNullOrWhiteSpace)))
            {
                throw new MsalUiRequiredException(
                   MsalError.ScopesRequired,
                   MsalErrorMessage.ScopesRequired,
                   null,
                   UiRequiredExceptionClassification.AcquireTokenSilentFailed);
            }
        }

        /// <inheritdoc/>
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenSilent;
        }

        /// <summary>
        /// Applicable to first-party applications only, this method also allows to specify 
        /// if the <see href="https://datatracker.ietf.org/doc/html/rfc7517#section-4.7">x5c claim</see> should be sent to Azure AD.
        /// Sending the x5c enables application developers to achieve easy certificate roll-over in Azure AD:
        /// this method will send the certificate chain to Azure AD along with the token request,
        /// so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or PowerShell/CLI operation). For details see https://aka.ms/msal-net-sni
        /// </summary>
        /// <param name="withSendX5C"><c>true</c> if the x5c should be sent. Otherwise <c>false</c>.
        /// The default is <c>false</c></param>
        /// <returns>The builder to chain the .With methods</returns>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
        public AcquireTokenSilentParameterBuilder WithSendX5C(bool withSendX5C)
        {
            Parameters.SendX5C = withSendX5C;
            return this;
        }

        /// <summary>
        ///  Modifies the token acquisition request so that the acquired token is a Proof-of-Possession token (PoP), rather than a Bearer token. 
        ///  PoP tokens are similar to Bearer tokens, but are bound to the HTTP request and to a cryptographic key, which MSAL can manage on Windows.
        ///  See https://aka.ms/msal-net-pop
        /// </summary>
        /// <param name="popAuthenticationConfiguration">Configuration properties used to construct a Proof-of-Possession request.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>An Authentication header is automatically added to the request.</description></item>
        /// <item><description>The PoP token is bound to the HTTP request, more specifically to the HTTP method (GET, POST, etc.) and to the Uri (path and query, but not query parameters).</description></item>
        /// <item><description>MSAL creates, reads and stores a key in memory that will be cycled every 8 hours.</description></item>
        /// <item><description>This is an experimental API. The method signature may change in the future without involving a major version upgrade.</description></item>
        /// </list>
        /// </remarks>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide on public client
#endif
        public AcquireTokenSilentParameterBuilder WithProofOfPossession(PoPAuthenticationConfiguration popAuthenticationConfiguration)
        {
            ClientApplicationBase.GuardMobileFrameworks();
            ValidateUseOfExperimentalFeature();

            CommonParameters.PopAuthenticationConfiguration = popAuthenticationConfiguration ?? throw new ArgumentNullException(nameof(popAuthenticationConfiguration));

            CommonParameters.AuthenticationScheme = new PopAuthenticationScheme(CommonParameters.PopAuthenticationConfiguration, ServiceBundle);

            return this;
        }

        /// <summary>
        ///  Modifies the token acquisition request so that the acquired token is a Proof-of-Possession token (PoP), rather than a Bearer token. 
        ///  PoP tokens are similar to Bearer tokens, but are bound to the HTTP request and to a cryptographic key, which MSAL can manage on Windows.
        ///  Note that only the host and path parts of the request URI will be bound.
        ///  See https://aka.ms/msal-net-pop
        /// </summary>
        /// <param name="nonce">Nonce of the protected resource (RP) which will be published as part of the WWWAuthenticate header associated with a 401 HTTP response
        /// or as part of the AuthorityInfo header associated with 200 response. Set it here to make it part of the Signed HTTP Request part of the POP token.</param>
        /// <param name="httpMethod">The HTTP method ("GET", "POST" etc.) method that will be bound to the token. If set to null, the PoP token will not be bound to the method.
        /// Corresponds to the "m" part of the a signed HTTP request.</param>
        /// <param name="requestUri">The URI to bind the signed HTTP request to.</param>
        /// <returns>The builder.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>An Authentication header is automatically added to the request.</description></item>
        /// <item><description>The PoP token is bound to the HTTP request, more specifically to the HTTP method (GET, POST, etc.) and to the Uri (path and query, but not query parameters).</description></item>
        /// <item><description>MSAL creates, reads and stores a key in memory that will be cycled every 8 hours.</description></item>
        /// <item><description>On confidential clients, this is an experimental API. The method signature may change in the future without involving a major version upgrade.</description></item>
        /// <item><description>Broker is required to use Proof-of-Possession on public clients.</description></item>
        /// </list>
        /// </remarks>
#if iOS || ANDROID || WINDOWS_UWP
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public AcquireTokenSilentParameterBuilder WithProofOfPossession(string nonce, HttpMethod httpMethod, Uri requestUri)
        {
            if (ServiceBundle.Config.IsConfidentialClient)
            {
                ValidateUseOfExperimentalFeature();
            }

            // On public client, we only support POP via the broker
            if (!ServiceBundle.Config.IsConfidentialClient &&
                !ServiceBundle.Config.IsBrokerEnabled)
            {
                throw new MsalClientException(MsalError.BrokerRequiredForPop, MsalErrorMessage.BrokerRequiredForPop);
            }

            ClientApplicationBase.GuardMobileFrameworks();
            var broker = ServiceBundle.PlatformProxy.CreateBroker(ServiceBundle.Config, null);

            if (ServiceBundle.Config.IsBrokerEnabled)
            {
                if (string.IsNullOrEmpty(nonce))
                {
                    throw new ArgumentNullException(nameof(nonce));
                }

                if (!broker.IsPopSupported)
                {
                    throw new MsalClientException(MsalError.BrokerDoesNotSupportPop, MsalErrorMessage.BrokerDoesNotSupportPop);
                }
            }

            PoPAuthenticationConfiguration popConfig = new PoPAuthenticationConfiguration(requestUri ?? throw new ArgumentNullException(nameof(requestUri)));
            popConfig.HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
            popConfig.Nonce = nonce;

            IAuthenticationScheme authenticationScheme;

            //POP Auth scheme should not wrap and sign token when broker is enabled for public clients
            if (ServiceBundle.Config.IsBrokerEnabled)
            {
                popConfig.SignHttpRequest = false;
                authenticationScheme = new PopBrokerAuthenticationScheme();
            }
            else
            {
                authenticationScheme = new PopAuthenticationScheme(popConfig, ServiceBundle);
            }
            CommonParameters.PopAuthenticationConfiguration = popConfig;
            CommonParameters.AuthenticationScheme = authenticationScheme;

            return this;
        }
    }
}
