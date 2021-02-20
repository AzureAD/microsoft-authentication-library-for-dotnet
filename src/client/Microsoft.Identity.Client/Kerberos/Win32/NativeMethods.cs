// -----------------------------------------------------------------------
// Licensed to The .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Kerberos.Win32
{
    internal unsafe class NativeMethods
    {
        private const string SECUR32 = "secur32.dll";
        private const string ADVAPI32 = "advapi32.dll";
        private const string KERNEL32 = "kernel32.dll";

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
                    LowPart = (UInt32)(luid & 0xffffffffL),
                    HighPart = (Int32)(luid >> 32)
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

        public enum KERB_PROTOCOL_MESSAGE_TYPE : UInt32
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

            public bool IsSet => this.dwLower > 0 || this.dwUpper > 0;
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
    }
}
