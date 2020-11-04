// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    /// <summary>
    /// </summary>
    internal class AcquireTokenByIntegratedWindowsAuthParameters : AbstractAcquireTokenByUsernameParameters, IAcquireTokenParameters
    {
        /// <inheritdoc />
        public void LogParameters(ICoreLogger logger)
        {
        }
    }
}
