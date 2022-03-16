// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal interface IAcquireTokenParameters
    {
        void LogParameters(IMsalLogger logger);
    }
}
