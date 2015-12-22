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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Indicates the type of <see cref=" UserIdentifier"/>
    /// </summary>
    public enum UserIdentifierType
    {
        /// <summary>
        /// When a <see cref=" UserIdentifier"/> of this type is passed in a token acquisition operation,
        /// the operation is guaranteed to return a token issued for the user with corresponding <see cref=" UserIdentifier.UniqueId"/> or fail.
        /// </summary>
        UniqueId,

        /// <summary>
        /// When a <see cref=" UserIdentifier"/> of this type is passed in a token acquisition operation,
        /// the operation restricts cache matches to the value provided and injects it as a hint in the authentication experience. However the end user could overwrite that value, resulting in a token issued to a different account than the one specified in the <see cref=" UserIdentifier"/> in input.
        /// </summary>
        OptionalDisplayableId,
        
        /// <summary>
        /// When a <see cref=" UserIdentifier"/> of this type is passed in a token acquisition operation,
        /// the operation is guaranteed to return a token issued for the user with corresponding <see cref=" UserIdentifier.DisplayableId"/> (UPN or email) or fail
        /// </summary>
        RequiredDisplayableId
    }

    /// <summary>
    /// Contains identifier for a user.
    /// </summary>
    public sealed class UserIdentifier
    {
        private const string AnyUserId = "AnyUser";
        private static readonly UserIdentifier AnyUserSingleton = new UserIdentifier(AnyUserId, UserIdentifierType.UniqueId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type"></param>
        public UserIdentifier(string id, UserIdentifierType type)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            this.Id = id;
            this.Type = type;
        }

        /// <summary>
        /// Gets type of the <see cref="UserIdentifier"/>.
        /// </summary>
        public UserIdentifierType Type { get; private set; }
        
        /// <summary>
        /// Gets Id of the <see cref="UserIdentifier"/>.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets an static instance of <see cref="UserIdentifier"/> to represent any user.
        /// </summary>
        public static UserIdentifier AnyUser { 
            get
            {
                return AnyUserSingleton;
            }
        }

        internal bool IsAnyUser 
        {
            get
            {
                return (this.Type == AnyUser.Type && this.Id == AnyUser.Id);
            }            
        }

        internal string UniqueId
        {
            get
            {
                return (!this.IsAnyUser && this.Type == UserIdentifierType.UniqueId) ? this.Id : null;
            }
        }

        internal string DisplayableId
        {
            get
            {
                return (!this.IsAnyUser && (this.Type == UserIdentifierType.OptionalDisplayableId || this.Type == UserIdentifierType.RequiredDisplayableId)) ? this.Id : null;
            }
        }
    }

}
