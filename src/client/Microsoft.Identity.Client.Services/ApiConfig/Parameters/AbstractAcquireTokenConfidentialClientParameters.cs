// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    /// <summary>
    /// Abstract class for confidential clients
    /// Supports common property(ies)
    /// </summary>
    internal abstract class AbstractAcquireTokenConfidentialClientParameters
    {
        /// <summary>
        /// Parameter sent to request to send X5C or not.
        /// This overrides application config settings.
        /// </summary>
        public bool? SendX5C { get; set; }

        /// <summary>
        /// if <c>true</c> then Spa code param will be sent via AcquireTokenByAuthorizeCode
        /// </summary>
        public bool SpaCode { get; set; }
    }
}
