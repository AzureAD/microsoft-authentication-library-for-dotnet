// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Internal.Utilities;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

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

            // Store ServiceBundle in a static field
            s_ServiceBundle = this.ServiceBundle;

            s_ServiceBundle.ApplicationLogger.Verbose(()=>$"ManagedIdentityApplication {configuration.GetHashCode()} created");
        }

        // Stores all app tokens
        internal ITokenCacheInternal AppTokenCacheInternal { get; }

        private static IServiceBundle s_ServiceBundle;


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

        /// <summary>
        /// Detects and returns the managed identity source available on the environment asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation. The task result contains the managed identity source detected on the environment if any.</returns>
        public static async Task<ManagedIdentitySource> GetManagedIdentitySourceAsync(CancellationToken cancellationToken = default)
        {
            return await ManagedIdentityClient.GetManagedIdentitySourceAsync(s_ServiceBundle, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves the binding certificate for advanced managed identity scenarios.
        /// </summary>
        /// <returns>Binding certificate used for advanced scenarios</returns>
        public static X509Certificate2 GetBindingCertificate()
        {
            // Return the binding certificate
            return CertificateHelper.GetOrCreateCertificate();
        }

        /// <summary>
        /// Occurs when the binding certificate has been updated/rotated.
        /// </summary>
        public static event Action<X509Certificate2> BindingCertificateRotated
        {
            add { CertificateHelper.CertificateUpdated += value; }
            remove { CertificateHelper.CertificateUpdated -= value; }
        }

        /// <summary>
        /// Forces an update of the in-memory binding certificate.
        /// For platform certificates (from the store), this is a no-op.
        /// </summary>
        /// <returns>The updated binding certificate.</returns>
        public static X509Certificate2 ForceRenewBindingCertificate()
        {
            return CertificateHelper.ForceUpdateInMemoryCertificate();
        }
    }
}
