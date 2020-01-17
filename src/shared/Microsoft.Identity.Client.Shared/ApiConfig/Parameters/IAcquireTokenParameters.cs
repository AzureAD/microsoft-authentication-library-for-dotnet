// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal interface IAcquireTokenParameters
    {
        void LogParameters(ICoreLogger logger);
    }
}
