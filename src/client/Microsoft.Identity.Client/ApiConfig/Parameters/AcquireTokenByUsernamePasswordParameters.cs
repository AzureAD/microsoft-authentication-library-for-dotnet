// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenByUsernamePasswordParameters : AbstractAcquireTokenByUsernameParameters, IAcquireTokenParameters
    {
        public string Password { get; set; }

        /// <inheritdoc />
        public void LogParameters(ILoggerAdapter logger)
        {
        }
    }
}
