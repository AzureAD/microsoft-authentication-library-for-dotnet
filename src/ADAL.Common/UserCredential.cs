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

#if ADAL_WINRT
#else
using System.Security;
#endif

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
    internal sealed class UserCredential
    {
        /// <summary>
        /// Constructor to create user credential. Using this constructor would imply integrated authentication with logged in user
        /// and it can only be used in domain joined scenarios.
        /// </summary>
        public UserCredential()
            : this(null, (string)null)
        {
            this.UserAuthType = UserAuthType.IntegratedAuth;
        }

        /// <summary>
        /// Constructor to create credential with client id and secret
        /// </summary>
        /// <param name="userId">Identifier of the user application requests token on behalf.</param>
        public UserCredential(string userId) 
            : this(userId, (string)null)
        {
            this.UserAuthType = UserAuthType.IntegratedAuth;
        }

        /// <summary>
        /// Constructor to create credential with client id and secret
        /// </summary>
        /// <param name="userId">Identifier of the user application requests token on behalf.</param>
        /// <param name="password">User password.</param>
        public UserCredential(string userId, string password)
        {
            this.UserId = userId;
            this.Password = password;
            this.UserAuthType = UserAuthType.UsernamePassword;
        }

#if ADAL_WINRT
#else
        /// <summary>
        /// Constructor to create credential with client id and secret
        /// </summary>
        /// <param name="userId">Identifier of the user application requests token on behalf.</param>
        /// <param name="securePassword">User password.</param>
        public UserCredential(string userId, SecureString securePassword)
        {
            this.UserId = userId;
            this.SecurePassword = securePassword;
            this.UserAuthType = UserAuthType.UsernamePassword;
        }
#endif

        /// <summary>
        /// Gets identifier of the user.
        /// </summary>
        public string UserId { get; internal set; }

        internal string Password { get; private set; }

        internal UserAuthType UserAuthType { get; private set; }

#if ADAL_WINRT
        internal char[] PasswordToCharArray()
        {
            return (this.Password != null) ? this.Password.ToCharArray() : null;
        }
#else
        internal SecureString SecurePassword { get; private set; }

        internal char[] PasswordToCharArray()
        {
            if (this.SecurePassword != null)
            {
                return this.SecurePassword.ToCharArray();
            }

            if (this.Password != null)
            {
                return this.Password.ToCharArray();
            }

            return null;
        }
#endif
    }
}
