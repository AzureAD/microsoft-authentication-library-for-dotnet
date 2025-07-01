﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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

        /// <summary>
        /// Detects and returns the managed identity source available on the environment asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation. The task result contains the managed identity source detected on the environment if any.</returns>
        public static async Task<ManagedIdentitySource> GetManagedIdentitySourceAsync(CancellationToken cancellationToken = default)
        {
            var config = new ApplicationConfiguration(MsalClientType.ManagedIdentityClient);
            var serviceBundle = new ServiceBundle(config);
            return await ManagedIdentityClient.GetManagedIdentitySourceAsync(serviceBundle, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the managed identity binding certificate.
        /// </summary>
        /// <returns></returns>
        public static X509Certificate2 GetManagedIdentityBindingCertificate()
        {
            return CertificateHelper.GetOrCreateCertificate();
        }

        /// <summary>
        /// Updates the managed identity binding certificate.
        /// </summary>
        /// <returns></returns>
        public static X509Certificate2 ForceUpdateInMemoryCertificate()
        {
            return CertificateHelper.ForceUpdateInMemoryCertificate();
        }

        /// <summary>
        /// Raised whenever the managed-identity binding certificate is created
        /// or renewed by MSAL. Subscribe early (before the first call to
        /// <see cref="GetManagedIdentityBindingCertificate"/> or token acquisition)
        /// if you need to process every update.
        /// </summary>
        public static event Action<X509Certificate2> BindingCertificateUpdated;

        // Static ctor wires the internal helper’s event to the public one.
        static ManagedIdentityApplication()
        {
            // Forward the event without exposing CertificateHelper.
            CertificateHelper.CertificateUpdated += cert =>
                BindingCertificateUpdated?.Invoke(cert);
        }
    }
}
