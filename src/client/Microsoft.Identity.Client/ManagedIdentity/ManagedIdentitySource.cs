// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Managed identity sources supported. The library will handle these internally. 
    /// </summary>
    public enum ManagedIdentitySource
    {
        /// <summary>
        /// Default.
        /// </summary>
        None,

        /// <summary>
        /// The source to acquire token for managed identity is IMDS.
        /// </summary>
        Imds,

        /// <summary>
        /// The source to acquire token for managed identity is App Service.
        /// </summary>
        AppService,

        /// <summary>
        /// The source to acquire token for managed identity is Azure Arc.
        /// </summary>
        AzureArc,

        /// <summary>
        /// The source to acquire token for managed identity is Cloud Shell.
        /// </summary>
        CloudShell,

        /// <summary>
        /// The source to acquire token for managed identity is Service Fabric.
        /// </summary>
        ServiceFabric
    }
}
