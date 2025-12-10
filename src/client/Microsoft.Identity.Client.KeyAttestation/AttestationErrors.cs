// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.KeyAttestation
{
    internal static class AttestationErrors
    {
        internal static string Describe(AttestationResultErrorCode rc) => rc switch
        {
            AttestationResultErrorCode.ERRORCURLINITIALIZATION
                => "libcurl failed to initialize (DLL missing or version mismatch).",
            AttestationResultErrorCode.ERRORHTTPREQUESTFAILED
                => "Could not reach the attestation service (network / proxy?).",
            AttestationResultErrorCode.ERRORATTESTATIONFAILED
                => "The enclave rejected the evidence (key type / PCR policy).",
            AttestationResultErrorCode.ERRORJWTDECRYPTIONFAILED
                => "The JWT returned by the service could not be decrypted.",
            AttestationResultErrorCode.ERRORLOGGERINITIALIZATION
                => "Native logger setup failed (rare).",
            _ => rc.ToString()         // default: enum name
        };
    }
}
