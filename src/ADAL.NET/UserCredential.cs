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

using System.Security;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    public sealed partial class UserCredential
    {
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
        /// Constructor to create credential with client id and secret
        /// </summary>
        /// <param name="userName">Identifier of the user application requests token on behalf.</param>
        /// <param name="securePassword">User password.</param>
        public UserCredential(string userName, SecureString securePassword)
        {
            this.UserName = userName;
            this.SecurePassword = securePassword;
            this.UserAuthType = UserAuthType.UsernamePassword;
        }

        internal string Password { get; private set; }

        internal SecureString SecurePassword { get; private set; }

        internal char[] PasswordToCharArray()
        {
            if (this.SecurePassword != null)
            {
                return this.SecurePassword.ToCharArray();
            }

            return (this.Password != null) ? this.Password.ToCharArray() : null;
        }
    }
}
