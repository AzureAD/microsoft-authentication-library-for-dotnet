// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.AuthScheme.CDT
{
    internal static class CdtClaimTypes
    {
        #region JSON keys for Http request

        /// <summary>
        /// Access token with response cnf
        /// 
        /// </summary>
        public const string Ticket = "t";

        /// <summary>
        /// Constraints specified by the client
        /// 
        /// </summary>
        public const string ConstraintsToken = "c";

        /// <summary>
        /// Constraints specified by the client
        /// 
        /// </summary>
        public const string Constraints = "constraints";

        /// <summary>
        /// Non-standard claim representing a nonce that protects against replay attacks.
        /// </summary>
        public const string Nonce = "nonce";

        /// <summary>
        /// 
        /// </summary>
        public const string Type = "typ";

        #endregion
    }
}
