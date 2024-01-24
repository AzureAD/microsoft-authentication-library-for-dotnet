// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos
{
#pragma warning disable 618 // This workaround required for Native Win32 API call

    internal unsafe class NativeMethods
    {
        private const string SECUR32 = "secur32.dll";
        private const string ADVAPI32 = "advapi32.dll";
        private const string KERNEL32 = "kernel32.dll";

        [DllImport(
            SECUR32,
            EntryPoint = "InitializeSecurityContext",
            CharSet = (CharSet)4,
            BestFitMapping = false,
            ThrowOnUnmappableChar = true,
            SetLastError = true)]
        internal static extern SecStatus InitializeSecurityContext_0(
            ref SECURITY_HANDLE phCredential,
            IntPtr phContext,
            string pszTargetName,
            InitContextFlag fContextReq,
            int Reserved1,
            int TargetDataRep,
            IntPtr pInput,
            int Reserved2,
            ref SECURITY_HANDLE phNewContext,
            ref SecBufferDesc pOutput,
            out InitContextFlag pfContextAttr,
            IntPtr ptsExpiry
        );

        [DllImport(
            SECUR32,
            CharSet = (CharSet)4,
            BestFitMapping = false,
            ThrowOnUnmappableChar = true,
            EntryPoint = "AcquireCredentialsHandle")]
        internal static extern SecStatus AcquireCredentialsHandle(
            string pszPrincipal,
            string pszPackage,
            int fCredentialUse,
            IntPtr PAuthenticationID,
            void* pAuthData,
            IntPtr pGetKeyFn,
            IntPtr pvGetKeyArgument,
            ref SECURITY_HANDLE phCredential,
            IntPtr ptsExpiry
        );

        [DllImport(SECUR32)]
        internal static extern uint FreeCredentialsHandle(SECURITY_HANDLE* handle);

        [DllImport(SECUR32)]
        public static extern SecStatus DeleteSecurityContext(SECURITY_HANDLE* context);

        [DllImport(SECUR32)]
        public static extern int LsaDeregisterLogonProcess(
            IntPtr LsaHandle
        );

        [DllImport(SECUR32)]
        public static extern int LsaLookupAuthenticationPackage(
            LsaSafeHandle LsaHandle,
            ref LSA_STRING PackageName,
            out int AuthenticationPackage
        );

        [DllImport(SECUR32)]
        public static extern int LsaConnectUntrusted(
           [Out] out LsaSafeHandle LsaHandle
        );

        [DllImport(SECUR32)]
        public static unsafe extern int LsaCallAuthenticationPackage(
            LsaSafeHandle LsaHandle,
            int AuthenticationPackage,
            void* ProtocolSubmitBuffer,
            int SubmitBufferLength,
            out LsaBufferSafeHandle ProtocolReturnBuffer,
            out int ReturnBufferLength,
            out int ProtocolStatus
        );

        [DllImport(SECUR32)]
        public static extern int LsaFreeReturnBuffer(IntPtr Buffer);

        [DllImport(ADVAPI32)]
        public static extern int LsaNtStatusToWinError(int Status);

        [DllImport(KERNEL32)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport(ADVAPI32)]
        public static extern bool ImpersonateLoggedOnUser(LsaTokenSafeHandle hToken);

        [DllImport(ADVAPI32)]
        public static extern bool RevertToSelf();

        public static void LsaThrowIfError(int result)
        {
            if (result != 0)
            {
                result = LsaNtStatusToWinError(result);

                throw new Win32Exception(result);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KERB_INTERACTIVE_LOGON
        {
            public KERB_LOGON_SUBMIT_TYPE MessageType;
            public UNICODE_STRING LogonDomainName;
            public UNICODE_STRING UserName;
            public UNICODE_STRING Password;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_SOURCE
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] SourceName; // TOKEN_SOURCE_LENGTH
            public LUID SourceIdentifier;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KERB_S4U_LOGON
        {
            public KERB_LOGON_SUBMIT_TYPE MessageType;
            public S4uFlags Flags;
            public UNICODE_STRING ClientUpn;
            public UNICODE_STRING ClientRealm;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        [Flags]
        public enum S4uFlags
        {
            KERB_S4U_LOGON_FLAG_CHECK_LOGONHOURS = 0x2,
            KERB_S4U_LOGON_FLAG_IDENTIFY = 0x8
        }

        public enum KERB_LOGON_SUBMIT_TYPE
        {
            KerbInteractiveLogon = 2,
            KerbSmartCardLogon = 6,
            KerbWorkstationUnlockLogon = 7,
            KerbSmartCardUnlockLogon = 8,
            KerbProxyLogon = 9,
            KerbTicketLogon = 10,
            KerbTicketUnlockLogon = 11,
            KerbS4ULogon = 12,
            KerbCertificateLogon = 13,
            KerbCertificateS4ULogon = 14,
            KerbCertificateUnlockLogon = 15,
            KerbNoElevationLogon = 83,
            KerbLuidLogon = 84,
        }

        public enum SECURITY_LOGON_TYPE
        {
            UndefinedLogonType = 0,
            Interactive = 2,
            Network,
            Batch,
            Service,
            Proxy,
            Unlock,
            NetworkCleartext,
            NewCredentials,
            RemoteInteractive,
            CachedInteractive,
            CachedRemoteInteractive,
            CachedUnlock
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LSA_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public string Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LUID
        {
            public uint LowPart;
            public int HighPart;

            public static implicit operator ulong(LUID luid)
            {
                ulong val = (ulong)luid.HighPart << 32;

                return val + luid.LowPart;
            }

            public static implicit operator LUID(long luid)
            {
                return new LUID
                {
                    LowPart = (uint)(luid & 0xffffffffL),
                    HighPart = (int)(luid >> 32)
                };
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KERB_SUBMIT_TKT_REQUEST
        {
            public KERB_PROTOCOL_MESSAGE_TYPE MessageType;
            public LUID LogonId;
            public int Flags;
            public KERB_CRYPTO_KEY32 Key;
            public int KerbCredSize;
            public int KerbCredOffset;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KERB_PURGE_TKT_CACHE_EX_REQUEST
        {
            public KERB_PROTOCOL_MESSAGE_TYPE MessageType;
            public LUID LogonId;
            public int Flags;
            public KERB_TICKET_CACHE_INFO_EX TicketTemplate;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KERB_TICKET_CACHE_INFO_EX
        {
            public UNICODE_STRING ClientName;
            public UNICODE_STRING ClientRealm;
            public UNICODE_STRING ServerName;
            public UNICODE_STRING ServerRealm;
            public long StartTime;
            public long EndTime;
            public long RenewTime;
            public int EncryptionType;
            public int TicketFlags;
        }

        public enum KERB_PROTOCOL_MESSAGE_TYPE : uint
        {
            KerbDebugRequestMessage = 0,
            KerbQueryTicketCacheMessage,
            KerbChangeMachinePasswordMessage,
            KerbVerifyPacMessage,
            KerbRetrieveTicketMessage,
            KerbUpdateAddressesMessage,
            KerbPurgeTicketCacheMessage,
            KerbChangePasswordMessage,
            KerbRetrieveEncodedTicketMessage,
            KerbDecryptDataMessage,
            KerbAddBindingCacheEntryMessage,
            KerbSetPasswordMessage,
            KerbSetPasswordExMessage,
            KerbVerifyCredentialsMessage,
            KerbQueryTicketCacheExMessage,
            KerbPurgeTicketCacheExMessage,
            KerbRefreshSmartcardCredentialsMessage,
            KerbAddExtraCredentialsMessage,
            KerbQuerySupplementalCredentialsMessage,
            KerbTransferCredentialsMessage,
            KerbQueryTicketCacheEx2Message,
            KerbSubmitTicketMessage,
            KerbAddExtraCredentialsExMessage,
            KerbQueryKdcProxyCacheMessage,
            KerbPurgeKdcProxyCacheMessage,
            KerbQueryTicketCacheEx3Message,
            KerbCleanupMachinePkinitCredsMessage,
            KerbAddBindingCacheEntryExMessage,
            KerbQueryBindingCacheMessage,
            KerbPurgeBindingCacheMessage,
            KerbPinKdcMessage,
            KerbUnpinAllKdcsMessage,
            KerbQueryDomainExtendedPoliciesMessage,
            KerbQueryS4U2ProxyCacheMessage,
            KerbRetrieveKeyTabMessage,
            KerbRefreshPolicyMessage
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KERB_CRYPTO_KEY32
        {
            public int KeyType;
            public int Length;
            public int Offset;
        }

        internal enum SecBufferType
        {
            SECBUFFER_VERSION = 0,
            SECBUFFER_DATA = 1,
            SECBUFFER_TOKEN = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SECURITY_HANDLE
        {
            public ulong dwLower;
            public ulong dwUpper;

            public bool IsSet => dwLower > 0 || dwUpper > 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SECURITY_INTEGER
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SecPkgContext_SecString
        {
            public void* sValue;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SecBuffer
        {
            public int cbBuffer;
            public SecBufferType BufferType;
            public IntPtr pvBuffer;

            public SecBuffer(int bufferSize)
            {
                cbBuffer = bufferSize;
                BufferType = SecBufferType.SECBUFFER_TOKEN;
                pvBuffer = Marshal.AllocHGlobal(bufferSize);
            }

            public SecBuffer(byte[] secBufferBytes)
                : this(secBufferBytes.Length)
            {
                Marshal.Copy(secBufferBytes, 0, pvBuffer, cbBuffer);
            }

            public void Dispose()
            {
                if (pvBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pvBuffer);
                    pvBuffer = IntPtr.Zero;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SecBufferDesc : IDisposable
        {
            private readonly SecBufferType ulVersion;
            public int cBuffers;
            public IntPtr pBuffers; // Point to SecBuffer

            public SecBufferDesc(int bufferSize)
                : this(new SecBuffer(bufferSize))
            {
            }

            public SecBufferDesc(byte[] secBufferBytes)
                : this(new SecBuffer(secBufferBytes))
            {
            }

            private SecBufferDesc(SecBuffer secBuffer)
            {
                ulVersion = SecBufferType.SECBUFFER_VERSION;

                cBuffers = 1;

                pBuffers = Marshal.AllocHGlobal(Marshal.SizeOf(secBuffer));

                Marshal.StructureToPtr(secBuffer, pBuffers, false);
            }

            public void Dispose()
            {
                if (pBuffers != IntPtr.Zero)
                {
                    ForEachBuffer(thisSecBuffer => thisSecBuffer.Dispose());

                    // Freeing pBuffers

                    Marshal.FreeHGlobal(pBuffers);
                    pBuffers = IntPtr.Zero;
                }
            }

            private void ForEachBuffer(Action<SecBuffer> onBuffer)
            {
                for (int Index = 0; Index < cBuffers; Index++)
                {
                    int CurrentOffset = Index * Marshal.SizeOf(typeof(SecBuffer));

                    SecBuffer thisSecBuffer = (SecBuffer)Marshal.PtrToStructure(
                        IntPtr.Add(
                            pBuffers,
                            CurrentOffset
                        ),
                        typeof(SecBuffer)
                    );

                    onBuffer(thisSecBuffer);
                }
            }

            public byte[] ReadBytes()
            {
                if (cBuffers <= 0)
                {
                    return Array.Empty<byte>();
                }

                int finalLen = 0;
                var bufferList = new List<byte[]>();

                ForEachBuffer(thisSecBuffer =>
                {
                    if (thisSecBuffer.cbBuffer <= 0)
                    {
                        return;
                    }

                    var buffer = new byte[thisSecBuffer.cbBuffer];

                    Marshal.Copy(thisSecBuffer.pvBuffer, buffer, 0, thisSecBuffer.cbBuffer);

                    bufferList.Add(buffer);

                    finalLen += thisSecBuffer.cbBuffer;
                });

                var finalBuffer = new byte[finalLen];

                var position = 0;

                for (var i = 0; i < bufferList.Count; i++)
                {
                    bufferList[i].CopyTo(finalBuffer, position);

                    position += bufferList[i].Length - 1;
                }

                return finalBuffer;
            }
        }
    }
#pragma warning restore 618
}
