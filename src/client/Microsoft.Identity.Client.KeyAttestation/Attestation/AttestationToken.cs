// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.KeyAttestation.Attestation
{
    /// <summary>
    /// Represents a successfully attested token with structured metadata.
    /// </summary>
    /// <param name="Token">The raw JWT string.</param>
    /// <param name="ExpiresOn">The expiration time of the token (UTC).</param>
    /// <remarks>
    /// Internal use only. This record enables the library to access token expiry 
    /// without requiring callers to manually decode the JWT.
    /// </remarks>
    internal sealed record AttestationToken(
        string Token,
        DateTimeOffset ExpiresOn);
}
