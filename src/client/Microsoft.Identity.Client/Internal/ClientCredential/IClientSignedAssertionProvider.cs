// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Internal.ClientCredential
{
    /// <summary>
    /// Capability interface: implemented only by credentials that can produce a ClientSignedAssertion
    /// (JWT + optional TokenBindingCertificate).
    /// </summary>
    internal interface IClientSignedAssertionProvider
    {
        Task<ClientSignedAssertion> GetAssertionAsync(AssertionRequestOptions options, CancellationToken cancellationToken);
    }
}
