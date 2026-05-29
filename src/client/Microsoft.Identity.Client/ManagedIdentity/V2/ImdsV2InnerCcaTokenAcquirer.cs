// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Instance.Oidc;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.OAuth2.Throttling;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Spike (gladjohn/rewire): drives the IMDSv2 token leg through an internal
    /// <see cref="ConfidentialClientApplication"/> using <c>AcquireTokenForClient</c>
    /// instead of issuing the HTTP POST directly. The cert mint
    /// (/getplatformmetadata + /issuecredential) still happens up the stack; this class
    /// only replaces the post-mint token POST so we can ride the ConfClient pipeline
    /// (cache, claims, telemetry, retry, mTLS PoP scheme, x5t#S256 binding, etc.).
    /// </summary>
    internal static class ImdsV2InnerCcaTokenAcquirer
    {
        /// <summary>
        /// Builds (or reuses) an internal CCA bound to <paramref name="bindingCert"/> and acquires a token
        /// through <c>AcquireTokenForClient</c>. The OIDC discovery cache is pre-seeded so the inner CCA
        /// never makes a /.well-known/openid-configuration call against the IMDS mTLS endpoint.
        /// </summary>
        public static async Task<ManagedIdentityResponse> AcquireTokenAsync(
            RequestContext requestContext,
            AcquireTokenForManagedIdentityParameters parameters,
            string mtlsEndpoint,
            string tenantId,
            string clientId,
            string resource,
            X509Certificate2 bindingCert,
            bool isMtlsPopRequested,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(mtlsEndpoint))
            {
                throw new ArgumentException("mtlsEndpoint must be provided.", nameof(mtlsEndpoint));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException("tenantId must be provided.", nameof(tenantId));
            }

            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentException("clientId must be provided.", nameof(clientId));
            }

            if (bindingCert is null)
            {
                throw new ArgumentNullException(nameof(bindingCert));
            }

            string authorityRaw = mtlsEndpoint.TrimEnd('/') + "/" + tenantId.Trim('/');

            // Match the exact key format the inner CCA will use to query OIDC cache
            // (GenericAuthority queries with AuthorityInfo.CanonicalAuthority.AbsoluteUri).
            string authority = new Uri(authorityRaw).AbsoluteUri;
            string tokenEndpoint = authority.TrimEnd('/') + ImdsV2ManagedIdentitySource.AcquireEntraTokenPath;

            // Pre-seed OIDC discovery so the generic-authority code path inside the inner CCA
            // doesn't hit /.well-known/openid-configuration on the IMDS mTLS endpoint.
            OidcRetrieverWithCache.TrySetPreseeded(authority, new OidcMetadata
            {
                Issuer = authority,
                TokenEndpoint = tokenEndpoint,
            });

            requestContext.Logger.Info(() =>
                $"[ImdsV2][CcaRewire] Driving token leg via inner CCA. authority={authority}, clientId={clientId}, pop={isMtlsPopRequested}");

            IConfidentialClientApplication cca = BuildInnerCca(
                requestContext,
                authorityRaw,
                clientId,
                bindingCert);

            string scope = resource.TrimEnd('/') + "/.default";

            // IMDS / Azure ARM expect specific correlation + throttling headers. ConfClient
            // already emits `client-request-id` and `return-client-request-id` natively, so we
            // only add the IMDS-flavored `x-ms-correlation-id` (matching the wire shape that
            // the IMDS mTLS endpoint and existing ImdsManagedIdentitySource emit today) plus
            // the standard throttle marker.
            var extraHeaders = new Dictionary<string, string>
            {
                { OAuth2Header.XMsCorrelationId, requestContext.CorrelationId.ToString() },
                { ThrottleCommon.ThrottleRetryAfterHeaderName, ThrottleCommon.ThrottleRetryAfterHeaderValue },
            };

            AcquireTokenForClientParameterBuilder builder = cca
                .AcquireTokenForClient(new[] { scope })
                .WithExtraHttpHeaders(extraHeaders);

            if (isMtlsPopRequested)
            {
                builder = builder.WithMtlsProofOfPossession();
            }

            if (!string.IsNullOrEmpty(parameters.Claims))
            {
                builder = builder.WithClaims(parameters.Claims);
            }

            try
            {
                AuthenticationResult result = await builder
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);

                return new ManagedIdentityResponse
                {
                    AccessToken = result.AccessToken,
                    ExpiresOn = ((DateTimeOffset)result.ExpiresOn)
                        .ToUnixTimeSeconds()
                        .ToString(CultureInfo.InvariantCulture),
                    TokenType = isMtlsPopRequested ? Constants.MtlsPoPTokenType : Constants.BearerTokenType,
                    ClientId = clientId,
                    Resource = resource,
                };
            }
            catch (MsalServiceException ex)
            {
                // Wrap so callers (ManagedIdentityAuthRequest, SCHANNEL retry, telemetry) keep
                // seeing the same exception surface they do today.
                int? statusCode = ex.StatusCode == 0 ? (int?)null : ex.StatusCode;

                throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                    string.IsNullOrEmpty(ex.ErrorCode) ? MsalError.ManagedIdentityRequestFailed : ex.ErrorCode,
                    ex.Message,
                    ex,
                    ManagedIdentitySource.ImdsV2,
                    statusCode);
            }
        }

        private static IConfidentialClientApplication BuildInnerCca(
            RequestContext requestContext,
            string authority,
            string clientId,
            X509Certificate2 bindingCert)
        {
            // Spike: build a fresh inner CCA per call. Caching by (clientId, certThumbprint)
            // is a follow-up optimization.
            //
            // Disable the inner CCA's in-memory cache entirely. The outer MI cache (on
            // ManagedIdentityApplication) is the single source of truth for token reads/writes;
            // letting the inner CCA also cache means we'd serialize/store every token twice with
            // no read benefit (the inner cache is GC'd with the fresh CCA instance per call).
            // See CacheOptions.DisableInternalCacheOptions docs and ClientCredentialRequest.cs:54.
            //
            // Thread the outer's retry policy factory through to the inner CCA. Today the default
            // factory is stateless (new RetryPolicyFactory() returns the same DefaultRetryPolicy
            // for STS on either instance), so behavior is unchanged. Explicit wire-up keeps intent
            // visible, lets tests that mock RetryPolicyFactory on the outer MI app exercise the
            // inner pipeline too, and avoids a hidden divergence if the default contract ever
            // changes. OAuth2Client.cs:119 still drives the actual policy selection via
            // RequestType.STS.
            ConfidentialClientApplicationBuilder builder = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithCertificate(bindingCert)
                .WithOidcAuthority(authority)
                .WithInstanceDiscovery(false)
                .WithCacheOptions(CacheOptions.DisableInternalCacheOptions)
                .WithHttpManager(requestContext.ServiceBundle.HttpManager)
                .WithRetryPolicyFactory(requestContext.ServiceBundle.Config.RetryPolicyFactory);

            return builder.Build();
        }
    }
}
