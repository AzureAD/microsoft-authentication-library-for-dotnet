// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
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
        internal ManagedIdentityApplication(
            ApplicationConfiguration configuration)
            : base(configuration)
        {
            GuardMobileFrameworks();

            AppTokenCacheInternal = configuration.AppTokenCacheInternalForTest ?? new TokenCache(ServiceBundle, true);

            this.ServiceBundle.ApplicationLogger.Verbose(()=>$"ManagedIdentityApplication {configuration.GetHashCode()} created");
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

        /// <summary>
        /// Detects and returns the managed identity source available on the environment.
        /// </summary>
        /// <returns>Managed identity source detected on the environment if any.</returns>
        public static ManagedIdentitySource GetManagedIdentitySource()
        {
            return ManagedIdentityClient.GetManagedIdentitySource();
        }
    }
}
