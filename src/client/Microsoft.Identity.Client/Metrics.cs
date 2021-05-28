// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    public static class Metrics
    {
        /// <summary>
        /// Total tokens obtained by MSAL
        /// </summary>
        public static long TotalTokensObtainedByMsal { get; set; }

        /// <summary>
        /// Total tokens obtained by MSAL via cache
        /// </summary>
        public static long TotalTokensObtainedByMsalViaCache { get; set; }
    }
}
