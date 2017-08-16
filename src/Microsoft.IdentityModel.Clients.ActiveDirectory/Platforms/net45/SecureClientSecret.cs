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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// This class allows to pass client secret as a SecureString to the API.
    /// </summary>
    public class SecureClientSecret : ISecureClientSecret
    {
        private SecureString secureString;

        /// <summary>
        /// Required Constructor
        /// </summary>
        /// <param name="secret">SecureString secret. Required and cannot be null.</param>
        public SecureClientSecret(SecureString secret)
        {
            if (secret == null)
            {
                throw new ArgumentNullException(nameof(secret));
            }

            this.secureString = secret;
        }
        
        /// <summary>
        /// Applies the secret to the dictionary.
        /// </summary>
        /// <param name="parameters">Dictionary to which the securestring is applied to be sent to server</param>
        public void ApplyTo(IDictionary<string, string> parameters)
        {
            var output = new char[secureString.Length];
            IntPtr secureStringPtr = Marshal.SecureStringToCoTaskMemUnicode(secureString);
            for (int i = 0; i < secureString.Length; i++)
            {
                output[i] = (char) Marshal.ReadInt16(secureStringPtr, i*2);
            }

            Marshal.ZeroFreeCoTaskMemUnicode(secureStringPtr);
            parameters[OAuthParameter.ClientSecret] = new string(output);

            if (secureString != null && !secureString.IsReadOnly())
            {
                secureString.Clear();
            }

            secureString = null;
        }
    }
}
