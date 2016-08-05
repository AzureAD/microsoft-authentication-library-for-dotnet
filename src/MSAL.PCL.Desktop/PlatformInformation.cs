//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

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
                PlatformPlugin.Logger.Warning(null, ex.Message);
                // ignore the exception as the result is already set to false;
            }

            return returnValue;
        }

        private static class NativeMethods
        {
            public enum NetJoinStatus
            {
                NetSetupUnknownStatus = 0,
                NetSetupUnjoined,
                NetSetupWorkgroupName,
                NetSetupDomainName
            }

            private const int PROCESSOR_ARCHITECTURE_AMD64 = 9;
            private const int PROCESSOR_ARCHITECTURE_ARM = 5;
            private const int PROCESSOR_ARCHITECTURE_IA64 = 6;
            private const int PROCESSOR_ARCHITECTURE_INTEL = 0;
            public const int ErrorSuccess = 0;

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
                catch (Exception ex)
                {
                    PlatformPlugin.Logger.Warning(null, ex.Message);
                    return "Unknown";
                }
            }

            [DllImport("secur32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.U1)]
            public static extern bool GetUserNameEx(int nameFormat, StringBuilder userName, ref uint userNameSize);

            [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int NetGetJoinInformation(string server, out IntPtr domain, out NetJoinStatus status);

            [DllImport("Netapi32.dll")]
            public static extern int NetApiBufferFree(IntPtr Buffer);

            [StructLayout(LayoutKind.Sequential)]
            private struct SYSTEM_INFO
            {
                public readonly short wProcessorArchitecture;
                public readonly short wReserved;
                public readonly int dwPageSize;
                public readonly IntPtr lpMinimumApplicationAddress;
                public readonly IntPtr lpMaximumApplicationAddress;
                public readonly IntPtr dwActiveProcessorMask;
                public readonly int dwNumberOfProcessors;
                public readonly int dwProcessorType;
                public readonly int dwAllocationGranularity;
                public readonly short wProcessorLevel;
                public readonly short wProcessorRevision;
            }
        }
    }
}