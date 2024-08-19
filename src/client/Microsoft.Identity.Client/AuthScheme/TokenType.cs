// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.AuthScheme
{
    /// <summary>
    /// Specifies the token type to log to telemetry.
    /// </summary>
    public enum TokenType
    {
        /// <summary>
        /// Bearer token type.
        /// </summary>
        Bearer = 1,

        /// <summary>
        /// Pop token type (referrs to the official SHR POP)
        /// </summary>
        Pop = 2,

        /// <summary>
        /// Ssh-cert token type.
        /// </summary>
        SshCert = 3,

        /// <summary>
        /// External token type. Currently used by the unoficial SHR POP implementation.
        /// </summary>
        External = 4

        // values up to 20 are reserved for internal use
    }
}
