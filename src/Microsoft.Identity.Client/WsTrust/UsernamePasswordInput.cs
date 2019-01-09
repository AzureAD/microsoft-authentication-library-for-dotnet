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

#if DESKTOP || ANDROID || iOS
using System.Security;
using static System.Runtime.InteropServices.Marshal;
#else
using System.Security;
using static System.Security.SecureStringMarshal;
using static System.Runtime.InteropServices.Marshal;
#endif

using System;
using Microsoft.Identity.Client.WsTrust;

namespace Microsoft.Identity.Client
{
    internal sealed class UsernamePasswordInput : IUsernameInput
    {
        public string UserName { get; set; }
        private SecureString _securePassword;
        private readonly string _password;

        public UsernamePasswordInput(string userName, string password)
        {
            _password = password;
            UserName = userName;
        }

        public UsernamePasswordInput(string userName, SecureString securePassword)
        {
            _securePassword = securePassword;
            UserName = userName;
        }

        public char[] PasswordToCharArray()
        {
            if (_securePassword != null)
            {
                var output = new char[_securePassword.Length];

                IntPtr secureStringPtr = SecureStringToCoTaskMemUnicode(_securePassword);
                for (int i = 0; i < _securePassword.Length; i++)
                {
                    output[i] = (char)ReadInt16(secureStringPtr, i * 2);
                }

                ZeroFreeCoTaskMemUnicode(secureStringPtr);
                return output;
            }

            return _password?.ToCharArray();
        }

        public bool HasPassword()
        {
            bool hasSecurePassword = false;

            hasSecurePassword = _securePassword != null;
            bool hasPlainPassword = !string.IsNullOrEmpty(_password);
            return hasSecurePassword || hasPlainPassword;
        }
    }
}

