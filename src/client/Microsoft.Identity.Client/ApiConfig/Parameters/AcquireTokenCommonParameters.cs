// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.AuthScheme.Bearer;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using static Microsoft.Identity.Client.Extensibility.AbstractConfidentialClientAcquireTokenParameterBuilderExtension;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenCommonParameters
    {
        public ApiEvent.ApiIds ApiId { get; set; } = ApiEvent.ApiIds.None;
        public Guid CorrelationId { get; set; }
        public Guid UserProvidedCorrelationId { get; set; }
        public bool UseCorrelationIdFromUser { get; set; }
        public IEnumerable<string> Scopes { get; set; }
        public IDictionary<string, string> ExtraQueryParameters { get; set; }
        public string Claims { get; set; }
        public string ClientClaims { get; internal set; }
        public AuthorityInfo AuthorityOverride { get; set; }
        public IAuthenticationOperation AuthenticationOperation { get; set; } = new BearerAuthenticationOperation();
        public IDictionary<string, string> ExtraHttpHeaders { get; set; }
        public PoPAuthenticationConfiguration PopAuthenticationConfiguration { get; set; }
        public IList<Func<OnBeforeTokenRequestData, Task>> OnBeforeTokenRequestHandler { get; internal set; }
        public X509Certificate2 MtlsCertificate { get; internal set; }
        public List<string> AdditionalCacheParameters { get; set; }
        public SortedList<string, Func<CancellationToken, Task<string>>> CacheKeyComponents { get; internal set; }
        public bool? SendOfflineAccessScope { get; set; }
        public string FmiPathSuffix { get; internal set; }
        public string ClientAssertionFmiPath { get; internal set; }
        public bool IsMtlsPopRequested { get; set; }

        /// <summary>
        /// When true, MSAL uses the full IMDSv2 attested flow (mTLS connection to ESTS via a
        /// Credential Guard–issued certificate) but requests <c>token_type=bearer</c> from the
        /// token endpoint, returning a standard bearer token with no binding certificate.
        /// </summary>
        public bool IsMtlsBearerRequested { get; set; }
        public string ExtraClientAssertionClaims { get; internal set; }

        /// <summary>
        /// Optional caller-supplied delegate that adds extra tags to the OpenTelemetry metrics MSAL records
        /// for this request. It receives the <see cref="ExecutionResult"/> of the acquisition (success or failure)
        /// and a mutable list of tags to which additional dimensions can be appended.
        /// Set via <c>WithOtelTagsEnricher</c>.
        /// </summary>
        /// <remarks>
        /// The tags returned by the enricher are applied to every metric MSAL records for the request, so keep
        /// both their value cardinality and their number low. High-cardinality tag values (for example correlation
        /// ids, timestamps, or user identifiers) are the dominant cost: each distinct value multiplies the number
        /// of metric time series the downstream backend must store and aggregate. A large number of tags is a
        /// secondary cost that adds per-record overhead on MSAL's metric-recording path. Prefer a small set of
        /// low-cardinality dimensions; avoid using request-unique values as tags.
        /// </remarks> 
        public Action<ExecutionResult, IList<KeyValuePair<string, object>>> OtelTagsEnricher { get; set; }

        /// <summary>
        /// Optional delegate for obtaining attestation JWT for Credential Guard keys.
        /// Set by the KeyAttestation package via .WithAttestationSupport().
        /// Returns null for non-attested flows.
        /// Signature: (endpoint, keyHandle, clientId, keyId, logger, cancellationToken) → JWT or null.
        /// </summary>
        public Func<string, SafeHandle, string, string, ILoggerAdapter, CancellationToken, Task<string>> AttestationTokenProvider { get; set; }

        /// <summary>
        /// This tries to see if the token request should be done over mTLS or over normal HTTP 
        /// and set the correct parameters
        /// </summary>
        /// <param name="serviceBundle"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="MsalClientException"></exception>
        internal async Task TryInitMtlsPopParametersAsync(IServiceBundle serviceBundle, CancellationToken ct)
        {
            await MtlsPopParametersInitializer.TryInitAsync(this, serviceBundle, ct).ConfigureAwait(false);
        }
    }
}
