// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Labs.Internal
{
    /// <summary>
    /// Abstraction of a secret store (Azure Key Vault by default).
    /// </summary>
    internal interface ISecretStore
    {
        /// <summary>
        /// Retrieves the value of the specified secret.
        /// </summary>
        /// <param name="secretName">The name of the secret to read.</param>
        /// <param name="ct">An optional cancellation token.</param>
        /// <returns>The secret value.</returns>
        Task<string> GetAsync(string secretName, CancellationToken ct = default);
    }
}
