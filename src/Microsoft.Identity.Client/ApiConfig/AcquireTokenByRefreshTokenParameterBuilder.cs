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
    /// <summary>
    /// Parameter builder for the <see cref="IByRefreshToken.AcquireTokenByRefreshToken(IEnumerable{string}, string)"/>
    /// method. See https://aka.ms/msal-net-migration-adal2-msal2
    /// </summary>
    public sealed class AcquireTokenByRefreshTokenParameterBuilder :
        AbstractClientAppBaseAcquireTokenParameterBuilder<AcquireTokenByRefreshTokenParameterBuilder>
    {
        private AcquireTokenByRefreshTokenParameters Parameters { get; } = new AcquireTokenByRefreshTokenParameters();

        internal override ApiTelemetryId ApiTelemetryId => ApiTelemetryId.AcquireTokenByRefreshToken;

        /// <inheritdoc />
        internal AcquireTokenByRefreshTokenParameterBuilder(IClientApplicationBaseExecutor clientApplicationBaseExecutor)
            : base(clientApplicationBaseExecutor)
        {
        }

        internal static AcquireTokenByRefreshTokenParameterBuilder Create(
            IClientApplicationBaseExecutor clientApplicationBaseExecutor,
            IEnumerable<string> scopes,
            string refreshToken)
        {
            return new AcquireTokenByRefreshTokenParameterBuilder(clientApplicationBaseExecutor)
                   .WithScopes(scopes).WithRefreshToken(refreshToken);
        }

        internal AcquireTokenByRefreshTokenParameterBuilder WithRefreshToken(string refreshToken)
        {
            Parameters.RefreshToken = refreshToken;
            return this;
        }

        /// <inheritdoc />
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return ClientApplicationBaseExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        /// <inheritdoc />
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenByRefreshToken;
        }
    }
}
