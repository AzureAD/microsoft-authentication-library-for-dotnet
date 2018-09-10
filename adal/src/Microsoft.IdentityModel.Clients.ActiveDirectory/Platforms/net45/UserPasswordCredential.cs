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

using Microsoft.Identity.Core;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Credential used for username/password authentication.
    /// </summary>
    public sealed class UserPasswordCredential : UserCredential
    {
        internal UsernamePasswordInput UserPasswordInput { get; }

        /// <summary>
        /// Constructor to create credential with username and password
        /// </summary>
        /// <param name="userName">Identifier of the user application requests token on behalf.</param>
        /// <param name="password">User password.</param>
        public UserPasswordCredential(string userName, string password)
        {
            UserPasswordInput = new UsernamePasswordInput(userName, password);
            
        }

        /// <summary>
        /// Constructor to create credential with username and password
        /// </summary>
        /// <param name="userName">Identifier of the user application requests token on behalf.</param>
        /// <param name="securePassword">User password.</param>
        public UserPasswordCredential(string userName, SecureString securePassword) 
        {
            UserPasswordInput = new UsernamePasswordInput(userName, securePassword);
        }
    }
}
