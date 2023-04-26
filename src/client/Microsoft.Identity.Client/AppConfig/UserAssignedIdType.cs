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
    /// Enum to define types of user assigned identities supported by MSAL.
    /// </summary>
    public enum UserAssignedIdType
    {
        /// <summary>
        /// Client Id of the user assigned identity.
        /// </summary>
        ClientId,

        /// <summary>
        /// Resource Id of the user assigned identity.
        /// </summary>
        ResourceId
    }
}
