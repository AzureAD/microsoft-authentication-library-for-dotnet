// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.TelemetryCore
{
    internal class XmsCliTelemInfo
    {
        /// <summary>
        /// Monotonically increasing integer specifying
        /// x-ms-cliteleminfo header version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Bundle id for server error.
        /// </summary>
        public string ServerErrorCode { get; set; }

        /// <summary>
        /// Bundle id for server suberror.
        /// </summary>
        public string ServerSubErrorCode { get; set; }

        /// <summary>
        /// Bundle id for refresh token age.
        /// Floating-point value with a unit of milliseconds
        /// </summary>
        public string TokenAge { get; set; }

        /// <summary>
        /// Bundle id for spe_ring info. Indicates whether the request was executed
        /// on a ring serving SPE traffic. An empty string indicates this occurred on
        /// an outer ring, and the string "I" indicates the request occurred on the
        /// inner ring
        /// </summary>
        public string SpeInfo { get; set; }
    }
}
