// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Builder for AcquireTokenForManagedIdentity (used to get token for managed identities).
    /// See https://aka.ms/msal-net-managed-identity
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide managed identity flow on mobile
#endif
    public sealed class AcquireTokenForManagedIdentityParameterBuilder :
        AbstractManagedIdentityAcquireTokenParameterBuilder<AcquireTokenForManagedIdentityParameterBuilder>
    {
        private AcquireTokenForManagedIdentityParameters Parameters { get; } = new AcquireTokenForManagedIdentityParameters();

        /// <inheritdoc/>
        internal AcquireTokenForManagedIdentityParameterBuilder(IManagedIdentityApplicationExecutor managedIdentityApplicationExecutor)
            : base(managedIdentityApplicationExecutor)
        {
        }

        internal static AcquireTokenForManagedIdentityParameterBuilder Create(
            IManagedIdentityApplicationExecutor managedIdentityApplicationExecutor,
            string resource)
        {
            return new AcquireTokenForManagedIdentityParameterBuilder(managedIdentityApplicationExecutor).WithResource(resource);
        }

        private AcquireTokenForManagedIdentityParameterBuilder WithResource(string resource)
        {
            Parameters.Resource = ScopeHelper.RemoveDefaultSuffixIfPresent(resource);
            CommonParameters.Scopes = new string[] { Parameters.Resource };
            return this;
        }

        /// <summary>
        /// Specifies if the token request will ignore the access token in the application token cache
        /// and will attempt to acquire a new access token for managed identity.
        /// By default the token is taken from the application token cache (forceRefresh=false)
        /// </summary>
        /// <param name="forceRefresh">If <c>true</c>, the request will ignore the token cache. The default is <c>false</c>
        /// </param>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenForManagedIdentityParameterBuilder WithForceRefresh(bool forceRefresh)
        {
            Parameters.ForceRefresh = forceRefresh;
            return this;
        }

        /// <inheritdoc/>
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            return ManagedIdentityApplicationExecutor.ExecuteAsync(CommonParameters, Parameters, cancellationToken);
        }

        /// <inheritdoc/>
        internal override ApiEvent.ApiIds CalculateApiEventId()
        {
            if (ServiceBundle.Config.ManagedIdentityId.IdType == AppConfig.ManagedIdentityIdType.SystemAssigned)
            {
                return ApiEvent.ApiIds.AcquireTokenForSystemAssignedManagedIdentity;
            }

            return ApiEvent.ApiIds.AcquireTokenForUserAssignedManagedIdentity;
        }
    }
}
