// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos
{
    /// <summary>
    /// Flags that specify the attributes required by the AcceptSecurityContext (CredSSP) function
    /// for a server to establish the context.
    /// https://docs.microsoft.com/en-us/windows/win32/api/sspi/nf-sspi-acceptsecuritycontext
    /// </summary>
    [Flags]
    internal enum AcceptContextFlag
    {
        Zero = 0,
        Delegate = 0x00000001,
        MutualAuth = 0x00000002,
        ReplayDetect = 0x00000004,
        SequenceDetect = 0x00000008,
        Confidentiality = 0x00000010,
        UseSessionKey = 0x00000020,
        AllocateMemory = 0x00000100,
        Connection = 0x00000800,
        AcceptExtendedError = 0x00008000,
        AcceptStream = 0x00010000,
        AcceptIntegrity = 0x00020000,
        AcceptIdentify = 0x00080000,
        ProxyBindings = 0x04000000,
        AllowMissingBindings = 0x10000000,
        UnverifiedTargetName = 0x20000000
    }
}
