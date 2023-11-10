// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Component to be used with managed identity applications for Azure resources.
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide managed identity flow on mobile
#endif
    public interface IManagedIdentityApplication : IApplicationBase
    {
        /// <summary>
        /// Acquires token for a managed identity configured on Azure resource. See https://aka.ms/msal-net-managed-identity.
        /// </summary>
        /// <param name="resource">resource requested to access the protected API. For this flow (managed identity), the resource
        /// should be of the form "{ResourceIdUri}" or {ResourceIdUri/.default} for instance <c>https://management.azure.net</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com/.default</c>.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>You can also chain the following optional parameters:
        /// <see cref="AcquireTokenForManagedIdentityParameterBuilder.WithForceRefresh(bool)"/>
        /// </remarks>
        AcquireTokenForManagedIdentityParameterBuilder AcquireTokenForManagedIdentity(string resource);
    }
}
