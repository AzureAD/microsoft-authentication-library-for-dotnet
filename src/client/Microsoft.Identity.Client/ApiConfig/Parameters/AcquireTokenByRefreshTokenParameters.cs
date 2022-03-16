// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenByRefreshTokenParameters : AbstractAcquireTokenConfidentialClientParameters, IAcquireTokenParameters
    {
        public string RefreshToken { get; set; }

        public void LogParameters(IMsalLogger logger)
        {
        }
    }
}
