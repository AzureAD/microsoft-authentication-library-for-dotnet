// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Class to be used for managed identity applications (on Azure resources like App Services, Virtual Machines, Azure Arc, Service Fabric and Cloud Shell).
    /// </summary>
    /// <remarks>
    /// Managed identity can be enabled on Azure resources as a system assigned managed identity or a user assigned managed identity.
    /// </remarks>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide managed identity flow on mobile
#endif
    public sealed class ManagedIdentityApplication
        : ApplicationBase,
            IManagedIdentityApplication
    {
        internal ManagedIdentityClient ManagedIdentityClient { get; }
        
        internal ManagedIdentityApplication(
            ApplicationConfiguration configuration)
            : base(configuration)
        {
            GuardMobileFrameworks();

            AppTokenCacheInternal = configuration.AppTokenCacheInternalForTest ?? new TokenCache(ServiceBundle, true);

            this.ServiceBundle.ApplicationLogger.Verbose(()=>$"ManagedIdentityApplication {configuration.GetHashCode()} created");
        
            ManagedIdentityClient = new ManagedIdentityClient();
        }

        // Stores all app tokens
        internal ITokenCacheInternal AppTokenCacheInternal { get; }

        /// <inheritdoc/>
        public AcquireTokenForManagedIdentityParameterBuilder AcquireTokenForManagedIdentity(string resource)
        {
            if (string.IsNullOrEmpty(resource))
            {
                throw new ArgumentNullException(nameof(resource));
            }

            return AcquireTokenForManagedIdentityParameterBuilder.Create(
                ClientExecutorFactory.CreateManagedIdentityExecutor(this),
                resource);
        }

        /// <inheritdoc/>
        public Task<ManagedIdentitySourceResult> GetManagedIdentitySourceAsync(CancellationToken cancellationToken)
        {
            // Default behavior: do NOT probe IMDS.
            return GetManagedIdentitySourceAsync(probe: false, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Detects and returns the managed identity source available on the environment.
        /// probe=false: no IMDS probe; returns DefaultToImds if no env-based MI source detected.
        /// probe=true: probes IMDS; returns None if IMDS is unreachable.
        /// </summary>
        public async Task<ManagedIdentitySourceResult> GetManagedIdentitySourceAsync(
            bool probe = false,
            CancellationToken cancellationToken = default)
        {
            var cached = ManagedIdentityClient.s_sourceName;

            if (!probe && cached != ManagedIdentitySource.None)
            {
                return new ManagedIdentitySourceResult(cached);
            }

            if (probe && cached != ManagedIdentitySource.None)
            {
                if (cached != ManagedIdentitySource.DefaultToImds)
                {
                    // Cache contains a concrete source; no need to probe again.
                    return new ManagedIdentitySourceResult(cached);
                }

                // If cached is DefaultToImds, probing can refine to Imds/ImdsV2/None.
            }

            // Create a temporary RequestContext for the logger and the optional IMDS probe request.
            var requestContext = new RequestContext(this.ServiceBundle, Guid.NewGuid(), null, cancellationToken);

            // Use isMtlsPopRequested: true so probe mode can detect IMDSv2 capability when available.
            return await ManagedIdentityClient
                .GetManagedIdentitySourceAsync(requestContext, isMtlsPopRequested: true, probe: probe, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Detects and returns the managed identity source available on the environment.
        /// </summary>
        /// <returns>Managed identity source detected on the environment if any.</returns>
        [Obsolete("Use GetManagedIdentitySourceAsync() instead. \"ManagedIdentityApplication mi = miBuilder.Build() as ManagedIdentityApplication;\"")]
        public static ManagedIdentitySource GetManagedIdentitySource()
        {
            var source = ManagedIdentityClient.GetManagedIdentitySourceNoImds();
            
            return source == ManagedIdentitySource.None
                // ManagedIdentitySource.DefaultToImds is marked obsolete, but is intentionally used here as a sentinel value to support legacy detection logic.
                // This value signals that none of the environment-based managed identity sources were detected.
                ? ManagedIdentitySource.DefaultToImds
                : source;

        }
    }
}
