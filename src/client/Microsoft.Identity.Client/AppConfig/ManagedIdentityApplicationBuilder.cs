// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        /// <inheritdoc/>
        internal ManagedIdentityApplicationBuilder(ApplicationConfiguration configuration)
            : base(configuration)
        {
            ApplicationBase.GuardMobileFrameworks();
        }

        /// <summary>
        /// Creates a ManagedIdentityApplicationBuilder from a user assigned managed identity clientID / resourceId / objectId.
        /// For example, for a system assigned managed identity use ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
        /// and for a user assigned managed identity use ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.WithUserAssignedClientId(clientId)) or
        /// ManagedIdentityId.WithUserAssignedResourceId("resourceId") or 
        /// ManagedIdentityId.WithUserAssignedObjectId("objectid").
        /// For more details see https://aka.ms/msal-net-managed-identity
        /// </summary>
        /// <param name="managedIdentityId">Configuration of the Managed Identity assigned to the resource.</param>
        /// <returns>A <see cref="ManagedIdentityApplicationBuilder"/> from which to set more
        /// parameters, and to create a managed identity application instance</returns>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
        public static ManagedIdentityApplicationBuilder Create(ManagedIdentityId managedIdentityId)
        {
            ApplicationBase.GuardMobileFrameworks();

            return new ManagedIdentityApplicationBuilder(BuildConfiguration(managedIdentityId));
        }

        private static ApplicationConfiguration BuildConfiguration(ManagedIdentityId managedIdentityId)
        {
            _ = managedIdentityId ?? throw new ArgumentNullException(nameof(managedIdentityId));
            var config = new ApplicationConfiguration(MsalClientType.ManagedIdentityClient);

            config.ManagedIdentityId = managedIdentityId;

            config.CacheSynchronizationEnabled = false;
            config.AccessorOptions = CacheOptions.EnableSharedCacheOptions;

            return config;
        }

        /// <summary>
        /// This method is obsolete. See https://aka.ms/msal-net-telemetry
        /// </summary>
        [Obsolete("Telemetry is sent automatically by MSAL.NET. See https://aka.ms/msal-net-telemetry.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ManagedIdentityApplicationBuilder WithTelemetryClient(params ITelemetryClient[] telemetryClients)
        {
            return this;
        }        

        internal ManagedIdentityApplicationBuilder WithAppTokenCacheInternalForTest(ITokenCacheInternal tokenCacheInternal)
        {
            Config.AppTokenCacheInternalForTest = tokenCacheInternal;
            return this;
        }

        /// <summary>
        /// Microsoft Identity specific OIDC extension that allows resource challenges to be resolved without interaction. 
        /// Allows configuration of one or more client capabilities, e.g. "llt"
        /// </summary>
        /// <remarks>
        /// MSAL will transform these into special claims request. See https://openid.net/specs/openid-connect-core-1_0-final.html#ClaimsParameter for
        /// details on claim requests. 
        /// For more details see https://aka.ms/msal-net-claims-request
        /// </remarks>
        public ManagedIdentityApplicationBuilder WithClientCapabilities(IEnumerable<string> clientCapabilities)
        {
            if (clientCapabilities != null && clientCapabilities.Any())
            {
                Config.ClientCapabilities = clientCapabilities;
            }

            return this;
        }

        /// <summary>
        /// Builds an instance of <see cref="IManagedIdentityApplication"/> 
        /// from the parameters set in the <see cref="ManagedIdentityApplicationBuilder"/>.
        /// </summary>
        /// <exception cref="MsalClientException">Thrown when errors occur locally in the library itself (for example, because of incorrect configuration).</exception>
        /// <returns>An instance of <see cref="IManagedIdentityApplication"/></returns>
        public IManagedIdentityApplication Build()
        {
            return BuildConcrete();
        }

        internal ManagedIdentityApplication BuildConcrete()
        {
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
            if (Config.ManagedIdentityId.IdType == ManagedIdentityIdType.SystemAssigned)
            {
                Config.ClientId = Constants.ManagedIdentityDefaultClientId;
            }
            else
            {
                Config.ClientId = Config.ManagedIdentityId.UserAssignedId;
            }
        }
    }
}
