// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Component to be used with managed identity applications for Azure resources.
    /// </summary>
#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
    public partial interface IManagedIdentityApplication : IClientApplicationBase
    {
        /// <summary>
        /// Application token cache. This case holds access tokens for the application. It's maintained
        /// and updated silently if needed when calling <see cref="AcquireTokenForManagedIdentity(IEnumerable{string})"/>
        /// </summary>
        /// <remarks>On .NET Framework and .NET Core you can also customize the token cache serialization.
        /// See https://aka.ms/msal-net-token-cache-serialization. This is taken care of by MSAL.NET on other platforms.
        /// </remarks>
        ITokenCache AppTokenCache { get; }

        /// <summary>
        /// Acquires token for a managed identity configured on Azure resource. See https://aka.ms/msal-net-managed-identity.
        /// </summary>
        /// <param name="resource">resource requested to access the protected API. For this flow (managed identity), the resource
        /// should be of the form "{ResourceIdUri}" for instance <c>https://management.azure.net</c> or, for Microsoft
        /// Graph, <c>https://graph.microsoft.com</c>.</param>
        /// <returns>A builder enabling you to add optional parameters before executing the token request</returns>
        /// <remarks>You can also chain the following optional parameters:
        /// <see cref="AcquireTokenForManagedIdentityParameterBuilder.WithForceRefresh(bool)"/>
        /// </remarks>
        AcquireTokenForManagedIdentityParameterBuilder AcquireTokenForManagedIdentity(string resource);
    }
}
