// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Mats.Internal.Events;

namespace Microsoft.Identity.Client
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

    /// <summary>
    /// Builder for AcquireTokenOnBehalfOf (OBO flow)
    /// See https://aka.ms/msal-net-on-behalf-of
    /// </summary>
    public sealed class AcquireTokenOnBehalfOfParameterBuilder :
        AbstractConfidentialClientAcquireTokenParameterBuilder<AcquireTokenOnBehalfOfParameterBuilder>
    {
        private AcquireTokenOnBehalfOfParameters Parameters { get; } = new AcquireTokenOnBehalfOfParameters();

        internal override ApiTelemetryId ApiTelemetryId => ApiTelemetryId.AcquireTokenOnBehalfOf;

        /// <inheritdoc />
        internal AcquireTokenOnBehalfOfParameterBuilder(IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor)
            : base(confidentialClientApplicationExecutor)
        {
        }

        internal static AcquireTokenOnBehalfOfParameterBuilder Create(
            IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor,
            IEnumerable<string> scopes, 
            UserAssertion userAssertion)
        {
            return new AcquireTokenOnBehalfOfParameterBuilder(confidentialClientApplicationExecutor)
                   .WithScopes(scopes)
                   .WithUserAssertion(userAssertion);
        }

        private AcquireTokenOnBehalfOfParameterBuilder WithUserAssertion(UserAssertion userAssertion)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithUserAssertion);
            Parameters.UserAssertion = userAssertion;
            return this;
        }

        /// <summary>
        /// Specifies if the x5c claim (public key of the certificate) should be sent to the STS.
        /// Sending the x5c enables application developers to achieve easy certificate roll-over in Azure AD:
        /// this method will send the public certificate to Azure AD along with the token request,
        /// so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or powershell/CLI operation)
        /// </summary>
        /// <param name="withSendX5C"><c>true</c> if the x5c should be sent. Otherwise <c>false</c>.
        /// The default is <c>false</c></param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenOnBehalfOfParameterBuilder WithSendX5C(bool withSendX5C)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithSendX5C);
            Parameters.SendX5C = withSendX5C;
            return this;
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return ConfidentialClientApplicationExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        /// <inheritdoc />
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return CommonParameters.AuthorityOverride == null
                       ? ApiEvent.ApiIds.AcquireTokenOnBehalfOfWithScopeUser
                       : ApiEvent.ApiIds.AcquireTokenOnBehalfOfWithScopeUserAuthority;
        }
    }
#endif
}
