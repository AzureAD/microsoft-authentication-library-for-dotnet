// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Labs
{
    /// <summary>
    /// Provides a mapping between an application tuple
    /// (<see cref="CloudType"/>, <see cref="Scenario"/>, <see cref="AppKind"/>) and the
    /// Key Vault secret names that store its client ID and credentials.
    /// </summary>
    public interface IAppMapProvider
    {
        /// <summary>
        /// Gets a map of tuples to application secret-name sets.
        /// </summary>
        /// <returns>
        /// A read-only dictionary keyed by <c>(cloudType, scenario, appKind)</c> whose values
        /// describe which Key Vault secret names hold each application credential.
        /// </returns>
        IReadOnlyDictionary<(CloudType cloud, Scenario scenario, AppKind kind), AppSecretKeys> GetAppMap();
    }
}
