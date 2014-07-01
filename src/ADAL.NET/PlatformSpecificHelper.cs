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
using System.DirectoryServices.ActiveDirectory;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class PlatformSpecificHelper
    {
        public static string GetProductName()
        {
            return ".NET";
        }

        public static string GetEnvironmentVariable(string variable)
        {
            string value = Environment.GetEnvironmentVariable(variable);
            return !string.IsNullOrWhiteSpace(value) ? value : null;
        }

        public static AuthenticationResult ProcessServiceError(string error, string errorDescription)
        {
            throw new AdalServiceException(error, errorDescription);
        }

        public static string PlatformSpecificToLower(this string input)
        {
            return input.ToLower(CultureInfo.InvariantCulture);
        }

        public static bool IsDomainJoined()
        {
            bool returnValue = true;
            IntPtr pDomain = IntPtr.Zero;
            try
            {
                NativeMethods.NetJoinStatus status = NativeMethods.NetJoinStatus.NetSetupUnknownStatus;
                int result = NativeMethods.NetGetJoinInformation(null, out pDomain, out status);
                if (pDomain != IntPtr.Zero)
                {
                    NativeMethods.NetApiBufferFree(pDomain);
                }
                if (result == NativeMethods.ErrorSuccess)
                {
                    if (status != NativeMethods.NetJoinStatus.NetSetupDomainName)
                    {
                        returnValue = false;
                    }
                }
                else
                {
                    returnValue = false;
                }
            }
            catch (Exception exception)
            {
                //if the machine is not domain joined or the request times out, this exception is thrown.
                returnValue = false;
            }
            finally
            {
                pDomain = IntPtr.Zero;
            }
            return returnValue;
        }

        public static bool IsUserLocal()
        {
            string prefix = WindowsIdentity.GetCurrent().Name.Split('\\')[0].ToUpperInvariant();
            return prefix.Equals(Environment.MachineName.ToUpperInvariant());
        }

        public static string GetUserPrincipalName()
        {
            string userId = System.DirectoryServices.AccountManagement.UserPrincipal.Current.UserPrincipalName;

            // On some machines, UserPrincipalName returns null
            if (string.IsNullOrWhiteSpace(userId))
            {
                const int NameUserPrincipal = 8;
                uint userNameSize = 0;
                if (!NativeMethods.GetUserNameEx(NameUserPrincipal, null, ref userNameSize))
                {
                    throw new AdalException(AdalError.GetUserNameFailed, new Win32Exception(Marshal.GetLastWin32Error()));
                }

                StringBuilder sb = new StringBuilder((int) userNameSize);
                if (!NativeMethods.GetUserNameEx(NameUserPrincipal, sb, ref userNameSize))
                {
                    throw new AdalException(AdalError.GetUserNameFailed, new Win32Exception(Marshal.GetLastWin32Error()));
                }

                userId = sb.ToString();
            }

            return userId;
        }

        internal static string CreateSha256Hash(string input)
        {
            SHA256 sha256 = SHA256Managed.Create();
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] inputBytes = encoding.GetBytes(input);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);
            string hash = Convert.ToBase64String(hashBytes);
            return hash;
        }

        private static class NativeMethods
        {
            [DllImport("secur32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.U1)]
            public static extern bool GetUserNameEx(int nameFormat, StringBuilder userName, ref uint userNameSize);

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
