﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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

        internal override ApiTelemetryId ApiTelemetryId => ApiTelemetryId.AcquireTokenByIntegratedWindowsAuth;

        /// <inheritdoc />
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
        /// <param name="username">Identifier of the user account for which to acquire a token with 
        /// Integrated Windows authentication. Generally in UserPrincipalName (UPN) format, 
        /// e.g. <c>john.doe@contoso.com</c></param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenByIntegratedWindowsAuthParameterBuilder WithUsername(string username)
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithUsername);
            Parameters.Username = username;
            return this;
        }

        /// <summary>
        /// Enables MSAL to read the federation metadata for a wstrust exchange from a file instead of acquiring it from an endpoint.
        /// See aka.ms/MsalFederationMetadata
        /// </summary>
        /// <param name="fileName">Path to federation metada xml file.</param>
        /// <returns></returns>
        public AcquireTokenByIntegratedWindowsAuthParameterBuilder WithFederationMetadata(string fileName)
        {
            CommonParameters.FederationMetadataFilePath = fileName;
            return this;
        }

        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return PublicClientApplicationExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        /// <inheritdoc />
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenByIntegratedWindowsAuth;
        }
    }
}
