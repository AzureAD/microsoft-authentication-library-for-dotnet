// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// Enum to represent the type of application.
    /// </summary>
    internal enum ApplicationType
    {
        ConfidentialClient,
        PublicClient,
        ManagedIdentity
    }
}
