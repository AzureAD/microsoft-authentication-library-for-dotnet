// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal interface IValidatedProbeEndpointFactory
    {
        Uri GetValidatedEndpoint(ILoggerAdapter logger, string subPath, string queryParams);
    }
}
