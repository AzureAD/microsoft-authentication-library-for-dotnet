﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.TelemetryClient;
using Microsoft.Identity.Client.Utils;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Builder for managed identity applications.
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide managed identity flow on mobile
#endif
    public sealed class ManagedIdentityApplicationBuilder : BaseAbstractApplicationBuilder<ManagedIdentityApplicationBuilder>
    {
        /// <inheritdoc />
        internal ManagedIdentityApplicationBuilder(ApplicationConfiguration configuration)
            : base(configuration)
        {
            ApplicationBase.GuardMobileFrameworks();
        }

        /// <summary>
        /// Creates a ManagedIdentityApplicationBuilder from a user assigned managed identity clientID / resourceId.
        /// For example, for a system assigned managed identity use ManagedIdentityApplicationBuilder.Create(SystemAssignedManagedIdentity.Default())
        /// and for a user assigned managed identity use ManagedIdentityApplicationBuilder.Create(UserAssignedManagedIdentity.FromClientId(clientId)).
        /// For more details see https://aka.ms/msal-net-managed-identity
        /// </summary>
        /// <param name="managedIdentityConfiguration">Configuration of the Managed Identity assigned to the resource.</param>
        /// <returns>A <see cref="ManagedIdentityApplicationBuilder"/> from which to set more
        /// parameters, and to create a managed identity application instance</returns>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
        public static ManagedIdentityApplicationBuilder Create(ManagedIdentityConfiguration managedIdentityConfiguration)
        {
            ApplicationBase.GuardMobileFrameworks();

            return new ManagedIdentityApplicationBuilder(BuildConfiguration(managedIdentityConfiguration));
        }

        private static ApplicationConfiguration BuildConfiguration(ManagedIdentityConfiguration managedIdentityConfiguration)
        {
            _ = managedIdentityConfiguration ?? throw new ArgumentNullException(nameof(managedIdentityConfiguration));
            var config = new ApplicationConfiguration(MsalClientType.ManagedIdentityClient);

            switch (managedIdentityConfiguration.IdType)
            {
                case ManagedIdentityIdType.ClientId:
                    config.IsUserAssignedManagedIdentity = true;
                    config.ManagedIdentityUserAssignedClientId = managedIdentityConfiguration.UserAssignedId;
                    break;
                case ManagedIdentityIdType.ResourceId:
                    config.IsUserAssignedManagedIdentity = true;
                    config.ManagedIdentityUserAssignedResourceId = managedIdentityConfiguration.UserAssignedId;
                    break;
            }

            config.CacheSynchronizationEnabled = false;
            config.AccessorOptions = CacheOptions.EnableSharedCacheOptions;

            return config;
        }

        /// <summary>
        /// Sets telemetry client for the application.
        /// </summary>
        /// <param name="telemetryClients">List of telemetry clients to add telemetry logs.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public ManagedIdentityApplicationBuilder WithTelemetryClient(params ITelemetryClient[] telemetryClients)
        {
            ValidateUseOfExperimentalFeature("ITelemetryClient");

            if (telemetryClients == null)
            {
                throw new ArgumentNullException(nameof(telemetryClients));
            }

            if (telemetryClients.Length > 0)
            {
                foreach (var telemetryClient in telemetryClients)
                {
                    if (telemetryClient == null)
                    {
                        throw new ArgumentNullException(nameof(telemetryClient));
                    }

                    telemetryClient.Initialize();
                }

                Config.TelemetryClients = telemetryClients;
            }

            TelemetryClientLogMsalVersion();

            return this;
        }

        private void TelemetryClientLogMsalVersion()
        {
            if (Config.TelemetryClients.HasEnabledClients(TelemetryConstants.ConfigurationUpdateEventName))
            {
                MsalTelemetryEventDetails telemetryEventDetails = new MsalTelemetryEventDetails(TelemetryConstants.ConfigurationUpdateEventName);
                telemetryEventDetails.SetProperty(TelemetryConstants.MsalVersion, MsalIdHelper.GetMsalVersion());

                Config.TelemetryClients.TrackEvent(telemetryEventDetails);
            }
        }

        internal ManagedIdentityApplicationBuilder WithAppTokenCacheInternalForTest(ITokenCacheInternal tokenCacheInternal)
        {
            Config.AppTokenCacheInternalForTest = tokenCacheInternal;
            return this;
        }

        /// <summary>
        /// Builds the ManagedIdentityApplication from the parameters set
        /// in the builder
        /// </summary>
        /// <returns></returns>
        public IManagedIdentityApplication Build()
        {
            return BuildConcrete();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        internal ManagedIdentityApplication BuildConcrete()
        {
            ValidateUseOfExperimentalFeature("ManagedIdentity");
            DefaultConfiguration();
            return new ManagedIdentityApplication(BuildConfiguration());
        }

        private void DefaultConfiguration()
        {
            ComputeClientIdForCaching();

            Config.TenantId = Constants.ManagedIdentityDefaultTenant;
            Config.RedirectUri = Constants.DefaultConfidentialClientRedirectUri;
            Config.IsInstanceDiscoveryEnabled = false; // Disable instance discovery for managed identity
        }

        private void ComputeClientIdForCaching()
        {
            if (!string.IsNullOrEmpty(Config.ManagedIdentityUserAssignedClientId))
            {
                Config.ClientId = Config.ManagedIdentityUserAssignedClientId;
            }
            else if (!string.IsNullOrEmpty(Config.ManagedIdentityUserAssignedResourceId))
            {
                Config.ClientId = Config.ManagedIdentityUserAssignedResourceId;
            }
            else
            {
                Config.ClientId = Constants.ManagedIdentityDefaultClientId;
            }
        }
    }
}
