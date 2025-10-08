// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.MtlsPop.Attestation
{
    /// <summary>
    /// High-level outcome categories returned by <see cref="AttestationClient.Attest"/>.
    /// </summary>
    internal enum AttestationStatus
    {
        /// <summary>Everything succeeded; <see cref="AttestationResult.Jwt"/> is populated.</summary>
        Success = 0,

        /// <summary>Native library returned a non-zero <c>AttestationResultErrorCode</c>.</summary>
        NativeError = 1,

        /// <summary>rc == 0 but the token buffer was null/empty.</summary>
        TokenEmpty = 2,

        /// <summary><see cref="AttestationClient"/> could not initialize the native DLL.</summary>
        NotInitialized = 3,

        /// <summary>Any managed exception thrown while attempting the call.</summary>
        Exception = 4
    }
}
