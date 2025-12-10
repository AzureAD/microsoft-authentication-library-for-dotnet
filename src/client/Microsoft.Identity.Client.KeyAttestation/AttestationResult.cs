// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.KeyAttestation
{
    /// <summary>
    /// AttestationResult is the result of an attestation operation.
    /// </summary>
    /// <param name="Status">High-level outcome category.</param>
    /// <param name="Jwt">JWT on success; null otherwise (caller may pass null).</param>
    /// <param name="NativeErrorCode">Raw native return code (0 on success).</param>
    /// <param name="ErrorMessage">Optional descriptive text for non-success cases.</param>
    /// <remarks>
    /// This is a positional record. The compiler synthesizes init-only auto-properties:
    ///   public AttestationStatus Status { get; init; }
    ///   public string Jwt           { get; init; }
    ///   public int NativeErrorCode  { get; init; }
    ///   public string ErrorMessage  { get; init; }
    /// Because they are init-only, values are fixed after construction; to "modify" use a 'with'
    /// expression, e.g.: var updated = result with { Jwt = newJwt };
    /// The netstandard2.0 target relies on the IsExternalInit shim (see IsExternalInit.cs) to enable 'init'.
    /// </remarks>
    public sealed record AttestationResult(
        AttestationStatus Status,
        string Jwt,
        int NativeErrorCode,
        string ErrorMessage);
}
