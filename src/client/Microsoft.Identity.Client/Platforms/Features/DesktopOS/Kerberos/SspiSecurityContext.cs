// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos
{
    internal partial class SspiSecurityContext : IDisposable
    {
        private const int SECPKG_CRED_BOTH = 0x00000003;
        private const int SECURITY_NETWORK_DREP = 0x00;

        private const int _maxTokenSize = 16 * 1024;

        private const InitContextFlag _defaultRequiredFlags =
                                    InitContextFlag.Connection |
                                    InitContextFlag.ReplayDetect |
                                    InitContextFlag.SequenceDetect |
                                    InitContextFlag.Confidentiality |
                                    InitContextFlag.AllocateMemory |
                                    InitContextFlag.Delegate |
                                    InitContextFlag.InitExtendedError;

        private readonly HashSet<object> _disposable = new HashSet<object>();

        private readonly Credential _credential;
        private readonly InitContextFlag _clientFlags;
        private NativeMethods.SECURITY_HANDLE _credentialsHandle;
        private NativeMethods.SECURITY_HANDLE _securityContext;
        private long _logonId;

        public SspiSecurityContext(
            Credential credential,
            string package,
            long logonId = 0,
            InitContextFlag clientFlags = _defaultRequiredFlags)
        {
            if (!DesktopOsHelper.IsWindows())
            {
                throw new PlatformNotSupportedException("Ticket Cache interface is not supported for this OS platform.");
            }

            _credential = credential;
            _clientFlags = clientFlags;
            Package = package;
            _logonId = logonId;
        }

        public string Package { get; private set; }

        private static void ThrowIfError(uint result)
        {
            if (result != 0 && result != 0x80090301)
            {
                throw new Win32Exception((int)result);
            }
        }

        public ContextStatus InitializeSecurityContext(string targetName, out byte[] clientRequest)
        {
            var targetNameNormalized = targetName.ToLowerInvariant();

            clientRequest = null;

            SecStatus result = 0;
            int tokenSize = 0;
            NativeMethods.SecBufferDesc clientToken = default;

            try
            {
                do
                {
                    InitContextFlag contextFlags;

                    clientToken = new NativeMethods.SecBufferDesc(tokenSize);

                    if (!_credentialsHandle.IsSet || result == SecStatus.SEC_I_CONTINUE_NEEDED)
                    {
                        AcquireCredentials();
                    }

                    result = NativeMethods.InitializeSecurityContext_0(
                                    ref _credentialsHandle,
                                    IntPtr.Zero,
                                    targetNameNormalized,
                                    _clientFlags,
                                    0,
                                    SECURITY_NETWORK_DREP,
                                    IntPtr.Zero,
                                    0,
                                    ref _securityContext,
                                    ref clientToken,
                                    out contextFlags,
                                    IntPtr.Zero);

                    if (result == SecStatus.SEC_E_INSUFFICIENT_MEMORY)
                    {
                        if (tokenSize > _maxTokenSize)
                        {
                            break;
                        }

                        tokenSize += 1000;
                    }
                }
                while (result == SecStatus.SEC_I_INCOMPLETE_CREDENTIALS || result == SecStatus.SEC_E_INSUFFICIENT_MEMORY);

                if (result > SecStatus.SEC_E_ERROR)
                {
                    throw new Win32Exception((int)result);
                }

                clientRequest = clientToken.ReadBytes();

                if (result == SecStatus.SEC_I_CONTINUE_NEEDED)
                {
                    return ContextStatus.RequiresContinuation;
                }

                return ContextStatus.Accepted;
            }
            finally
            {
                clientToken.Dispose();
            }
        }

        private void TrackUnmanaged(object thing)
        {
            _disposable.Add(thing);
        }

        private unsafe void AcquireCredentials()
        {
            CredentialHandle creds = _credential.Structify();

            TrackUnmanaged(creds);
            IntPtr authIdPtr = IntPtr.Zero;

            if (_logonId != 0)
            {
                authIdPtr = Marshal.AllocHGlobal(Marshal.SizeOf<long>());
                Marshal.StructureToPtr(_logonId, authIdPtr, false);
            }

            SecStatus result = NativeMethods.AcquireCredentialsHandle(
                                    null,
                                    Package,
                                    SECPKG_CRED_BOTH,
                                    authIdPtr,
                                    (void*)creds.DangerousGetHandle(),
                                    IntPtr.Zero,
                                    IntPtr.Zero,
                                    ref _credentialsHandle,
                                    IntPtr.Zero);

            if (result != SecStatus.SEC_E_OK)
            {
                throw new Win32Exception((int)result);
            }

            TrackUnmanaged(_credentialsHandle);
        }

        public unsafe void Dispose()
        {
            foreach (var thing in _disposable)
            {
                if (thing is IDisposable managedDispose)
                {
                    managedDispose.Dispose();
                }
                else if (thing is NativeMethods.SECURITY_HANDLE handle)
                {
                    NativeMethods.DeleteSecurityContext(&handle);

                    ThrowIfError(NativeMethods.FreeCredentialsHandle(&handle));
                }
                else if (thing is IntPtr pThing)
                {
                    Marshal.FreeHGlobal(pThing);
                }
            }
        }
    }
}
