// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using System.Security;
using static System.Runtime.InteropServices.Marshal;

namespace Microsoft.Identity.Client.WsTrust
{
    internal static class SecureStringExtensions
    {
        public static char[] PasswordToCharArray(this SecureString secureString)
        {
            if (secureString == null)
            {
                return null;
            }

            var output = new char[secureString.Length];

            IntPtr secureStringPtr = SecureStringToCoTaskMemUnicode(secureString);
            for (int i = 0; i < secureString.Length; i++)
            {
                output[i] = (char)ReadInt16(secureStringPtr, i * 2);
            }

            ZeroFreeCoTaskMemUnicode(secureStringPtr);
            return output;
        }
    }
}
