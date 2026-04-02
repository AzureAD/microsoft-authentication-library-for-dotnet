// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    internal interface IClientCredential
    {
        AssertionType AssertionType { get; }

        /// <summary>
        /// Resolves credential material for a single token request.
        /// The returned <see cref="CredentialMaterial"/> contains the body parameters to add to
        /// the token request and, optionally, a certificate to use for mTLS transport.
        /// </summary>
        /// <param name="context">Immutable context describing the current request.</param>
        /// <param name="cancellationToken">Cancellation token; by convention the last parameter.</param>
        Task<CredentialMaterial> GetCredentialMaterialAsync(
            CredentialContext context,
            CancellationToken cancellationToken);
    }
}
