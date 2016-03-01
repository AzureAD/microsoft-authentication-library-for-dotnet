//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    internal class PlatformInformation : PlatformInformationBase
    {
        public override string GetProductName()
        {
            return "MSAL.Desktop";
        }

        public override async Task<string> GetUserPrincipalNameAsync()
        {
            return await Task.Factory.StartNew(() =>
            {
                const int NameUserPrincipal = 8;
                uint userNameSize = 0;
                NativeMethods.GetUserNameEx(NameUserPrincipal, null, ref userNameSize);
                if (userNameSize == 0)
                {
                    throw new MsalException(MsalError.GetUserNameFailed, new Win32Exception(Marshal.GetLastWin32Error()));
                }

                StringBuilder sb = new StringBuilder((int) userNameSize);
                if (!NativeMethods.GetUserNameEx(NameUserPrincipal, sb, ref userNameSize))
                {
                    throw new MsalException(MsalError.GetUserNameFailed, new Win32Exception(Marshal.GetLastWin32Error()));
                }

                return sb.ToString();
            }).ConfigureAwait(false);
        }

        public override string GetEnvironmentVariable(string variable)
        {
            string value = Environment.GetEnvironmentVariable(variable);
            return !string.IsNullOrWhiteSpace(value) ? value : null;
        }

        public override string GetProcessorArchitecture()
        {
            return NativeMethods.GetProcessorArchitecture();
        }

        public override string GetOperatingSystem()
        {
            return Environment.OSVersion.ToString();
        }

        public override string GetDeviceModel()
        {
            // Since ADAL .NET may be used on servers, for security reasons, we do not emit device type.
            return null;
        }

        public override async Task<bool> IsUserLocalAsync(CallState callState)
        {
            return await Task.Factory.StartNew(() =>
            {
                WindowsIdentity current = WindowsIdentity.GetCurrent();
                if (current != null)
                {
                    string prefix = WindowsIdentity.GetCurrent().Name.Split('\\')[0].ToUpperInvariant();
                    return prefix.Equals(Environment.MachineName.ToUpperInvariant());
                }

                return false;
            }).ConfigureAwait(false);
        }

        public override bool IsDomainJoined()
        {
            bool returnValue = false;
            try
            {
                NativeMethods.NetJoinStatus status;
                IntPtr pDomain;
                int result = NativeMethods.NetGetJoinInformation(null, out pDomain, out status);
                if (pDomain != IntPtr.Zero)
                {
                    NativeMethods.NetApiBufferFree(pDomain);
                }

                returnValue = result == NativeMethods.ErrorSuccess &&
                              status == NativeMethods.NetJoinStatus.NetSetupDomainName;
            }
            catch (Exception ex)
            {
                PlatformPlugin.Logger.Error(null, ex);
                // ignore the exception as the result is already set to false;
            }

            return returnValue;
        }
        
        private static class NativeMethods
        {
            private const int PROCESSOR_ARCHITECTURE_AMD64 = 9;
            private const int PROCESSOR_ARCHITECTURE_ARM = 5;
            private const int PROCESSOR_ARCHITECTURE_IA64 = 6;
            private const int PROCESSOR_ARCHITECTURE_INTEL = 0;

            [DllImport("kernel32.dll")]
            private static extern void GetNativeSystemInfo(ref SYSTEM_INFO lpSystemInfo);

            public static string GetProcessorArchitecture()
            {
                try
                { 
                    SYSTEM_INFO systemInfo = new SYSTEM_INFO();
                    GetNativeSystemInfo(ref systemInfo);
                    switch (systemInfo.wProcessorArchitecture)
                    {
                        case PROCESSOR_ARCHITECTURE_AMD64:
                        case PROCESSOR_ARCHITECTURE_IA64:
                            return "x64";

                        case PROCESSOR_ARCHITECTURE_ARM:
                            return "ARM";

                        case PROCESSOR_ARCHITECTURE_INTEL:
                            return "x86";

                        default:
                            return "Unknown";
                    }
                }
                catch(Exception ex)
                {
                    PlatformPlugin.Logger.Error(null, ex);
                    return "Unknown";
                }
            }

            [DllImport("secur32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.U1)]
            public static extern bool GetUserNameEx(int nameFormat, StringBuilder userName, ref uint userNameSize);

            [StructLayout(LayoutKind.Sequential)]
            private struct SYSTEM_INFO
            {
                public short wProcessorArchitecture;
                public short wReserved;
                public int dwPageSize;
                public IntPtr lpMinimumApplicationAddress;
                public IntPtr lpMaximumApplicationAddress;
                public IntPtr dwActiveProcessorMask;
                public int dwNumberOfProcessors;
                public int dwProcessorType;
                public int dwAllocationGranularity;
                public short wProcessorLevel;
                public short wProcessorRevision;
            }

            public const int ErrorSuccess = 0;

            [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int NetGetJoinInformation(string server, out IntPtr domain, out NetJoinStatus status);

            [DllImport("Netapi32.dll")]
            public static extern int NetApiBufferFree(IntPtr Buffer);

            public enum NetJoinStatus
            {
                NetSetupUnknownStatus = 0,
                NetSetupUnjoined,
                NetSetupWorkgroupName,
                NetSetupDomainName
            }
        }
    }
}
