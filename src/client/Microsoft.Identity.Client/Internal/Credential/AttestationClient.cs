// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.Credential
{
    /// <summary>
    /// Calls into the Interop layer to get an attestation token for MAA Service.
    /// </summary>
    internal static class AttestationClient
    {
        internal static Task<string> GetTokenAsync(
            string endpoint, KeyMaterial km, CancellationToken ct) =>
            Task.FromResult<string>(null);
    }
}
