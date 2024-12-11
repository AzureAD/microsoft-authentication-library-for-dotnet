// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Internal
{
    internal static class TelemetryTokenTypeConstants
    {
        /// Bearer token type for telemetry.
        public const int Bearer = 1;

        /// Pop token type for telemetry.
        public const int Pop = 2;

        /// Ssh-cert token type for telemetry.
        public const int SshCert = 3;

        /// External token type for telemetry.
        public const int External = 4;

        /// Extension token type for telemetry.
        public const int Extension = 5;
    }
}
