// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
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
        /// Constructor of a ManagedIdentityApplicationBuilder from application configuration options.
        /// See https://aka.ms/msal-net-application-configuration
        /// </summary>
        /// <param name="options">Managed identity applications configuration options</param>
        /// <returns>A <see cref="ManagedIdentityApplicationBuilder"/> from which to set more
        /// parameters, and to create a managed identity application instance</returns>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide managed identity flow on mobile
#endif
        public static ManagedIdentityApplicationBuilder CreateWithApplicationOptions(
            ManagedIdentityApplicationOptions options)
        {
            ApplicationBase.GuardMobileFrameworks();

            var config = new ApplicationConfiguration(isConfidentialClient: false);
            var builder = new ManagedIdentityApplicationBuilder(config).WithOptions(options);

            if (!string.IsNullOrWhiteSpace(options.UserAssignedClientId))
            {
                builder = builder.WithUserAssignedManagedIdentity(options.UserAssignedClientId);
            }

            builder = builder.WithCacheSynchronization(options.EnableCacheSynchronization);

            return builder;
        }

        /// <summary>
        /// Creates a ManagedIdentityApplicationBuilder.
        /// See https://aka.ms/msal-net-application-configuration
        /// </summary>
        /// <returns>A <see cref="ManagedIdentityApplicationBuilder"/> from which to set more
        /// parameters, and to create a managed identity application instance</returns>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide managed identity flow on mobile
#endif
        public static ManagedIdentityApplicationBuilder Create()
        {
            ApplicationBase.GuardMobileFrameworks();

            var config = new ApplicationConfiguration(isConfidentialClient: false);
            return new ManagedIdentityApplicationBuilder(config)
                .WithCacheSynchronization(false);
        }

        /// <summary>
        /// Creates a ManagedIdentityApplicationBuilder from a user assigned managed identity clientID / resourceId.
        /// See https://aka.ms/msal-net-managed-identity
        /// </summary>
        /// <param name="userAssignedId">Client ID / Resource ID of the user assigned managed identity assigned to the resource.</param>
        /// <returns>A <see cref="ManagedIdentityApplicationBuilder"/> from which to set more
        /// parameters, and to create a managed identity application instance</returns>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
        public static ManagedIdentityApplicationBuilder Create(string userAssignedId)
        {
            ApplicationBase.GuardMobileFrameworks();

            if (string.IsNullOrWhiteSpace(userAssignedId))
            {
                throw new ArgumentNullException(nameof(userAssignedId));
            }

            var config = new ApplicationConfiguration(isConfidentialClient: false);
            return new ManagedIdentityApplicationBuilder(config)
                .WithUserAssignedManagedIdentity(userAssignedId)
                .WithCacheSynchronization(false);
        }

        private ManagedIdentityApplicationBuilder WithUserAssignedManagedIdentity(string userAssignedId)
        {
            Config.IsUserAssignedManagedIdentity = true;

            if (Guid.TryParse(userAssignedId, out _))
            {
                Config.ManagedIdentityUserAssignedClientId = userAssignedId;
            } 
            else
            {
                Config.ManagedIdentityUserAssignedResourceId = userAssignedId;
            }

            return this;
        }

        /// <summary>
        /// When set to <c>true</c>, MSAL will lock cache access at the <see cref="ManagedIdentityApplication"/> level, i.e.
        /// the block of code between BeforeAccessAsync and AfterAccessAsync callbacks will be synchronized. 
        /// Apps can set this flag to <c>false</c> to enable an optimistic cache locking strategy, which may result in better performance, especially 
        /// when ConfidentialClientApplication or ManagedIdentityApplication objects are reused.
        /// </summary>
        /// <remarks>
        /// False by default.
        /// Not recommended for apps that call RemoveAsync
        /// </remarks>
        public ManagedIdentityApplicationBuilder WithCacheSynchronization(bool enableCacheSynchronization)
        {
            Config.CacheSynchronizationEnabled = enableCacheSynchronization;
            return this;
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
