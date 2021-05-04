// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

using static Microsoft.Identity.Client.Kerberos.Win32.NativeMethods;

namespace Microsoft.Identity.Client.Kerberos.Win32
{
#pragma warning disable 618 // This workaround required for Native Win32 API call

    internal partial class SspiSecurityContext : IDisposable
    {
        private const int SECPKG_CRED_BOTH = 0x00000003;
        private const int SECURITY_NETWORK_DREP = 0x00;

        private const int MaxTokenSize = 16 * 1024;

        private const InitContextFlag DefaultRequiredFlags =
                                    InitContextFlag.Connection |
                                    InitContextFlag.ReplayDetect |
                                    InitContextFlag.SequenceDetect |
                                    InitContextFlag.Confidentiality |
                                    InitContextFlag.AllocateMemory |
                                    InitContextFlag.Delegate |
                                    InitContextFlag.InitExtendedError;

        private readonly HashSet<object> disposable = new HashSet<object>();

        private readonly Credential credential;
        private readonly InitContextFlag clientFlags;

        private SECURITY_HANDLE credentialsHandle;
        private SECURITY_HANDLE securityContext;
        private long logonId;

        public SspiSecurityContext(
            Credential credential,
            string package,
            long logonId = 0,
            InitContextFlag clientFlags = DefaultRequiredFlags)
        {
            this.credential = credential;
            this.clientFlags = clientFlags;
            this.Package = package;
            this.logonId = logonId;
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

            // 1. acquire
            // 2. initialize
            // 3. ??

            SecStatus result = 0;

            int tokenSize = 0;

            SecBufferDesc clientToken = default;

            try
            {
                do
                {
                    InitContextFlag contextFlags;

                    clientToken = new SecBufferDesc(tokenSize);

                    if (!this.credentialsHandle.IsSet || result == SecStatus.SEC_I_CONTINUE_NEEDED)
                    {
                        this.AcquireCredentials();
                    }

                    result = InitializeSecurityContext_0(
                                    ref this.credentialsHandle,
                                    IntPtr.Zero,
                                    targetNameNormalized,
                                    this.clientFlags,
                                    0,
                                    SECURITY_NETWORK_DREP,
                                    IntPtr.Zero,
                                    0,
                                    ref this.securityContext,
                                    ref clientToken,
                                    out contextFlags,
                                    IntPtr.Zero);

                    if (result == SecStatus.SEC_E_INSUFFICENT_MEMORY)
                    {
                        if (tokenSize > MaxTokenSize)
                        {
                            break;
                        }

                        tokenSize += 1000;
                    }
                }
                while (result == SecStatus.SEC_I_INCOMPLETE_CREDENTIALS || result == SecStatus.SEC_E_INSUFFICENT_MEMORY);

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
            this.disposable.Add(thing);
        }

        private unsafe void AcquireCredentials()
        {
            CredentialHandle creds = this.credential.Structify();

            this.TrackUnmanaged(creds);
            IntPtr authIdPtr = IntPtr.Zero;
            if (this.logonId != 0)
            {
                authIdPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(long)));
                Marshal.StructureToPtr(this.logonId, authIdPtr, false);
            }
            SecStatus result = AcquireCredentialsHandle(
                                    null,
                                    this.Package,
                                    SECPKG_CRED_BOTH,
                                    authIdPtr,
                                    (void*)creds.DangerousGetHandle(),
                                    IntPtr.Zero,
                                    IntPtr.Zero,
                                    ref this.credentialsHandle,
                                    IntPtr.Zero);

            if (result != SecStatus.SEC_E_OK)
            {
                throw new Win32Exception((int)result);
            }

            this.TrackUnmanaged(this.credentialsHandle);
        }

        public unsafe void Dispose()
        {
            foreach (var thing in this.disposable)
            {
                if (thing is IDisposable managedDispose)
                {
                    managedDispose.Dispose();
                }
                else if (thing is SECURITY_HANDLE handle)
                {
                    DeleteSecurityContext(&handle);

                    ThrowIfError(FreeCredentialsHandle(&handle));
                }
                else if (thing is IntPtr pThing)
                {
                    Marshal.FreeHGlobal(pThing);
                }
            }
        }
    }

#pragma warning restore 618
}
