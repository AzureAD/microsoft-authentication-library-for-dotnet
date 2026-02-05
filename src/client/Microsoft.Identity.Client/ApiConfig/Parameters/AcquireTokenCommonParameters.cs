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
        public AuthorityInfo AuthorityOverride { get; set; }
        public IAuthenticationOperation AuthenticationOperation { get; set; } = new BearerAuthenticationOperation();
        public IDictionary<string, string> ExtraHttpHeaders { get; set; }
        public PoPAuthenticationConfiguration PopAuthenticationConfiguration { get; set; }
        public IList<Func<OnBeforeTokenRequestData, Task>> OnBeforeTokenRequestHandler { get; internal set; }
        public X509Certificate2 MtlsCertificate { get; internal set; }
        public List<string> AdditionalCacheParameters { get; set; }
        public SortedList<string, Func<CancellationToken, Task<string>>> CacheKeyComponents { get; internal set; }
        public string FmiPathSuffix { get; internal set; }
        public string ClientAssertionFmiPath { get; internal set; }
        public bool IsMtlsPopRequested { get; set; }
        public string ExtraClientAssertionClaims { get; internal set; }

        /// <summary>
        /// Optional delegate for obtaining attestation JWT for Credential Guard keys.
        /// Set by the KeyAttestation package via .WithAttestationSupport().
        /// Returns null for non-attested flows.
        /// </summary>
        public Func<string, SafeHandle, string, CancellationToken, Task<string>> AttestationTokenProvider { get; set; }

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
