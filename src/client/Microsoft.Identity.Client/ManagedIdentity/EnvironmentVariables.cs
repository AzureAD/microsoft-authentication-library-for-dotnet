// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class EnvironmentVariables
    {
        public static string IdentityEndpoint => Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT");
        public static string IdentityHeader => Environment.GetEnvironmentVariable("IDENTITY_HEADER");
        public static string PodIdentityEndpoint => Environment.GetEnvironmentVariable("AZURE_POD_IDENTITY_AUTHORITY_HOST");
        public static string ImdsEndpoint => Environment.GetEnvironmentVariable("IMDS_ENDPOINT");
        public static string MsiEndpoint => Environment.GetEnvironmentVariable("MSI_ENDPOINT");
        public static string MsiSecret => Environment.GetEnvironmentVariable("MSI_SECRET");
        public static string IdentityServerThumbprint => Environment.GetEnvironmentVariable("IDENTITY_SERVER_THUMBPRINT");
        public static string MachineLearningDefaultClientId => Environment.GetEnvironmentVariable("DEFAULT_IDENTITY_CLIENT_ID");

        public const string DisableImdsV2EnvVar = "MSAL_MI_DISABLE_IMDS_V2";

        /// <summary>
        /// True when <c>MSAL_MI_DISABLE_IMDS_V2</c> is set to <c>true</c> or <c>1</c>.
        /// </summary>
        /// <remarks>
        /// Only these two values disable IMDSv2. Anything else, including an unset or empty value, is
        /// ignored so that a typo cannot silently downgrade a host that supports IMDSv2. The value is
        /// read from the process environment, which is fixed when the process starts, so a change to
        /// the variable requires a restart or recycle to take effect.
        /// </remarks>
        public static bool IsImdsV2Disabled
        {
            get
            {
                string value = Environment.GetEnvironmentVariable(DisableImdsV2EnvVar);

                return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(value, "1", StringComparison.Ordinal);
            }
        }
    }
}
