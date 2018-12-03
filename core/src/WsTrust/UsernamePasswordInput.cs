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

using System.Security;

#if DESKTOP 
using static System.Runtime.InteropServices.Marshal;
#endif

#if NET_CORE || NETSTANDARD
using static System.Security.SecureStringMarshal;
using static System.Runtime.InteropServices.Marshal;
#endif


using System;

namespace Microsoft.Identity.Core
{

    internal sealed class UsernamePasswordInput : IUsernameInput
    {
        public string UserName { get; set; }
#if DESKTOP || NET_CORE || NETSTANDARD //TODO: remove once MSAL is split 
        private SecureString securePassword;
#endif
        private string password;


        public UsernamePasswordInput(string userName, string password)
        {
            this.password = password;
            this.UserName = userName;
        }

#if DESKTOP || NET_CORE || NETSTANDARD
        public UsernamePasswordInput(string userName, SecureString securePassword)
        {
            this.securePassword = securePassword;
            this.UserName = userName;
        }
#endif

        public char[] PasswordToCharArray() 
        {
            
#if DESKTOP || NET_CORE || NETSTANDARD
            if (securePassword != null)
            {
                var output = new char[securePassword.Length];

                IntPtr secureStringPtr = SecureStringToCoTaskMemUnicode(securePassword);
                for (int i = 0; i < securePassword.Length; i++)
                {
                    output[i] = (char)ReadInt16(secureStringPtr, i * 2);
                }

                ZeroFreeCoTaskMemUnicode(secureStringPtr);
                return output;
            }

#endif
            return (this.password != null) ? this.password.ToCharArray() : null;
        }

        public bool HasPassword()
        {

            bool hasSecurePassword = false;
#if DESKTOP || NET_CORE || NETSTANDARD
            hasSecurePassword = this.securePassword != null;
#endif
            bool hasPlainPassowrd = !string.IsNullOrEmpty(password);

            return hasSecurePassword || hasPlainPassowrd;
        }

    }
}

