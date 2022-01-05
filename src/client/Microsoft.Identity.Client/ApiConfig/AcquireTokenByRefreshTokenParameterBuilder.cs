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

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();
            if (Parameters.SendX5C == null)
            {
                Parameters.SendX5C = this.ServiceBundle.Config.SendX5C;
            }
        }

        /// <inheritdoc />
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenByRefreshToken;
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
        public AcquireTokenByRefreshTokenParameterBuilder WithSendX5C(bool withSendX5C)
        {
            Parameters.SendX5C = withSendX5C;
            return this;
        }
    }
}
