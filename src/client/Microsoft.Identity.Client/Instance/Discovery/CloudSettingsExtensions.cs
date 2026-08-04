// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    /// <summary>
    /// Typed convenience accessors over the <see cref="CloudSettings"/> key-bag.
    /// </summary>
    /// <remarks>
    /// These read values from the <see cref="CloudSettings"/> key-bag via
    /// <see cref="CloudSettings.GetValueOrDefault(string)"/>. Being extension methods keyed off
    /// <see cref="MsalCloudKeys"/>, accessors can be added or removed without changing the
    /// <see cref="CloudSettings"/> type.
    /// </remarks>
    public static class CloudSettingsExtensions
    {
        private const string DefaultSuffix = "/.default";

        /// <summary>
        /// The FIC token exchange audience for the cloud in <b>bare</b> form (no <c>/.default</c>),
        /// suitable for managed-identity / resource contexts. Returns <c>null</c> if not set.
        /// </summary>
        public static string TokenExchangeAudience(this CloudSettings settings)
        {
            return settings?.GetValueOrDefault(MsalCloudKeys.TokenExchangeAudience);
        }

        /// <summary>
        /// The FIC token exchange <b>scope</b> for the cloud — the bare audience with <c>/.default</c>
        /// appended — suitable for client-credentials / app-token contexts. Returns <c>null</c> if the
        /// audience is not set.
        /// </summary>
        /// <remarks>
        /// The audience is stored bare; the scope form is computed here so no call site hand-appends
        /// <c>/.default</c>.
        /// </remarks>
        public static string TokenExchangeScope(this CloudSettings settings)
        {
            string audience = settings?.GetValueOrDefault(MsalCloudKeys.TokenExchangeAudience);
            if (string.IsNullOrEmpty(audience))
            {
                return null;
            }

            return audience.EndsWith(DefaultSuffix, StringComparison.OrdinalIgnoreCase)
                ? audience
                : audience + DefaultSuffix;
        }
    }
}
