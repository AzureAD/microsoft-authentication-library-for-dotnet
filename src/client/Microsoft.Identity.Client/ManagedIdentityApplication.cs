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
        public async Task<ManagedIdentitySource> GetManagedIdentitySourceAsync()
        {
            if (ManagedIdentityClient.s_sourceName != ManagedIdentitySource.None)
            {
                return ManagedIdentityClient.s_sourceName;
            }

            // Create a temporary RequestContext for the CSR metadata probe request.
            var csrMetadataProbeRequestContext = new RequestContext(this.ServiceBundle, Guid.NewGuid(), null, CancellationToken.None);

            // GetManagedIdentitySourceAsync might return ImdsV2 = true, but it still requires .WithMtlsProofOfPossesion on the Managed Identity Application object to hit the ImdsV2 flow
            return await ManagedIdentityClient.GetManagedIdentitySourceAsync(csrMetadataProbeRequestContext, isMtlsPopRequested: true).ConfigureAwait(false);
        }

        /// <summary>
        /// Detects and returns the managed identity source available on the environment.
        /// </summary>
        /// <returns>Managed identity source detected on the environment if any.</returns>
        [Obsolete("Use GetManagedIdentitySourceAsync() instead. \"ManagedIdentityApplication mi = miBuilder.Build() as ManagedIdentityApplication;\"")]
        public static ManagedIdentitySource GetManagedIdentitySource()
        {
            return ManagedIdentityClient.GetManagedIdentitySourceNoImdsV2();
        }
    }
}
