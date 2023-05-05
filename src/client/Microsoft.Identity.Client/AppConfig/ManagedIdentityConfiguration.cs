// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// Class to store configuration for a managed identity enabled on a resource.
    /// For a system assigned managed identity use ManagedIdentityConfiguration.SystemAssigned.
    /// For user assigned managed identity use ManagedIdentityConfiguration.WithUserAssignedClientId("clientId") or 
    /// ManagedIdentityConfiguration.WithUserAssignedResourceId("resourceId").
    /// For more details see https://aka.ms/msal-net-managed-identity
    /// </summary>
    public class ManagedIdentityConfiguration
    {
        internal string UserAssignedId { get; private set; }
        internal ManagedIdentityIdType IdType { get; private set; }

        private ManagedIdentityConfiguration(ManagedIdentityIdType idType)
        {
            IdType = idType;
        }

        /// <summary>
        /// Create an instance of ManagedIdentityConfiguration for a system assigned managed identity.
        /// </summary>
        public static ManagedIdentityConfiguration SystemAssigned { get; } = 
            new ManagedIdentityConfiguration(ManagedIdentityIdType.SystemAssigned);

        /// <summary>
        /// Create an instance of ManagedIdentityConfiguration for a user assigned managed identity from a client id.
        /// </summary>
        /// <param name="clientId">Client id of the user assigned managed identity assigned to azure resource.</param>
        /// <returns>Instance of ManagedIdentityConfiguration.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ManagedIdentityConfiguration WithUserAssignedClientId(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentNullException(clientId);
            }

            return new ManagedIdentityConfiguration(ManagedIdentityIdType.ClientId) { UserAssignedId = clientId };
        }

        /// <summary>
        /// Create an instance of ManagedIdentityConfiguration for a user assigned managed identity from a resource id.
        /// </summary>
        /// <param name="resourceId">Resource id of the user assigned managed identity assigned to azure resource.</param>
        /// <returns>Instance of ManagedIdentityConfiguration.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ManagedIdentityConfiguration WithUserAssignedResourceId(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                throw new ArgumentNullException(resourceId);
            }

            return new ManagedIdentityConfiguration(ManagedIdentityIdType.ResourceId) { UserAssignedId = resourceId };
        }
    }
}
