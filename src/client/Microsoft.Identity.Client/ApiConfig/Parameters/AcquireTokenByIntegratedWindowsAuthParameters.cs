// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    /// <summary>
    /// </summary>
    internal class AcquireTokenByIntegratedWindowsAuthParameters : AbstractAcquireTokenByUsernameParameters, IAcquireTokenParameters
    {
        /// <inheritdoc />
        public void LogParameters(IMsalLogger logger)
        {
        }
    }
}
