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
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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

        public static string PlatformSpecificToLower(this string input)
        {
            return input.ToLower(CultureInfo.InvariantCulture);
        }

        public static string GetUserPrincipalName()
        {
            const int NameUserPrincipal = 8;
            uint userNameSize = 0;
            NativeMethods.GetUserNameEx(NameUserPrincipal, null, ref userNameSize);
            if (userNameSize == 0)
            {
                throw new AdalException(AdalError.GetUserNameFailed, new Win32Exception(Marshal.GetLastWin32Error()));
            }

            StringBuilder sb = new StringBuilder((int) userNameSize);
            if (!NativeMethods.GetUserNameEx(NameUserPrincipal, sb, ref userNameSize))
            {
                throw new AdalException(AdalError.GetUserNameFailed, new Win32Exception(Marshal.GetLastWin32Error()));
            }

            return sb.ToString();
        }

        public static string CreateSha256Hash(string input)
        {
            using (SHA256Cng sha = new SHA256Cng())
            {
                UTF8Encoding encoding = new UTF8Encoding();
                return Convert.ToBase64String(sha.ComputeHash(encoding.GetBytes(input)));
            }
        }

        public static void CloseHttpWebResponse(WebResponse response)
        {
            response.Close();
        }

        private static class NativeMethods
        {
            [DllImport("secur32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.U1)]
            public static extern bool GetUserNameEx(int nameFormat, StringBuilder userName, ref uint userNameSize);
        }
    }
}
