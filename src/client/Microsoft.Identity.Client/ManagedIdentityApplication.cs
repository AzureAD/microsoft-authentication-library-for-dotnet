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
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.ManagedIdentity;
using System.Diagnostics;
#if TRA
using Microsoft.Identity.Client.Platforms.Features.KeyMaterial;
using Microsoft.Identity.Client.Credential;
#endif

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
#if TRA
            KeyMaterialManager = ServiceBundle.PlatformProxy.GetKeyMaterial();
            
            configuration.ManagedIdentityBindingCertificate = KeyMaterialManager.BindingCertificate;
            configuration.IsManagedIdentityTokenRequestInfoAvailable = KeyMaterialManager.CryptoKeyType != CryptoKeyType.None;
#endif
            ServiceBundle.ApplicationLogger.Verbose(() => $"ManagedIdentityApplication {configuration.GetHashCode()} created");
        }

        // Stores all app tokens
        internal ITokenCacheInternal AppTokenCacheInternal { get; }
#if TRA

        internal IKeyMaterialManager KeyMaterialManager { get; }
#endif  
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
#if TRA
        /// <summary>
        /// Used to determine if managed identity is able to perform Proof-of-Possession.
        /// </summary>
        /// <returns>Boolean indicating if Proof-of-Possession is supported</returns>
        public bool IsProofOfPossessionSupportedByClient()
        {
            return KeyMaterialManager.CryptoKeyType == CryptoKeyType.KeyGuard;
        }

        /// <summary>
        /// Used to determine if managed identity is able to handle claims.
        /// </summary>
        /// <returns>Boolean indicating if Claims is supported</returns>
        public bool IsClaimsSupportedByClient()
        {
            return KeyMaterialManager.CryptoKeyType != CryptoKeyType.None;
        }

        /// <summary>
        /// Retrives the binding certificate for advanced managed identity scenarios.
        /// </summary>
        /// <returns>Binding certificate used for advanced scenarios</returns>
        public X509Certificate2 GetBindingCertificate()
        {
            return KeyMaterialManager.BindingCertificate; //return the binding cert
        }
#endif
    }
}
