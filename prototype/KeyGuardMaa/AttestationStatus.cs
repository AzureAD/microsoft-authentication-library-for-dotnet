// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace KeyGuard.Attestation;

/// <summary>
/// High-level outcome categories returned by <see cref="AttestationClient.Attest"/>.
/// </summary>
public enum AttestationStatus
{
    /// <summary>Everything succeeded; <see cref="AttestationResult.Jwt"/> is populated.</summary>
    Success = 0,

    /// <summary>Native library returned a non-zero <c>AttestationResultErrorCode</c>.</summary>
    NativeError = 1,

    /// <summary>rc == 0 but the token buffer was null/empty.</summary>
    TokenEmpty = 2,

    /// <summary><see cref="AttestationClient"/> could not initialise the native DLL.</summary>
    NotInitialized = 3,

    /// <summary>Any managed exception thrown while attempting the call.</summary>
    Exception = 4
}

/// <summary>
/// Unified result returned by <see cref="AttestationClient.Attest"/>.
/// </summary>
/// <param name="Status">High-level category.</param>
/// <param name="Jwt">JWT when <see cref="AttestationStatus.Success"/>; otherwise null.</param>
/// <param name="NativeCode">
/// The raw integer code returned by <c>AttestKeyGuardImportKey</c>
/// (cast to <see cref="AttestationResultErrorCode"/> for readability).
/// Zero when <see cref="Status"/> is not <see cref="AttestationStatus.NativeError"/>.
/// </param>
/// <param name="Message">Optional descriptive text for non-success cases.</param>
public record AttestationResult(
    AttestationStatus Status,
    string? Jwt,
    int NativeCode,
    string? Message);
