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
        /// Classifies a single read of <c>MSAL_MI_DISABLE_IMDS_V2</c> into both states at once.
        /// </summary>
        /// <remarks>
        /// Preferred over the two properties below whenever a caller needs both, because the switch is
        /// read live: two separate reads can straddle a change and pair an enforcement decision with a
        /// log line that contradicts it.
        /// </remarks>
        internal static void ReadImdsV2DisableState(out bool disabled, out bool unrecognized)
        {
            string value = Environment.GetEnvironmentVariable(DisableImdsV2EnvVar);

            if (string.IsNullOrEmpty(value))
            {
                disabled = false;
                unrecognized = false;
                return;
            }

            disabled = value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                       value.Equals("true", StringComparison.OrdinalIgnoreCase);

            unrecognized = !disabled;
        }

        /// <summary>
        /// True when <c>MSAL_MI_DISABLE_IMDS_V2</c> is set to <c>true</c> or <c>1</c>
        /// (case-insensitive). Absent, empty, and unrecognized values leave IMDSv2 enabled.
        /// </summary>
        /// <remarks>
        /// Unrecognized values are ignored so a typo can never silently weaken token binding; matching
        /// is exact, so <c>"true "</c> with trailing whitespace does not disable IMDSv2. Use
        /// <see cref="ReadImdsV2DisableState"/> instead when the caller also needs to know whether the
        /// value was unrecognized.
        /// <para>
        /// Read live rather than cached, so changing the variable takes effect without restarting the
        /// process. Mirrors <c>MSAL_MI_DISABLE_PERSISTENT_CERT_CACHE</c>.
        /// </para>
        /// </remarks>
        public static bool IsImdsV2Disabled
        {
            get
            {
                ReadImdsV2DisableState(out bool disabled, out _);
                return disabled;
            }
        }
    }
}
