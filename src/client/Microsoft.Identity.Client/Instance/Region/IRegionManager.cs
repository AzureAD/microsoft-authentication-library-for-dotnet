// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Region
{

    internal interface IRegionManager
    {
        /// <summary>
        /// Gets the azure region and adds telemetry to the ApiEvents
        /// </summary>        
        /// <returns>Returns null if region should not be used or cannot be discovered.</returns>
        Task<string> GetAzureRegionAsync(RequestContext requestContext);

    }
}
