// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Enumeration for the AuthorityTypes
    /// </summary>
    internal enum AuthorityType
    {
        /// <summary>
        /// Azure Active Directory
        /// </summary>
        Aad,

        /// <summary>
        /// ADFS authority
        /// </summary>
        Adfs,

        /// <summary>
        /// For perosnal/social accounts
        /// </summary>
        B2C, 

        /// <summary>
        /// For DSTS only
        /// </summary>
        Dsts,
    }
}
