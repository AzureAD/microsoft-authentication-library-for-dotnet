// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenByAuthorizationCodeParameters : IAcquireTokenParameters
    {
        public string AuthorizationCode { get; set; }

        public bool SendX5C { get; set; }

        public void LogParameters(ICoreLogger logger)
        {
        }
    }
}
