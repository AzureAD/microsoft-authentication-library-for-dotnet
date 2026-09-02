// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class EnvironmentVariables
    {
        /// <summary>
        /// Name of the process-wide kill switch that disables IMDSv2. Exposed so diagnostics can
        /// reference the variable by name without ever reading or logging its value.
        /// </summary>
        internal const string DisableImdsV2EnvVar = "MSAL_MI_DISABLE_IMDS_V2";

        public static string IdentityEndpoint => Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT");
        public static string IdentityHeader => Environment.GetEnvironmentVariable("IDENTITY_HEADER");
        public static string PodIdentityEndpoint => Environment.GetEnvironmentVariable("AZURE_POD_IDENTITY_AUTHORITY_HOST");
        public static string ImdsEndpoint => Environment.GetEnvironmentVariable("IMDS_ENDPOINT");
        public static string MsiEndpoint => Environment.GetEnvironmentVariable("MSI_ENDPOINT");
        public static string MsiSecret => Environment.GetEnvironmentVariable("MSI_SECRET");
        public static string IdentityServerThumbprint => Environment.GetEnvironmentVariable("IDENTITY_SERVER_THUMBPRINT");
        public static string MachineLearningDefaultClientId => Environment.GetEnvironmentVariable("DEFAULT_IDENTITY_CLIENT_ID");

        /// <summary>
        /// Process-wide kill switch that disables IMDSv2. IMDSv2 remains enabled by default; it is
        /// disabled only when <c>MSAL_MI_DISABLE_IMDS_V2</c> is set to <c>true</c> or <c>1</c>
        /// (case-insensitive). Any other value - including absent, empty, and unrecognized values -
        /// leaves IMDSv2 enabled.
        /// </summary>
        /// <remarks>
        /// Read live on every call rather than cached, so flipping the variable takes effect without
        /// restarting the process. This mirrors <c>MSAL_MI_DISABLE_PERSISTENT_CERT_CACHE</c>.
        /// </remarks>
        public static bool IsImdsV2Disabled
        {
            get
            {
                string value = Environment.GetEnvironmentVariable(DisableImdsV2EnvVar);

                return !string.IsNullOrEmpty(value) &&
                       (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                        value.Equals("true", StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
