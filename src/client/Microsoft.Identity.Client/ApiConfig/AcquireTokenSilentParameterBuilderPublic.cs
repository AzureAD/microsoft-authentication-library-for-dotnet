// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client
{
    /// <inheritdoc/>
    public sealed class AcquireTokenSilentParameterBuilderPublic :
        AbstractPublicClientAcquireTokenParameterBuilder<AcquireTokenSilentParameterBuilderPublic>
    {
        private AcquireTokenSilentParameters Parameters { get; } = new AcquireTokenSilentParameters();

        internal AcquireTokenSilentParameterBuilderPublic(IPublicClientApplicationExecutor publicClientApplicationExecutor)
            : base(publicClientApplicationExecutor)
        {
        }

        internal static AcquireTokenSilentParameterBuilderPublic Create(
            IPublicClientApplicationExecutor publicClientApplicationExecutor,
            IEnumerable<string> scopes,
            IAccount account)
        {
            return new AcquireTokenSilentParameterBuilderPublic(publicClientApplicationExecutor).WithScopes(scopes).WithAccount(account);
        }

        internal static AcquireTokenSilentParameterBuilderPublic Create(
            IPublicClientApplicationExecutor publicClientApplicationExecutor,
            IEnumerable<string> scopes,
            string loginHint)
        {
            return new AcquireTokenSilentParameterBuilderPublic(publicClientApplicationExecutor).WithScopes(scopes).WithLoginHint(loginHint);
        }

        private AcquireTokenSilentParameterBuilderPublic WithAccount(IAccount account)
        {
            Parameters.Account = account;
            return this;
        }

        private AcquireTokenSilentParameterBuilderPublic WithLoginHint(string loginHint)
        {
            Parameters.LoginHint = loginHint;
            return this;
        }

        /// <inheritdoc/>
        public AcquireTokenSilentParameterBuilderPublic WithForceRefresh(bool forceRefresh)
        {
            Parameters.ForceRefresh = forceRefresh;
            return this;
        }

        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return PublicClientApplicationExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
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
                 CommonParameters.Scopes.All(s => string.IsNullOrWhiteSpace(s))))
            {
                throw new MsalUiRequiredException(
                   MsalError.ScopesRequired,
                   MsalErrorMessage.ScopesRequired,
                   null,
                   UiRequiredExceptionClassification.AcquireTokenSilentFailed);
            }
        }

        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenSilent;
        }

        /// <inheritdoc/>
        public AcquireTokenSilentParameterBuilderPublic WithSendX5C(bool withSendX5C)
        {
            Parameters.SendX5C = withSendX5C;
            return this;
        }

        /// <inheritdoc/>
        public AcquireTokenSilentParameterBuilderPublic WithProofOfPossession(PoPAuthenticationConfiguration popAuthenticationConfiguration)
        {
            ClientApplicationBase.GuardMobileFrameworks();
            ValidateUseOfExperimentalFeature();

            CommonParameters.PopAuthenticationConfiguration = popAuthenticationConfiguration ?? throw new ArgumentNullException(nameof(popAuthenticationConfiguration));

            CommonParameters.AuthenticationScheme = new PopAuthenticationScheme(CommonParameters.PopAuthenticationConfiguration, ServiceBundle);

            return this;
        }

        /// <inheritdoc/>
        public AcquireTokenSilentParameterBuilderPublic WithProofOfPossession(string nonce, HttpMethod httpMethod, Uri requestUri)
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
            var broker = ServiceBundlePublic.PlatformProxyPublic.CreateBroker(ServiceBundlePublic.ConfigPublic, null);

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
