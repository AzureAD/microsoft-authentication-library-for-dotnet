// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.Labs
{
    /// <summary>
    /// Central configuration for the Labs resolver, including Key Vault location and
    /// the policy used to select the password secret for user accounts.
    /// </summary>
    public sealed class LabsOptions
    {
        /// <summary>
        /// Gets or sets the URI of the Azure Key Vault that contains secrets referenced by this package.
        /// </summary>
        public Uri KeyVaultUri { get; set; } = default!;

        /// <summary>
        /// Gets or sets the global password secret name (representing the current "active lab" password)
        /// to use for most user tuples, for example <c>msidlab1_pwd</c>.
        /// Use an empty string to indicate that no global value is configured.
        /// </summary>
        public string GlobalPasswordSecret { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets per-cloud password secret overrides, allowing sovereign clouds to use a
        /// different active-lab password secret name. Leave empty if not used.
        /// </summary>
        public Dictionary<CloudType, string> PasswordSecretByCloud { get; set; } = new();

        /// <summary>
        /// Gets or sets per-tuple password secret overrides. The dictionary key must be the
        /// lowercase string <c>"{auth}.{cloud}.{scenario}"</c> (for example, <c>"basic.public.obo"</c>).
        /// Leave empty if not used.
        /// </summary>
        public Dictionary<string, string> PasswordSecretByTuple { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether secret names should be derived by convention
        /// when a corresponding map entry is missing.
        /// </summary>
        public bool EnableConventionFallback { get; set; } = true;
    }
}
