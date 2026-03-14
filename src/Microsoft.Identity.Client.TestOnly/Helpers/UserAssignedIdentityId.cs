// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.TestOnly
{
    /// <summary>
    /// Specifies which user-assigned identity attribute to use when configuring a UAMI mock.
    /// </summary>
    public enum UserAssignedIdentityId
    {
        /// <summary>No user-assigned identity — uses system-assigned managed identity.</summary>
        None,

        /// <summary>Identify the UAMI by its client (application) ID GUID.</summary>
        ClientId,

        /// <summary>Identify the UAMI by its full Azure resource ID.</summary>
        ResourceId,

        /// <summary>Identify the UAMI by its object (principal) ID GUID.</summary>
        ObjectId
    }
}
