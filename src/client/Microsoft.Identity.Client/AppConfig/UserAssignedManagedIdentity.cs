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
    /// Class to define a user assigned managed identity.
    /// </summary>
    public class UserAssignedManagedIdentity : IManagedIdentity
    {
        /// <summary>
        /// Type of user assigned managed identity id supported by MSAL.
        /// </summary>
        public UserAssignedIdType UserAssignedIdType { get; private set; }

        /// <summary>
        /// Id of the user assigned managed identity.
        /// </summary>
        public string UserAssignedId { get; private set; }

        private UserAssignedManagedIdentity(UserAssignedIdType userAssignedIdType, string userAssignedId)
        {
            this.UserAssignedIdType = userAssignedIdType;
            this.UserAssignedId = userAssignedId;
        }

        /// <summary>
        /// Returns an instance of UserAssignedManagedIdentity with the given client id.
        /// </summary>
        /// <param name="clientId">Client id of the user assigned managed identity.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static UserAssignedManagedIdentity FromClientId(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            return new UserAssignedManagedIdentity(UserAssignedIdType.ClientId, clientId);
        }

        /// <summary>
        /// Returns an instance of UserAssignedManagedIdentity with the given resource id.
        /// </summary>
        /// <param name="resourceId">Resource id of the user assigned managed identity.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static UserAssignedManagedIdentity FromResourceId(string resourceId)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            return new UserAssignedManagedIdentity(UserAssignedIdType.ResourceId, resourceId);
        }
    }
}
