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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal enum UserAuthType
    {
        IntegratedAuth,
        UsernamePassword
    }

    // Disabled Non-Interactive Feature
    /// <summary>
    /// Credential used for integrated authentication on domain-joined machines.
    /// </summary>
    public sealed class UserCredential
    {
        /// <summary>
        /// Constructor to create user credential. Using this constructor would imply integrated authentication with logged in user
        /// and it can only be used in domain joined scenarios.
        /// </summary>
        public UserCredential()
        {
            this.UserAuthType = UserAuthType.IntegratedAuth;
        }

        /// <summary>
        /// Constructor to create credential with client id and secret
        /// </summary>
        /// <param name="userName">Identifier of the user application requests token on behalf.</param>
        public UserCredential(string userName)
        {
            this.UserName = userName;
            this.UserAuthType = UserAuthType.IntegratedAuth;
        }

        /// <summary>
        /// Constructor to create credential with client id and secret
        /// </summary>
        /// <param name="userName">Identifier of the user application requests token on behalf.</param>
        /// <param name="password">User password.</param>
        public UserCredential(string userName, string password)
        {
            this.UserName = userName;
            this.Password = password;
            this.UserAuthType = UserAuthType.UsernamePassword;
        }

        /// <summary>
        /// Gets identifier of the user.
        /// </summary>
        public string UserName { get; internal set; }

        internal UserAuthType UserAuthType { get; private set; }

        internal string Password { get; private set; }

        internal char[] PasswordToCharArray()
        {
            return (this.Password != null) ? this.Password.ToCharArray() : null;
        }

        internal char[] EscapedPasswordToCharArray()
        {
            string password = this.Password;
            password = password.Replace("&", "&amp;");
            password = password.Replace("\"", "&quot;");
            password = password.Replace("'", "&apos;");
            password = password.Replace("<", "&lt;");
            password = password.Replace(">", "&gt;");
            return (password != null) ? password.ToCharArray() : null;
        }
    }
}
