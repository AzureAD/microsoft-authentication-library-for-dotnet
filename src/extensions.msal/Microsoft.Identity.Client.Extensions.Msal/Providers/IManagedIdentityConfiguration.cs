// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    /// <summary>
    /// IManagedIdentityConfiguration provides the configurable properties for the ManagedIdentityProbe
    /// </summary>
    internal interface IManagedIdentityConfiguration
    {
        /// <summary>
        /// ManagedIdentitySecret is the secret for use in Azure AppService
        /// </summary>
        string ManagedIdentitySecret { get; }

        /// <summary>
        /// ManagedIdentityEndpoint is the AppService endpoint
        /// </summary>
        string ManagedIdentityEndpoint { get; }

        /// <summary>
        /// ClientId is the user assigned managed identity for use in VM managed identity
        /// </summary>
        string ClientId { get; }
    }
}
