// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.ManagedIdentity;
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
        private const string MiAttCacheKeyComponent = "mi_att";
        private static readonly Task<string> s_att0 = Task.FromResult("0");
        private static readonly Task<string> s_att1 = Task.FromResult("1");

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
        /// Specifies if the client application should ignore access tokens when reading the token cache. 
        /// New tokens will still be written to the application token cache.
        /// By default the token is taken from the application token cache (forceRefresh=false)
        /// </summary>
        /// <param name="forceRefresh">If <c>true</c>, the request will ignore cached access tokens on read, but will still write them to the cache once obtained from the Identity Provider. The default is <c>false</c>
        /// </param>
        /// <remarks>
        /// Do not use this flag except in well understood cases. Identity Providers will throttle clients that issue too many similar token requests.
        /// </remarks>
        /// <returns>The builder to chain the .With methods</returns>
        public AcquireTokenForManagedIdentityParameterBuilder WithForceRefresh(bool forceRefresh)
        {
            Parameters.ForceRefresh = forceRefresh;
            return this;
        }

        /// <summary>
        /// Adds a claims challenge to the token request. The SDK will bypass the token cache when a claims challenge is specified. Retry the 
        /// token acquisition, and use this value in the <see cref="WithClaims(string)"/> method. A claims challenge typically arises when 
        /// calling the protected downstream API, for example when the tenant administrator revokes credentials. Apps are required 
        /// to look for a 401 Unauthorized response from the protected api and to parse the WWW-Authenticate response header in order to 
        /// extract the claims. See https://aka.ms/msal-net-claim-challenge for details.
        /// </summary>
        /// <param name="claims">A string with one or multiple claims.</param>
        /// <returns>The builder to chain .With methods.</returns>
        public AcquireTokenForManagedIdentityParameterBuilder WithClaims(string claims)
        {
            CommonParameters.Claims = claims;
            return this;
        }

        /// <summary>
        /// Adds stable, client-originated claims (e.g., NSP network perimeter claims) to the token request.
        /// Unlike <see cref="WithClaims(string)"/>, these claims do NOT bypass the token cache.
        /// Tokens are cached and partitioned by the claims value — different claims produce different cache entries,
        /// while identical claims reuse cached tokens.
        /// The claims JSON must follow the OIDC claims request format (Section 5.5 of OpenID Connect Core 1.0).
        /// </summary>
        /// <param name="claimsJson">A JSON string in OIDC claims request format, e.g.,
        /// <c>{"access_token":{"xms_nsp_id":{"essential":true,"value":"nsp-perimeter-001"}}}</c>.</param>
        /// <returns>The builder to chain .With methods.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="claimsJson"/> is null or whitespace.</exception>
        /// <exception cref="MsalClientException">Thrown when <paramref name="claimsJson"/> is not a valid JSON object.</exception>
        /// <remarks>
        /// This is an experimental API. Call <c>.WithExperimentalFeatures(true)</c> on the application builder to enable it.
        /// Client claims are forwarded as the <c>claims</c> query parameter to IMDS endpoints.
        /// Other managed identity sources are not currently supported.
        /// When <see cref="WithClaims(string)"/> is also set, it triggers a separate cache bypass and
        /// revoked-token-hash flow; the two claims sources are not JSON-merged for the managed identity path.
        /// </remarks>
        public AcquireTokenForManagedIdentityParameterBuilder WithClaimsFromClient(string claimsJson)
        {
            ValidateUseOfExperimentalFeature();

            if (string.IsNullOrWhiteSpace(claimsJson))
            {
                throw new ArgumentNullException(nameof(claimsJson));
            }

            // Validate JSON is an object — OIDC claims parameter must be a JSON object
            try
            {
                using var doc = JsonDocument.Parse(claimsJson);
                if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
                {
                    throw new MsalClientException(
                        MsalError.InvalidJsonClaimsFormat,
                        "The claims parameter must be a JSON object. See https://openid.net/specs/openid-connect-core-1_0.html#ClaimsParameter.");
                }
            }
            catch (JsonException ex)
            {
                throw new MsalClientException(
                    MsalError.InvalidJsonClaimsFormat,
                    "The claims parameter is not valid JSON. Inspect the inner exception for details. See https://openid.net/specs/openid-connect-core-1_0.html#ClaimsParameter.",
                    ex);
            }

            CommonParameters.ClientClaims = claimsJson;

            // Add the claims to the cache key so different claims produce different cache entries.
            // Follows the same pattern as WithExtraClientAssertionClaims.
            CommonParameters.CacheKeyComponents ??=
                new SortedList<string, Func<CancellationToken, Task<string>>>();

            CommonParameters.CacheKeyComponents["client_claims"] =
                _ => Task.FromResult(claimsJson);

            return this;
        }

        /// <inheritdoc/>
        internal override Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            ApplyMtlsPopAndAttestation(acquireTokenForManagedIdentityParameters: Parameters, acquireTokenCommonParameters: CommonParameters);
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

        private static void ApplyMtlsPopAndAttestation(
            AcquireTokenCommonParameters acquireTokenCommonParameters, 
            AcquireTokenForManagedIdentityParameters acquireTokenForManagedIdentityParameters)
        {
            acquireTokenForManagedIdentityParameters.IsMtlsPopRequested = acquireTokenCommonParameters.IsMtlsPopRequested;
            acquireTokenForManagedIdentityParameters.AttestationTokenProvider = acquireTokenCommonParameters.AttestationTokenProvider;

            // PoP requests should be partitioned by attestation-support mode.
            if (acquireTokenCommonParameters.IsMtlsPopRequested)
            {
                acquireTokenCommonParameters.CacheKeyComponents ??=
                    new SortedList<string, Func<CancellationToken, Task<string>>>();

                acquireTokenCommonParameters.CacheKeyComponents[MiAttCacheKeyComponent] =
                    _ => acquireTokenCommonParameters.AttestationTokenProvider != null ? s_att1 : s_att0;
            }
        }
    }
}
