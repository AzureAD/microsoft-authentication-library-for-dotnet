// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Specifies the type of request being made to the identity provider.
    /// </summary>
    internal enum RequestType
    {
        /// <summary>
        /// Security Token Service (STS) request, used for standard authentication flows.
        /// </summary>
        STS,

        /// <summary>
        /// Managed Identity Default request, used when acquiring tokens for managed identities in Azure.
        /// </summary>
        ManagedIdentityDefault,

        /// <summary>
        /// Instance Metadata Service (IMDS) request, used for obtaining tokens from the Azure VM metadata endpoint.
        /// </summary>
        Imds,

        /// <summary>
        /// Region Discovery request, used for region discovery operations with exponential backoff retry strategy.
        /// </summary>
        RegionDiscovery
    }
}
