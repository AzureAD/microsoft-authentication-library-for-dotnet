// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos
{
    /// <summary>
    /// Status code returned from SSPI functions.
    /// https://docs.microsoft.com/en-us/windows/win32/api/sspi/nf-sspi-initializesecuritycontexta
    /// </summary>
    internal enum SecStatus : uint
    {
        SEC_E_OK = 0x0,
        SEC_E_ERROR = 0x80000000,
        SEC_E_INSUFFICIENT_MEMORY = 0x80090300,
        SEC_E_INVALID_HANDLE = 0x80090301,
        SEC_E_TARGET_UNKNOWN = 0x80090303,
        SEC_E_UNSUPPORTED_FUNCTION = 0x80090302,
        SEC_E_INTERNAL_ERROR = 0x80090304,
        SEC_E_SECPKG_NOT_FOUND = 0x80090305,
        SEC_E_INVALID_TOKEN = 0x80090308,
        SEC_E_QOP_NOT_SUPPORTED = 0x8009030A,
        SEC_E_LOGON_DENIED = 0x8009030C,
        SEC_E_UNKNOWN_CREDENTIALS = 0x8009030D,
        SEC_E_NO_CREDENTIALS = 0x8009030E,
        SEC_E_MESSAGE_ALTERED = 0x8009030F,
        SEC_E_OUT_OF_SEQUENCE = 0x80090310,
        SEC_E_NO_AUTHENTICATING_AUTHORITY = 0x80090311,
        SEC_E_CONTEXT_EXPIRED = 0x80090317,
        SEC_E_INCOMPLETE_MESSAGE = 0x80090318,
        SEC_E_BUFFER_TOO_SMALL = 0x80090321,
        SEC_E_WRONG_PRINCIPAL = 0x80090322,
        SEC_E_CRYPTO_SYSTEM_INVALID = 0x80090337,
        SEC_I_CONTINUE_NEEDED = 0x00090312,
        SEC_I_CONTEXT_EXPIRED = 0x00090317,
        SEC_I_INCOMPLETE_CREDENTIALS = 0x00090320,
        SEC_I_RENEGOTIATE = 0x00090321
    }
}
