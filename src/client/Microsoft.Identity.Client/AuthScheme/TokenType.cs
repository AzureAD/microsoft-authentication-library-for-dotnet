// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.AuthScheme
{
    /// <summary>
    /// Specifies the token type to log to telemetry.
    /// </summary>
    internal enum TokenType
    {
        /// <summary>
        /// Bearer token type.
        /// </summary>
        Bearer = 1,

        /// <summary>
        /// Pop token type.
        /// </summary>
        Pop = 2,

        /// <summary>
        /// Ssh-cert token type.
        /// </summary>
        SshCert = 3,

        /// <summary>
        /// External token type.
        /// </summary>
        External = 4,

        /// <summary>
        /// Extension token type.
        /// Extension = 5
        /// </summary>
        Extension = 5
    }
}
