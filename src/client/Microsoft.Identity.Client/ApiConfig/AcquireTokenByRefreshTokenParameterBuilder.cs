// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

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

#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide on mobile platforms

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
        public AcquireTokenByRefreshTokenParameterBuilder WithSendX5C(bool withSendX5C)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithSendX5C);
            Parameters.SendX5C = withSendX5C;
            return this;
        }
#endif
    }
}
