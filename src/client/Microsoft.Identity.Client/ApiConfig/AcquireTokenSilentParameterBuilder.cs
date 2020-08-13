// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client
{
    /// <inheritdoc />
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


        private AcquireTokenSilentParameterBuilder WithAccount(IAccount account)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithAccount);
            Parameters.Account = account;
            return this;
        }

        private AcquireTokenSilentParameterBuilder WithLoginHint(string loginHint)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithLoginHint);
            Parameters.LoginHint = loginHint;
            return this;
        }


        /// <summary>
        /// Specifies if the client application should force refreshing the
        /// token from the user token cache. By default the token is taken from the
        /// the application token cache (forceRefresh=false)
        /// </summary>
        /// <param name="forceRefresh">If <c>true</c>, ignore any access token in the user token cache
        /// and attempt to acquire new access token using the refresh token for the account
        /// if one is available. This can be useful in the case when the application developer wants to make
        /// sure that conditional access policies are applied immediately, rather than after the expiration of the access token.
        /// The default is <c>false</c></param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <remarks>Avoid un-necessarily setting <paramref name="forceRefresh"/> to <c>true</c> true in order to
        /// avoid negatively affecting the performance of your application</remarks>
        public AcquireTokenSilentParameterBuilder WithForceRefresh(bool forceRefresh)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithForceRefresh, forceRefresh);
            Parameters.ForceRefresh = forceRefresh;
            return this;
        }

#if DESKTOP || NET_CORE
        /// <summary>
        ///  Modifies the token acquisition request so that the acquired token is a Proof of Possession token (PoP), rather than a Bearer token. 
        ///  PoP tokens are similar to Bearer tokens, but are bound to the HTTP request and to a cryptographic key, which MSAL can manage on Windows.
        ///  See https://aka.ms/msal-net-pop
        /// </summary>
        /// <param name="httpRequestMessage">An HTTP request to the protected resource which requires a PoP token. The PoP token will be cryptographically bound to the request.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>This is an experimental API. The method signature may change in the future without involving a major version upgrade.</item>
        /// <item> An Authentication header is automatically added to the request</item>
        /// <item> The PoP token is bound to the HTTP request, more specifically to the HTTP method (GET, POST, etc.) and to the Uri (path and query, but not query parameters). </item>
        /// <item> MSAL creates, reads and stores a key securely on behalf of the Windows user. </item>
        /// </list>
        /// </remarks>
        public AcquireTokenSilentParameterBuilder WithProofOfPosession(HttpRequestMessage httpRequestMessage)
        {
            var defaultCryptoProvider = this.ClientApplicationBaseExecutor.ServiceBundle.PlatformProxy.GetDefaultPoPCryptoProvider();
            return WithProofOfPosession(httpRequestMessage, defaultCryptoProvider);
        }

        // Allows testing the PoP flow with any crypto. Consider making this public.
        internal AcquireTokenSilentParameterBuilder WithProofOfPosession(HttpRequestMessage httpRequestMessage, IPoPCryptoProvider popCryptoProvider)
        {
            if (!this.ClientApplicationBaseExecutor.ServiceBundle.Config.ExperimentalFeaturesEnabled)
            {
                throw new MsalClientException(
                    MsalError.ExperimentalFeature,
                    MsalErrorMessage.ExperimentalFeature(nameof(WithProofOfPosession)));
            }

            if (httpRequestMessage is null)
            {
                throw new ArgumentNullException(nameof(httpRequestMessage));
            }

            if (popCryptoProvider == null)
            {
                throw new ArgumentNullException(nameof(popCryptoProvider));
            }

            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithPoPScheme);
            CommonParameters.AuthenticationScheme = new PoPAuthenticationScheme(httpRequestMessage, popCryptoProvider);

            return this;
        }
#endif

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return ClientApplicationBaseExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        /// <inheritdoc />
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenSilent;
        }


        internal override ApiTelemetryId ApiTelemetryId => ApiTelemetryId.AcquireTokenSilent;

#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

        /// <summary>
        /// Specifies if the x5c claim (public key of the certificate) should be sent to the STS.
        /// Sending the x5c enables application developers to achieve easy certificate roll-over in Azure AD:
        /// this method will send the public certificate to Azure AD along with the token request,
        /// so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or powershell/CLI operation). For details see https://aka.ms/msal-net-sni
        /// </summary>
        /// <param name="withSendX5C"><c>true</c> if the x5c should be sent. Otherwise <c>false</c>.
        /// The default is <c>false</c></param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenSilentParameterBuilder WithSendX5C(bool withSendX5C)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithSendX5C);
            Parameters.SendX5C = withSendX5C;
            return this;
        }

#endif

        /// <summary>
        /// 
        /// </summary>
        protected override void Validate()
        {
            base.Validate();
            if (Parameters.Account == null && string.IsNullOrWhiteSpace(Parameters.LoginHint))
            {
                throw new MsalUiRequiredException(
                    MsalError.UserNullError, 
                    MsalErrorMessage.MsalUiRequiredMessage, 
                    null, 
                    UiRequiredExceptionClassification.AcquireTokenSilentFailed);
            }
            
            if (Parameters.Account?.HomeAccountId == null && string.IsNullOrEmpty(Parameters.Account?.Username) && string.IsNullOrWhiteSpace(Parameters.LoginHint))
            {
                throw new MsalUiRequiredException(
                    MsalError.UserNullError,
                    MsalErrorMessage.MsalUiRequiredMessage,
                    null,
                    UiRequiredExceptionClassification.AcquireTokenSilentFailed);
            }
        }
    }
}
