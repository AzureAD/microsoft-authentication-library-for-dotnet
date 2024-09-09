// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenByAuthorizationCodeParameters : AbstractAcquireTokenConfidentialClientParameters, IAcquireTokenParameters
    {
        public string AuthorizationCode { get; set; }

        public string PkceCodeVerifier { get; set; }

        /// <summary>
        /// if <c>true</c> then Spa code param will be sent via AcquireTokenByAuthorizeCode
        /// </summary>
        public bool SpaCode { get; set; }

        public void LogParameters(ILoggerAdapter logger)
        {
        }
    }
}
