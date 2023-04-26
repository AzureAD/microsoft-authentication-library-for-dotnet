// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.AppConfig;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Configuration options for a managed identity application.
    /// See https://aka.ms/msal-net/application-configuration
    /// </summary>
    public class ManagedIdentityApplicationOptions : BaseApplicationOptions
    {
        /// <summary>
        /// Managed identity assigned to the resource, for system assigned managed identity <see cref="SystemAssignedManagedIdentity"/>
        /// and for user assigned managed identity <see cref="UserAssignedManagedIdentity"/>.
        /// </summary>
        public IManagedIdentity ManagedIdentity { get; set; }

        /// <summary>
        /// When set to <c>true</c>, MSAL will lock cache access at the <see cref="ManagedIdentityApplication"/> level, i.e.
        /// the block of code between BeforeAccessAsync and AfterAccessAsync callbacks will be synchronized. 
        /// Apps can set this flag to <c>false</c> to enable an optimistic cache locking strategy, which may result in better performance, especially 
        /// when ManagedIdentityApplication objects are reused.
        /// </summary>
        /// <remarks>
        /// False by default.
        /// </remarks>
        public bool EnableCacheSynchronization { get; set; } = false;
    }
}
