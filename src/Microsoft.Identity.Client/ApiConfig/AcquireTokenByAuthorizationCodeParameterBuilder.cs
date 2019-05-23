// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

    /// <summary>
    /// Builder for AcquireTokenByAuthorizationCode
    /// </summary>
    public sealed class AcquireTokenByAuthorizationCodeParameterBuilder :
        AbstractConfidentialClientAcquireTokenParameterBuilder<AcquireTokenByAuthorizationCodeParameterBuilder>
    {
        private AcquireTokenByAuthorizationCodeParameters Parameters { get; } = new AcquireTokenByAuthorizationCodeParameters();

        internal override ApiTelemetryId ApiTelemetryId => ApiTelemetryId.AcquireTokenByAuthorizationCode;

        internal AcquireTokenByAuthorizationCodeParameterBuilder(IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor)
            : base(confidentialClientApplicationExecutor)
        {
            // TODO: where do we pass the authorization code? 
        }

        internal static AcquireTokenByAuthorizationCodeParameterBuilder Create(
            IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor,
            IEnumerable<string> scopes, 
            string authorizationCode)
        {
            return new AcquireTokenByAuthorizationCodeParameterBuilder(confidentialClientApplicationExecutor)
                   .WithScopes(scopes).WithAuthorizationCode(authorizationCode);
        }

        private AcquireTokenByAuthorizationCodeParameterBuilder WithAuthorizationCode(string authorizationCode)
        {
            Parameters.AuthorizationCode = authorizationCode;
            return this;
        }

        /// <inheritdoc />
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenByAuthorizationCodeV2;
        }

        /// <inheritdoc />
        protected override void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(Parameters.AuthorizationCode))
            {
                throw new ArgumentException("AuthorizationCode can not be null or whitespace", nameof(Parameters.AuthorizationCode));
            }
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return ConfidentialClientApplicationExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }
    }
#endif
}
