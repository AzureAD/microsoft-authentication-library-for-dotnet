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
    /// <summary>
    /// Builder for AcquireTokenForAgent, used to acquire tokens for agent scenarios involving
    /// Federated Managed Identity (FMI) and User Federated Identity Credentials (UserFIC).
    /// This orchestrates the multi-leg token acquisition automatically.
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public sealed class AcquireTokenForAgentParameterBuilder :
        AbstractConfidentialClientAcquireTokenParameterBuilder<AcquireTokenForAgentParameterBuilder>
    {
        internal AcquireTokenForAgentParameters Parameters { get; } = new AcquireTokenForAgentParameters();

        /// <inheritdoc/>
        internal AcquireTokenForAgentParameterBuilder(
            IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor,
            AgentIdentity agentIdentity)
            : base(confidentialClientApplicationExecutor)
        {
            Parameters.AgentIdentity = agentIdentity;
        }

        internal static AcquireTokenForAgentParameterBuilder Create(
            IConfidentialClientApplicationExecutor confidentialClientApplicationExecutor,
            IEnumerable<string> scopes,
            AgentIdentity agentIdentity)
        {
            if (agentIdentity == null)
            {
                throw new ArgumentNullException(nameof(agentIdentity));
            }

            return new AcquireTokenForAgentParameterBuilder(
                confidentialClientApplicationExecutor,
                agentIdentity)
                .WithScopes(scopes);
        }

        /// <summary>
        /// Specifies if the client application should ignore access tokens when reading the token cache.
        /// New tokens will still be written to the token cache.
        /// By default the token is taken from the cache (forceRefresh=false).
        /// </summary>
        /// <param name="forceRefresh">
        /// If <c>true</c>, the request will ignore cached access tokens on read, but will still write them to the cache once obtained from the identity provider. The default is <c>false</c>.
        /// </param>
        /// <returns>The builder to chain the .With methods.</returns>
        public AcquireTokenForAgentParameterBuilder WithForceRefresh(bool forceRefresh)
        {
            Parameters.ForceRefresh = forceRefresh;
            return this;
        }

        /// <summary>
        /// Specifies if the x5c claim (public key of the certificate) should be sent to the identity provider,
        /// which enables subject name/issuer based authentication for the client credential.
        /// This is useful for certificate rollover scenarios. See https://aka.ms/msal-net-sni.
        /// </summary>
        /// <param name="withSendX5C"><c>true</c> if the x5c should be sent. Otherwise <c>false</c>.
        /// The default is <c>false</c>.</param>
        /// <returns>The builder to chain the .With methods.</returns>
        public AcquireTokenForAgentParameterBuilder WithSendX5C(bool withSendX5C)
        {
            Parameters.SendX5C = withSendX5C;
            return this;
        }

        /// <inheritdoc/>
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return ConfidentialClientApplicationExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
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

        /// <inheritdoc/>
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            return ApiEvent.ApiIds.AcquireTokenForAgent;
        }
    }
}
