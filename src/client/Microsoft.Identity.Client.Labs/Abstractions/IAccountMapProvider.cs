// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.Labs
{
    /// <summary>
    /// Provides a mapping between a user tuple
    /// (<see cref="AuthType"/>, <see cref="CloudType"/>, <see cref="Scenario"/>) and the
    /// Key Vault secret name that stores the corresponding username.
    /// </summary>
    public interface IAccountMapProvider
    {
        /// <summary>
        /// Gets a map of tuples to username secret names. The value for each key must be a Key Vault
        /// secret name whose value is the username to use for the tuple.
        /// </summary>
        /// <returns>
        /// A read-only dictionary keyed by <c>(authType, cloudType, scenario)</c> whose values are
        /// the Key Vault secret names containing usernames.
        /// </returns>
        IReadOnlyDictionary<(AuthType auth, CloudType cloud, Scenario scenario), string> GetUsernameMap();
    }
}
