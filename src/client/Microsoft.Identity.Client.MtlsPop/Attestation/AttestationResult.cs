// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.MtlsPop.Attestation
{
    /// <summary>
    /// AttestationResult is the result of an attestation operation.
    /// </summary>
    /// <param name="Status"></param>
    /// <param name="Jwt"></param>
    /// <param name="NativeErrorCode"></param>
    /// <param name="ErrorMessage"></param>
    public sealed record AttestationResult(
        AttestationStatus Status,
        string Jwt,
        int NativeErrorCode,
        string ErrorMessage);
}
