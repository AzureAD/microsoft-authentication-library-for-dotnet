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
    /// Builder for AcquireTokenByIntegratedWindowsAuth
    /// </summary>
    public sealed class AcquireTokenByIntegratedWindowsAuthParameterBuilder :
        AbstractPublicClientAcquireTokenParameterBuilder<AcquireTokenByIntegratedWindowsAuthParameterBuilder>
    {
        private AcquireTokenByIntegratedWindowsAuthParameters Parameters { get; } = new AcquireTokenByIntegratedWindowsAuthParameters();

        /// <inheritdoc/>
        internal AcquireTokenByIntegratedWindowsAuthParameterBuilder(IPublicClientApplicationExecutor publicClientApplicationExecutor)
            : base(publicClientApplicationExecutor)
        {
        }

        internal static AcquireTokenByIntegratedWindowsAuthParameterBuilder Create(
            IPublicClientApplicationExecutor publicClientApplicationExecutor, IEnumerable<string> scopes)
        {
            return new AcquireTokenByIntegratedWindowsAuthParameterBuilder(publicClientApplicationExecutor).WithScopes(scopes);
        }

        /// <summary>
        /// Specifies the username.
        /// </summary>
        /// <remarks>
        /// Specifying the username explicitly is normally not needed, but some Windows administrators
        /// set policies preventing applications from looking up the signed-in user and in that case the username needs to be passed.
        /// </remarks>
        /// <param name="username">Identifier of the user account for which to acquire a token with 
        /// Integrated Windows Authentication. Generally in UserPrincipalName (UPN) format, 
        /// e.g. <c>john.doe@contoso.com</c></param>
        /// <returns>The builder to chain the .With methods.</returns>
        public AcquireTokenByIntegratedWindowsAuthParameterBuilder WithUsername(string username)
        {
            Parameters.Username = username;
            return this;
        }

        /// <summary>
        /// Enables MSAL to read the federation metadata for a WS-Trust exchange from the provided input instead of acquiring it from an endpoint.
        /// This is only applicable for managed ADFS accounts. See https://aka.ms/MsalFederationMetadata.
        /// </summary>
        /// <param name="federationMetadata">Federation metadata in the form of XML.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenByIntegratedWindowsAuthParameterBuilder WithFederationMetadata(string federationMetadata)
        {
            Parameters.FederationMetadata = federationMetadata;
            return this;
        }

        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return PublicClientApplicationExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        /// <inheritdoc/>
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenByIntegratedWindowsAuth;
        }
    }
}
