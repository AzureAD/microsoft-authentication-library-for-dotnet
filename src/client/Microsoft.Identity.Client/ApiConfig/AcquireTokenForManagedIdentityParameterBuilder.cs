// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Extensibility;
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

        /// <summary>
        /// Adds a claims challenge to the token request. The SDK will bypass the token cache when a claims challenge is specified.. Retry the 
        /// token acquisition, and use this value in the <see cref="WithClaims(string)"/> method. A claims challenge typically arises when 
        /// calling the protected downstream API, for example when the tenant administrator wants to revokes credentials. Apps are required 
        /// to look for a 401 Unauthorized response from the protected api and to parse the WWW-Authenticate response header in order to 
        /// extract the claims.See https://aka.ms/msal-net-claim-challenge for details. This API is not always available for managed identity flows, 
        /// depending on the client and that apps can monitor this by using <see cref="ManagedIdentityApplication.IsClaimsSupportedByClient"/> method
        /// </summary>
        /// <param name="claims">A string with one or multiple claims.</param>
        /// <returns>The builder to chain .With methods.</returns>
        public AcquireTokenForManagedIdentityParameterBuilder WithClaims(string claims)
        {
            ValidateUseOfExperimentalFeature("WithClaims");

            CommonParameters.Claims = claims;
            return this;
        }

        /// <summary>
        /// Registers an asynchronous delegate that will be invoked just before the token request is executed.
        /// This delegate allows for modifications to the token request data, such as adding or removing headers,
        /// or altering body parameters. Use this method to inject custom logic or to manipulate the request
        /// based on dynamic conditions or application-specific requirements.
        /// </summary>
        /// <param name="onBeforeTokenRequestHandler">An async delegate that takes an instance of <see cref="OnBeforeTokenRequestData"/>
        /// and allows for the manipulation of the request data before the token request is made. The delegate can perform
        /// operations such as modifying the request headers, changing the request body, or logging request data.</param>
        /// <returns>The same <see cref="AcquireTokenForManagedIdentityParameterBuilder"/> instance to enable method chaining.</returns>
        /// <remarks>
        /// This method is part of experimental features and may change in future releases. It is provided for testability purposes.
        /// </remarks>
        public AcquireTokenForManagedIdentityParameterBuilder OnBeforeTokenRequest(Func<OnBeforeTokenRequestData, Task> onBeforeTokenRequestHandler)
        {
            ValidateUseOfExperimentalFeature("OnBeforeTokenRequest");

            CommonParameters.OnBeforeTokenRequestHandler = onBeforeTokenRequestHandler;
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
