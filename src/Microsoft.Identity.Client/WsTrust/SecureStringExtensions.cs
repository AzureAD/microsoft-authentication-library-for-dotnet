//------------------------------------------------------------------------------
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

#if DESKTOP || ANDROID || iOS || MAC
using System.Security;
using static System.Runtime.InteropServices.Marshal;
#else
using System.Security;
using static System.Security.SecureStringMarshal;
using static System.Runtime.InteropServices.Marshal;
#endif


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
