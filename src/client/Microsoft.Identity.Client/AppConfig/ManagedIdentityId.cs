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
    /// For a system assigned managed identity use ManagedIdentityId.SystemAssigned.
    /// For user assigned managed identity use ManagedIdentityId.WithUserAssignedClientId("clientId") or 
    /// ManagedIdentityId.WithUserAssignedResourceId("resourceId") or 
    /// ManagedIdentityId.WithUserAssignedObjectId("objectid").
    /// For more details see https://aka.ms/msal-net-managed-identity
    /// </summary>
    public class ManagedIdentityId
    {
        internal string UserAssignedId { get; private set; }
        internal ManagedIdentityIdType IdType { get; private set; }
        internal readonly bool _isUserAssigned;

        private ManagedIdentityId(ManagedIdentityIdType idType)
        {
            IdType = idType;

            switch (idType)
            {
                case ManagedIdentityIdType.SystemAssigned:
                    _isUserAssigned = false;
                    break;
                case ManagedIdentityIdType.ClientId:
                case ManagedIdentityIdType.ResourceId:
                case ManagedIdentityIdType.ObjectId:
                    _isUserAssigned = true;
                    break;
            }
        }

        /// <summary>
        /// Create an instance of ManagedIdentityId for a system assigned managed identity.
        /// </summary>
        public static ManagedIdentityId SystemAssigned { get; } = 
            new ManagedIdentityId(ManagedIdentityIdType.SystemAssigned);

        /// <summary>
        /// Create an instance of ManagedIdentityId for a user assigned managed identity from a client id.
        /// </summary>
        /// <param name="clientId">Client id of the user assigned managed identity assigned to the azure resource.</param>
        /// <returns>Instance of ManagedIdentityId.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ManagedIdentityId WithUserAssignedClientId(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentNullException(clientId);
            }

            return new ManagedIdentityId(ManagedIdentityIdType.ClientId) { UserAssignedId = clientId };
        }

        /// <summary>
        /// Create an instance of ManagedIdentityId for a user assigned managed identity from a resource id.
        /// </summary>
        /// <param name="resourceId">Resource id of the user assigned managed identity assigned to the azure resource.</param>
        /// <returns>Instance of ManagedIdentityId.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ManagedIdentityId WithUserAssignedResourceId(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                throw new ArgumentNullException(resourceId);
            }

            return new ManagedIdentityId(ManagedIdentityIdType.ResourceId) { UserAssignedId = resourceId };
        }

        /// <summary>
        /// Create an instance of ManagedIdentityId for a user assigned managed identity from an object id.
        /// </summary>
        /// <param name="objectId">Object id of the user assigned managed identity assigned to the azure resource.</param>
        /// <returns>Instance of ManagedIdentityId.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ManagedIdentityId WithUserAssignedObjectId(string objectId)
        {
            if (string.IsNullOrEmpty(objectId))
            {
                throw new ArgumentNullException(objectId);
            }

            return new ManagedIdentityId(ManagedIdentityIdType.ObjectId) { UserAssignedId = objectId };
        }
    }
}
